module TradeOps.Types

open System

//-------------------------------------------------------------------------------------------------

type Issue =
    { IssueId   : int
      Ticker    : string }

type Quote =
    { IssueId   : int
      Date      : DateTime
      Hi        : decimal
      Lo        : decimal
      Close     : decimal }

type Direction =
    | Bullish
    | Bearish

type Operation =
    | Opening
    | Closing

type TransactionDivid =
    { Sequence  : int
      Date      : DateTime
      IssueId   : int
      Direction : Direction
      Shares    : int
      Amount    : decimal
      PayDate   : DateTime }

type TransactionSplit =
    { Sequence  : int
      Date      : DateTime
      IssueId   : int
      Direction : Direction
      Shares    : int
      New       : int
      Old       : int }

type TransactionTrade =
    { Sequence  : int
      Date      : DateTime
      IssueId   : int
      Direction : Direction
      Shares    : int
      Operation : Operation
      Price     : decimal }

type Transaction =
    | Divid of TransactionDivid
    | Split of TransactionSplit
    | Trade of TransactionTrade

type Stoploss =
    { Date      : DateTime
      IssueId   : int
      Price     : decimal }

//-------------------------------------------------------------------------------------------------

type Operations =
    { Date         : DateTime
      Transactions : Transaction[]
      Stoplosses   : Stoploss[] }

//-------------------------------------------------------------------------------------------------

module Statement =

    type PositionActiveToday =
        { Sequence             : int
          Date                 : DateTime
          IssueId              : int
          Direction            : Direction
          Shares               : int
          Basis                : decimal
          Close                : decimal
          Upper                : decimal
          Lower                : decimal
          Delta                : decimal }

    type PositionClosedToday =
        { Reference            : int
          Sequence             : int
          Date                 : DateTime
          IssueId              : int
          Direction            : Direction
          Shares               : int
          Basis                : decimal
          Close                : decimal
          Upper                : decimal
          Lower                : decimal
          Delta                : decimal }

    type PositionClosedPrior =
        { Reference            : int
          Sequence             : int
          Date                 : DateTime
          IssueId              : int
          Direction            : Direction
          Shares               : int
          Basis                : decimal
          Close                : decimal }

    type Model =
        { Date                 : DateTime
          Transactions         : Transaction[]
          Stops                : Map<int, decimal>
          PositionsActiveToday : Set<PositionActiveToday>
          PositionsClosedToday : Set<PositionClosedToday>
          PositionsClosedPrior : Set<PositionClosedPrior> }

    let empty =
        { Date                 = DateTime.MinValue
          Transactions         = Array.empty
          Stops                = Map.empty
          PositionsActiveToday = Set.empty
          PositionsClosedToday = Set.empty
          PositionsClosedPrior = Set.empty }
