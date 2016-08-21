module TradeOps.Processing

open System
open FSharp.Control.Reactive
open TradeOps.Types
open TradeOps.Models

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

let private mapSequence = function
    | Divid transaction -> transaction.Sequence
    | Split transaction -> transaction.Sequence
    | Trade transaction -> transaction.Sequence

let private mapDate = function
    | Divid transaction -> transaction.Date
    | Split transaction -> transaction.Date
    | Trade transaction -> transaction.Date

let private mapIssueId = function
    | Divid transaction -> transaction.IssueId
    | Split transaction -> transaction.IssueId
    | Trade transaction -> transaction.IssueId

let private mapTicker issueId =
    let issue = issues |> Array.find (fun x -> x.IssueId = issueId)
    issue.Ticker

//-------------------------------------------------------------------------------------------------

let getOperations date =

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

let renderTransactionListing (model : TransactionListing.Model) operations =

    let mapDivid (transaction : TransactionDivid) : TransactionListing.Divid =

        { Sequence = transaction.Sequence
          Date     = transaction.Date
          IssueId  = transaction.IssueId
          Ticker   = transaction.IssueId |> mapTicker
          Amount   = transaction.Amount
          PayDate  = transaction.PayDate }

    let mapSplit (transaction : TransactionSplit) : TransactionListing.Split =

        { Sequence = transaction.Sequence
          Date     = transaction.Date
          IssueId  = transaction.IssueId
          Ticker   = transaction.IssueId |> mapTicker
          New      = transaction.New
          Old      = transaction.Old }

    let mapTrade (transaction : TransactionTrade) : TransactionListing.Trade =

        { Sequence = transaction.Sequence
          Date     = transaction.Date
          IssueId  = transaction.IssueId
          Ticker   = transaction.IssueId |> mapTicker
          Shares   = transaction.Shares
          Price    = transaction.Price }

    let accumulate (model : TransactionListing.Model) = function
        | Divid transaction -> { model with Divids = Array.append model.Divids [| mapDivid transaction |] }
        | Split transaction -> { model with Splits = Array.append model.Splits [| mapSplit transaction |] }
        | Trade transaction -> { model with Trades = Array.append model.Trades [| mapTrade transaction |] }

    operations.Transactions |> Array.fold accumulate model
