module TradeOps.Types

open System

//-------------------------------------------------------------------------------------------------

type Issue =
    { IssueId  : int
      Ticker   : string }

type TransactionDivid =
    { Sequence : int
      Date     : DateTime
      IssueId  : int
      Amount   : decimal
      PayDate  : DateTime }

type TransactionSplit =
    { Sequence : int
      Date     : DateTime
      IssueId  : int
      New      : int
      Old      : int }

type TransactionTrade =
    { Sequence : int
      Date     : DateTime
      IssueId  : int
      Shares   : int
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
        { IssueId      : int
          Shares       : int
          TakeSequence : int
          TakeDate     : DateTime
          TakeBasis    : decimal }

    type PositionClosed =
        { IssueId      : int
          Shares       : int
          TakeSequence : int
          TakeDate     : DateTime
          TakeBasis    : decimal
          ExitSequence : int
          ExitDate     : DateTime
          ExitPrice    : decimal }

    type Model =
        { Date            : DateTime
          Transactions    : Transaction[]
          Stops           : Map<int, decimal>
          PositionsActive : Set<PositionActive>
          PositionsClosed : Set<PositionClosed> }

    let empty =
        { Date            = DateTime.MinValue
          Transactions    = Array.empty
          Stops           = Map.empty
          PositionsActive = Set.empty
          PositionsClosed = Set.empty }
