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

let getOperations date : Operations =

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

let private processDivid (statement : Statement.Model) (divid : TransactionDivid) =

    statement

let private processSplit (statement : Statement.Model) (split : TransactionSplit) =

    statement

let private processTradeOpening (statement : Statement.Model) (trade : TransactionTrade) =

    let positionActive : Statement.PositionActiveToday =
        { Sequence     = trade.Sequence
          Date         = trade.Date
          IssueId      = trade.IssueId
          Direction    = trade.Direction
          Shares       = trade.Shares
          Basis        = trade.Price
          Close        = trade.Price
          Delta        = Decimal.Zero }

    { statement with
        PositionsActiveToday = statement.PositionsActiveToday |> Set.add positionActive }

let processTradeClosing (statement : Statement.Model) (trade : TransactionTrade) =

    let rec loop (statement : Statement.Model) = function
        | shares when shares = 0 -> statement
        | shares
            ->
            let positionsActive = statement.PositionsActiveToday
            let positionsClosed = statement.PositionsClosedToday

            let positionSubject =
                statement.PositionsActiveToday
                |> Seq.sortBy (fun x -> x.Sequence)
                |> Seq.filter (fun x -> x.IssueId = trade.IssueId)
                |> Seq.filter (fun x -> x.Direction = trade.Direction)
                |> Seq.head

            let positionClosed : Statement.PositionClosedToday =
                { Reference       = positionSubject.Sequence
                  Sequence        = trade.Sequence
                  Date            = trade.Date
                  IssueId         = trade.IssueId
                  Direction       = trade.Direction
                  Shares          = min shares positionSubject.Shares
                  Basis           = positionSubject.Basis
                  Close           = trade.Price
                  Delta           = trade.Price - positionSubject.Close }

            let positionsClosed = positionsClosed |> Set.add positionClosed
            let positionsActive = positionsActive |> Set.remove positionSubject

            let statement =
                if positionSubject.Shares > shares then
                    let positionSubject = { positionSubject with Shares = positionSubject.Shares - shares }
                    let positionsActive = positionsActive |> Set.add positionSubject
                    { statement with
                        PositionsActiveToday = positionsActive
                        PositionsClosedToday = positionsClosed }
                else
                    { statement with
                        PositionsActiveToday = positionsActive
                        PositionsClosedToday = positionsClosed }

            loop statement (shares - positionClosed.Shares)

    loop statement trade.Shares

let private processTrade (statement : Statement.Model) (trade : TransactionTrade) =

    match trade.Operation with
    | Opening -> processTradeOpening statement trade
    | Closing -> processTradeClosing statement trade

let private processTransaction (statement : Statement.Model) = function
    | Divid transaction -> processDivid statement transaction
    | Split transaction -> processSplit statement transaction
    | Trade transaction -> processTrade statement transaction

let private applyTransaction (statement : Statement.Model) transaction =

    let statement = processTransaction statement transaction
    let transactions = [| transaction |] |> Array.append statement.Transactions
    { statement with Transactions = transactions }

let mapClosedTodayToClosedPrior (positionClosedToday : Statement.PositionClosedToday) : Statement.PositionClosedPrior =

    { Reference       = positionClosedToday.Reference
      Sequence        = positionClosedToday.Sequence
      Date            = positionClosedToday.Date
      IssueId         = positionClosedToday.IssueId
      Direction       = positionClosedToday.Direction
      Shares          = positionClosedToday.Shares
      Basis           = positionClosedToday.Basis
      Close           = positionClosedToday.Close }

let mapClosePrice date (positionActiveToday : Statement.PositionActiveToday) =

    let quote = Persistence.selectQuote positionActiveToday.IssueId date
    let close = quote.Close
    let delta = close - positionActiveToday.Close

    { positionActiveToday with
        Close = close
        Delta = delta }

let computeStatement (statement : Statement.Model) operations : Statement.Model =

    let updateStop stops stoploss = stops |> Map.add stoploss.IssueId stoploss.Price
    let appendPrev (statement : Statement.Model) =
        statement.PositionsClosedToday
        |> Set.map mapClosedTodayToClosedPrior
        |> Set.union statement.PositionsClosedPrior

    let statement = { statement with Date  = operations.Date }
    let statement = { statement with Stops = operations.Stoplosses |> Array.fold updateStop statement.Stops }
    let statement = { statement with PositionsClosedPrior = appendPrev statement }
    let statement = { statement with PositionsClosedToday = Set.empty }
    let statement = operations.Transactions |> Array.fold applyTransaction statement
    let statement = { statement with PositionsActiveToday = statement.PositionsActiveToday |> Set.map (mapClosePrice statement.Date) }

    statement
