module TradeOps.Reports.StatementPositions

open System
open TradeOps.Types
open TradeOps.Processing

//-------------------------------------------------------------------------------------------------

type PositionActive =
    { Sequence        : int
      Date            : DateTime
      IssueId         : int
      Ticker          : string
      Direction       : string
      Shares          : int
      Basis           : decimal }

type PositionClosed =
    { Sequence        : int
      Date            : DateTime
      IssueId         : int
      Ticker          : string
      Direction       : string
      Shares          : int
      Basis           : decimal
      Price           : decimal
      OpeningSequence : int
      OpeningDate     : DateTime }

type Model =
    { PositionsActive : PositionActive[]
      PositionsClosed : PositionClosed[] }

//-------------------------------------------------------------------------------------------------

let render (statement : Statement.Model) =

    let mapPositionsActive (item : Statement.PositionActive) : PositionActive =

        { Sequence        = item.Sequence
          Date            = item.Date
          IssueId         = item.IssueId
          Ticker          = item.IssueId |> mapTicker
          Direction       = item.Direction |> sprintf "%A"
          Shares          = item.Shares
          Basis           = item.Basis }

    let mapPositionsClosed (item : Statement.PositionClosed) : PositionClosed =

        { Sequence        = item.Sequence
          Date            = item.Date
          IssueId         = item.IssueId
          Ticker          = item.IssueId |> mapTicker
          Direction       = item.Direction |> sprintf "%A"
          Shares          = item.Shares
          Basis           = item.Basis
          Price           = item.Price
          OpeningSequence = item.OpeningSequence
          OpeningDate     = item.OpeningDate }

    let positionsActive =
        statement.PositionsActive
        |> Seq.map mapPositionsActive
        |> Seq.sortBy (fun x -> x.IssueId, x.Sequence)
        |> Seq.toArray

    let positionsClosed =
        statement.PositionsClosed
        |> Seq.map mapPositionsClosed
        |> Seq.sortBy (fun x -> x.Sequence, x.OpeningSequence)
        |> Seq.toArray

    { PositionsActive = positionsActive
      PositionsClosed = positionsClosed }
