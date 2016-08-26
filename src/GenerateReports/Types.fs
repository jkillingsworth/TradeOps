module TradeOps.Types

open System

//-------------------------------------------------------------------------------------------------

type Issue =
    { IssueId  : int
      Ticker   : string }

type Position =
    | Bullish
    | Bearish

type Activity =
    | Opening
    | Closing

type TransactionDivid =
    { Sequence : int
      Date     : DateTime
      IssueId  : int
      Position : Position
      Shares   : int
      Amount   : decimal
      PayDate  : DateTime }

type TransactionSplit =
    { Sequence : int
      Date     : DateTime
      IssueId  : int
      Position : Position
      Shares   : int
      New      : int
      Old      : int }

type TransactionTrade =
    { Sequence : int
      Date     : DateTime
      IssueId  : int
      Position : Position
      Shares   : int
      Activity : Activity
      Price    : decimal }

type Transaction =
    | Divid of TransactionDivid
    | Split of TransactionSplit
    | Trade of TransactionTrade

type Stoploss =
    { Date     : DateTime
      IssueId  : int
      Price    : decimal }

//-------------------------------------------------------------------------------------------------

type Operations =
    { Date         : DateTime
      Transactions : Transaction[]
      Stoplosses   : Stoploss[] }

//-------------------------------------------------------------------------------------------------

module Statement =

    type PositionActive =
        { Sequence        : int
          Date            : DateTime
          IssueId         : int
          Position        : Position
          Shares          : int
          Basis           : decimal }

    type PositionClosed =
        { Sequence        : int
          Date            : DateTime
          IssueId         : int
          Position        : Position
          Shares          : int
          Basis           : decimal
          Price           : decimal
          OpeningSequence : int
          OpeningDate     : DateTime }

    type Model =
        { Date            : DateTime
          Transactions    : Transaction[]
          PositionsActive : Set<PositionActive>
          PositionsClosed : Set<PositionClosed>
          Stops           : Map<int, decimal> }

    let empty =
        { Date            = DateTime.MinValue
          Transactions    = Array.empty
          Stops           = Map.empty
          PositionsActive = Set.empty
          PositionsClosed = Set.empty }
