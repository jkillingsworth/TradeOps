module TradeOps.Types

open System

//-------------------------------------------------------------------------------------------------

type TransactionDivid =
    { Sequence : int
      IssueId  : int
      Date     : DateTime
      Amount   : decimal }

type TransactionSplit =
    { Sequence : int
      IssueId  : int
      Date     : DateTime
      New      : int
      Old      : int }

type TransactionTrade =
    { Sequence : int
      IssueId  : int
      Date     : DateTime
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
