module TradeOps.Reports.StatementTransactions

open System
open TradeOps.Types
open TradeOps.Processing

//-------------------------------------------------------------------------------------------------

type Divid =
    { Sequence : int
      Date     : DateTime
      IssueId  : int
      Ticker   : string
      Position : string
      Shares   : int
      Amount   : decimal
      PayDate  : DateTime }

type Split =
    { Sequence : int
      Date     : DateTime
      IssueId  : int
      Ticker   : string
      Position : string
      Shares   : int
      New      : int
      Old      : int }

type Trade =
    { Sequence : int
      Date     : DateTime
      IssueId  : int
      Ticker   : string
      Position : string
      Shares   : int
      Activity : string
      Price    : decimal }

type Model =
    { Divids   : Divid[]
      Splits   : Split[]
      Trades   : Trade[] }

//-------------------------------------------------------------------------------------------------

let render (statement : Statement.Model) =

    let mapDivid (item : TransactionDivid) : Divid =

        { Sequence = item.Sequence
          Date     = item.Date
          IssueId  = item.IssueId
          Ticker   = item.IssueId |> mapTicker
          Position = item.Position |> sprintf "%A"
          Shares   = item.Shares
          Amount   = item.Amount
          PayDate  = item.PayDate }

    let mapSplit (item : TransactionSplit) : Split =

        { Sequence = item.Sequence
          Date     = item.Date
          IssueId  = item.IssueId
          Ticker   = item.IssueId |> mapTicker
          Position = item.Position |> sprintf "%A"
          Shares   = item.Shares
          New      = item.New
          Old      = item.Old }

    let mapTrade (item : TransactionTrade) : Trade =

        { Sequence = item.Sequence
          Date     = item.Date
          IssueId  = item.IssueId
          Ticker   = item.IssueId |> mapTicker
          Position = item.Position |> sprintf "%A"
          Shares   = item.Shares
          Activity = item.Activity |> sprintf "%A"
          Price    = item.Price }

    let accumulate model = function
        | Divid transaction -> { model with Divids = Array.append model.Divids [| mapDivid transaction |] }
        | Split transaction -> { model with Splits = Array.append model.Splits [| mapSplit transaction |] }
        | Trade transaction -> { model with Trades = Array.append model.Trades [| mapTrade transaction |] }

    let model =
        { Divids = Array.empty
          Splits = Array.empty
          Trades = Array.empty }

    statement.Transactions |> Array.fold accumulate model
