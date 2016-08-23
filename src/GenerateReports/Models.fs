﻿module TradeOps.Models

open System

//-------------------------------------------------------------------------------------------------

module StatementTransactions =

    type Divid = 
        { Sequence : int
          Date     : DateTime
          IssueId  : int
          Ticker   : string
          Amount   : decimal
          PayDate  : DateTime }

    type Split =
        { Sequence : int
          Date     : DateTime
          IssueId  : int
          Ticker   : string
          New      : int
          Old      : int }

    type Trade =
        { Sequence : int
          Date     : DateTime
          IssueId  : int
          Ticker   : string
          Shares   : int
          Price    : decimal }

    type Model =
        { Divids   : Divid[]
          Splits   : Split[]
          Trades   : Trade[] }

//-------------------------------------------------------------------------------------------------

module StatementPositions =

    type PositionActive =
        { Sequence        : int
          Date            : DateTime
          IssueId         : int
          Ticker          : string
          Shares          : int
          CostBasis       : decimal }

    type PositionClosed =
        { Sequence        : int
          Date            : DateTime
          IssueId         : int
          Ticker          : string
          Shares          : int
          CostBasis       : decimal
          ExitPrice       : decimal
          EntrySequence   : int
          EntryDate       : DateTime }

    type Model =
        { PositionsActive : PositionActive[]
          PositionsClosed : PositionClosed[] }

//-------------------------------------------------------------------------------------------------

module StatementStops =

    type Stop =
        { IssueId     : int
          Ticker      : string
          Price       : decimal }

    type Model =
        { StopsActive : Stop[]
          StopsClosed : Stop[] }
