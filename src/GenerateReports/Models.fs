module TradeOps.Models

open System

//-------------------------------------------------------------------------------------------------

module TransactionListing =

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

    let empty =
        { Divids   = Array.empty
          Splits   = Array.empty
          Trades   = Array.empty }
