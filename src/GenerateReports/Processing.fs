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

    let positionActive : Statement.PositionActive =
        { Sequence     = trade.Sequence
          Date         = trade.Date
          IssueId      = trade.IssueId
          Position     = trade.Position
          Shares       = trade.Shares
          CostBasis    = trade.Price }

    { statement with
        PositionsActive = statement.PositionsActive |> Set.add positionActive }

let processTradeClosing (statement : Statement.Model) (trade : TransactionTrade) =

    let rec loop (statement : Statement.Model) = function
        | shares when shares = 0 -> statement
        | shares
            ->
            let positionsActive = statement.PositionsActive
            let positionsClosed = statement.PositionsClosed

            let positionSubject =
                statement.PositionsActive
                |> Seq.sortBy (fun x -> x.Sequence)
                |> Seq.filter (fun x -> x.IssueId = trade.IssueId)
                |> Seq.filter (fun x -> x.Position = trade.Position)
                |> Seq.head

            let positionClosed : Statement.PositionClosed =
                { Sequence      = trade.Sequence
                  Date          = trade.Date
                  IssueId       = trade.IssueId
                  Position      = trade.Position
                  Shares        = min shares positionSubject.Shares
                  CostBasis     = positionSubject.CostBasis
                  ExitPrice     = trade.Price
                  EntrySequence = positionSubject.Sequence
                  EntryDate     = positionSubject.Date }

            let positionsClosed = positionsClosed |> Set.add positionClosed
            let positionsActive = positionsActive |> Set.remove positionSubject

            let statement =
                if positionSubject.Shares > shares then
                    let positionSubject = { positionSubject with Shares = positionSubject.Shares - shares }
                    let positionsActive = positionsActive |> Set.add positionSubject
                    { statement with
                        PositionsActive = positionsActive
                        PositionsClosed = positionsClosed }
                else
                    { statement with
                        PositionsActive = positionsActive
                        PositionsClosed = positionsClosed }

            loop statement (shares - positionClosed.Shares)

    loop statement trade.Shares

let private processTrade (statement : Statement.Model) (trade : TransactionTrade) =

    match trade.Activity with
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

let computeStatement (statement : Statement.Model) operations : Statement.Model =

    let updateStop stops stoploss = stops |> Map.add stoploss.IssueId stoploss.Price

    let statement = { statement with Date  = operations.Date }
    let statement = { statement with Stops = operations.Stoplosses |> Array.fold updateStop statement.Stops }

    operations.Transactions
    |> Array.fold applyTransaction statement
