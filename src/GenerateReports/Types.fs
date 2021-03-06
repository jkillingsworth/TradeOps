﻿module TradeOps.Types

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
      AccountId : int
      Date      : DateTime
      IssueId   : int
      Direction : Direction
      Shares    : int
      Amount    : decimal
      PayDate   : DateTime }

type TransactionSplit =
    { Sequence  : int
      AccountId : int
      Date      : DateTime
      IssueId   : int
      Direction : Direction
      Shares    : int
      New       : int
      Old       : int }

type TransactionTrade =
    { Sequence  : int
      AccountId : int
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

type Adjustments =
    { Date         : DateTime
      Transactions : Transaction[]
      Stoplosses   : Stoploss[] }
