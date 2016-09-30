module TradeOps.Processing

open System
open FSharp.Control.Reactive
open TradeOps.Types
open TradeOps.Intermediate

//-------------------------------------------------------------------------------------------------

let private issues = Persistence.selectIssues ()

let mapSequence = function
    | Divid transaction -> transaction.Sequence
    | Split transaction -> transaction.Sequence
    | Trade transaction -> transaction.Sequence

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

let computeIntermediate intermediate (adjustments : Adjustments) =

    let withAdjustments f = f adjustments

    intermediate
    |> withAdjustments beginDay
    |> withAdjustments applyTransactions
    |> withAdjustments closeDay
