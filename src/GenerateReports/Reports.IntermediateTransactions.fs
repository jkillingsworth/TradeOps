module TradeOps.Reports.IntermediateTransactions

open System
open TradeOps.Types
open TradeOps.Processing

//-------------------------------------------------------------------------------------------------

type Divid =
    { Sequence  : int
      Date      : DateTime
      IssueId   : int
      Ticker    : string
      Direction : string
      Shares    : int
      Amount    : decimal
      PayDate   : DateTime }

type Split =
    { Sequence  : int
      Date      : DateTime
      IssueId   : int
      Ticker    : string
      Direction : string
      Shares    : int
      New       : int
      Old       : int }

type Trade =
    { Sequence  : int
      Date      : DateTime
      IssueId   : int
      Ticker    : string
      Direction : string
      Shares    : int
      Operation : string
      Price     : decimal }

type Model =
    { Divids    : Divid[]
      Splits    : Split[]
      Trades    : Trade[] }

//-------------------------------------------------------------------------------------------------

let render (intermediate : Intermediate.Model) =

    let mapDivid (item : TransactionDivid) : Divid =

        { Sequence  = item.Sequence
          Date      = item.Date
          IssueId   = item.IssueId
          Ticker    = item.IssueId |> mapTicker
          Direction = item.Direction |> sprintf "%A"
          Shares    = item.Shares
          Amount    = item.Amount
          PayDate   = item.PayDate }

    let mapSplit (item : TransactionSplit) : Split =

        { Sequence  = item.Sequence
          Date      = item.Date
          IssueId   = item.IssueId
          Ticker    = item.IssueId |> mapTicker
          Direction = item.Direction |> sprintf "%A"
          Shares    = item.Shares
          New       = item.New
          Old       = item.Old }

    let mapTrade (item : TransactionTrade) : Trade =

        { Sequence  = item.Sequence
          Date      = item.Date
          IssueId   = item.IssueId
          Ticker    = item.IssueId |> mapTicker
          Direction = item.Direction |> sprintf "%A"
          Shares    = item.Shares
          Operation = item.Operation |> sprintf "%A"
          Price     = item.Price }

    let accumulate model = function
        | Divid transaction -> { model with Divids = Array.append model.Divids [| mapDivid transaction |] }
        | Split transaction -> { model with Splits = Array.append model.Splits [| mapSplit transaction |] }
        | Trade transaction -> { model with Trades = Array.append model.Trades [| mapTrade transaction |] }

    let model =
        { Divids = Array.empty
          Splits = Array.empty
          Trades = Array.empty }

    intermediate.Transactions |> Array.fold accumulate model
