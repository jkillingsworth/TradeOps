module TradeOps.Types

open System

//-------------------------------------------------------------------------------------------------

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

//-------------------------------------------------------------------------------------------------

type Operations =
    { Date         : DateTime
      Transactions : Transaction[] }
