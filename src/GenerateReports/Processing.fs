module TradeOps.Processing

open System
open FSharp.Control.Reactive
open TradeOps.Types

//-------------------------------------------------------------------------------------------------

let private dateStart = Persistence.selectStartDate ()
let private holidays = Persistence.selectHolidays ()
let private issues = Persistence.selectIssues ()

let private addDays days (date : DateTime) = date.AddDays(float days)
let private isWeekendSat (date : DateTime) = date.DayOfWeek = DayOfWeek.Saturday
let private isWeekendSun (date : DateTime) = date.DayOfWeek = DayOfWeek.Sunday

let private isWeekendOrHoliday = function
    | date when isWeekendSat date -> true
    | date when isWeekendSun date -> true
    | date -> Array.contains date holidays

let generateDates dateFinal =
    id
    |> Observable.generate dateStart (fun date -> date <= dateFinal) (addDays +1)
    |> Observable.filter (isWeekendOrHoliday >> not)

//-------------------------------------------------------------------------------------------------

let mapSequence = function
    | Divid transaction -> transaction.Sequence
    | Split transaction -> transaction.Sequence
    | Trade transaction -> transaction.Sequence

let mapDate = function
    | Divid transaction -> transaction.Date
    | Split transaction -> transaction.Date
    | Trade transaction -> transaction.Date

let mapIssueId = function
    | Divid transaction -> transaction.IssueId
    | Split transaction -> transaction.IssueId
    | Trade transaction -> transaction.IssueId

let mapTicker issueId =
    let issue = issues |> Array.find (fun x -> x.IssueId = issueId)
    issue.Ticker

//-------------------------------------------------------------------------------------------------

let getAdjustments date : Adjustments =

    let transactions =
        [ Persistence.selectTransactionsDivid date
          Persistence.selectTransactionsSplit date
          Persistence.selectTransactionsTrade date ]

    let transactions =
        transactions
        |> Array.concat
        |> Array.sortBy mapSequence

    let stoplosses = Persistence.selectStoplosses date

    { Date         = date
      Transactions = transactions
      Stoplosses   = stoplosses }

//-------------------------------------------------------------------------------------------------

let private processDivid (intermediate : Intermediate.Model) (divid : TransactionDivid) =

    intermediate

let private processSplit (intermediate : Intermediate.Model) (split : TransactionSplit) =

    intermediate

let private processTradeOpening (intermediate : Intermediate.Model) (trade : TransactionTrade) =

    let positionActive : Intermediate.PositionActiveToday =
        { Sequence     = trade.Sequence
          Date         = trade.Date
          IssueId      = trade.IssueId
          Direction    = trade.Direction
          Shares       = trade.Shares
          Basis        = trade.Price
          Final        = trade.Price
          Upper        = trade.Price
          Lower        = trade.Price
          Delta        = Decimal.Zero }

    { intermediate with
        PositionsActiveToday = intermediate.PositionsActiveToday |> Set.add positionActive }

let processTradeClosing (intermediate : Intermediate.Model) (trade : TransactionTrade) =

    let computeUpper quote =
        match trade.Direction, intermediate.Stops.[trade.IssueId] with
        | Bullish, stop -> quote.Hi
        | Bearish, stop -> quote.Hi |> min stop |> max trade.Price

    let computeLower quote =
        match trade.Direction, intermediate.Stops.[trade.IssueId] with
        | Bearish, stop -> quote.Lo
        | Bullish, stop -> quote.Lo |> max stop |> min trade.Price

    let rec loop (intermediate : Intermediate.Model) = function
        | shares when shares = 0 -> intermediate
        | shares
            ->
            let quote = Persistence.selectQuote trade.IssueId intermediate.Date

            let positionsActive = intermediate.PositionsActiveToday
            let positionsClosed = intermediate.PositionsClosedToday

            let positionSubject =
                intermediate.PositionsActiveToday
                |> Seq.sortBy (fun x -> x.Sequence)
                |> Seq.filter (fun x -> x.IssueId = trade.IssueId)
                |> Seq.filter (fun x -> x.Direction = trade.Direction)
                |> Seq.head

            let positionClosed : Intermediate.PositionClosedToday =
                { Reference       = positionSubject.Sequence
                  Sequence        = trade.Sequence
                  Date            = trade.Date
                  IssueId         = trade.IssueId
                  Direction       = trade.Direction
                  Shares          = min shares positionSubject.Shares
                  Basis           = positionSubject.Basis
                  Final           = trade.Price
                  Upper           = computeUpper quote
                  Lower           = computeLower quote
                  Delta           = trade.Price - positionSubject.Final }

            let positionsClosed = positionsClosed |> Set.add positionClosed
            let positionsActive = positionsActive |> Set.remove positionSubject

            let intermediate =
                if positionSubject.Shares > shares then
                    let positionSubject = { positionSubject with Shares = positionSubject.Shares - shares }
                    let positionsActive = positionsActive |> Set.add positionSubject
                    { intermediate with
                        PositionsActiveToday = positionsActive
                        PositionsClosedToday = positionsClosed }
                else
                    { intermediate with
                        PositionsActiveToday = positionsActive
                        PositionsClosedToday = positionsClosed }

            loop intermediate (shares - positionClosed.Shares)

    loop intermediate trade.Shares

let private processTrade (intermediate : Intermediate.Model) (trade : TransactionTrade) =

    match trade.Operation with
    | Opening -> processTradeOpening intermediate trade
    | Closing -> processTradeClosing intermediate trade

let private processTransaction (intermediate : Intermediate.Model) = function
    | Divid transaction -> processDivid intermediate transaction
    | Split transaction -> processSplit intermediate transaction
    | Trade transaction -> processTrade intermediate transaction

let private applyTransaction (intermediate : Intermediate.Model) transaction =

    let intermediate = processTransaction intermediate transaction
    let transactions = [| transaction |] |> Array.append intermediate.Transactions
    { intermediate with Transactions = transactions }

let mapClosedTodayToClosedPrior (positionClosedToday : Intermediate.PositionClosedToday) : Intermediate.PositionClosedPrior =

    { Reference       = positionClosedToday.Reference
      Sequence        = positionClosedToday.Sequence
      Date            = positionClosedToday.Date
      IssueId         = positionClosedToday.IssueId
      Direction       = positionClosedToday.Direction
      Shares          = positionClosedToday.Shares
      Basis           = positionClosedToday.Basis
      Final           = positionClosedToday.Final }

let mapEndOfDayValues date (positionActiveToday : Intermediate.PositionActiveToday) =

    let quote = Persistence.selectQuote positionActiveToday.IssueId date
    let final = quote.Close
    let upper = quote.Hi
    let lower = quote.Lo
    let delta = quote.Close - positionActiveToday.Final

    { positionActiveToday with
        Final = final
        Upper = upper
        Lower = lower
        Delta = delta }

let computeIntermediate (intermediate : Intermediate.Model) adjustments : Intermediate.Model =

    let updateStop stops stoploss = stops |> Map.add stoploss.IssueId stoploss.Price
    let appendPrev (intermediate : Intermediate.Model) =
        intermediate.PositionsClosedToday
        |> Set.map mapClosedTodayToClosedPrior
        |> Set.union intermediate.PositionsClosedPrior

    let intermediate = { intermediate with Date = adjustments.Date }
    let intermediate = { intermediate with Stops = adjustments.Stoplosses |> Array.fold updateStop intermediate.Stops }
    let intermediate = { intermediate with PositionsClosedPrior = appendPrev intermediate }
    let intermediate = { intermediate with PositionsClosedToday = Set.empty }
    let intermediate = adjustments.Transactions |> Array.fold applyTransaction intermediate
    let intermediate = { intermediate with PositionsActiveToday = intermediate.PositionsActiveToday |> Set.map (mapEndOfDayValues intermediate.Date) }

    intermediate
