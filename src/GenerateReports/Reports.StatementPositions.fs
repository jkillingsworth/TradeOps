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

let render (statement : Statement.Model) =

    let mapPositionsActive (item : Statement.PositionActive) : PositionActive =

        { Sequence      = item.Sequence
          Date          = item.Date
          IssueId       = item.IssueId
          Ticker        = item.IssueId |> mapTicker
          Shares        = item.Shares
          CostBasis     = item.CostBasis }

    let mapPositionsClosed (item : Statement.PositionClosed) : PositionClosed =

        { Sequence      = item.Sequence
          Date          = item.Date
          IssueId       = item.IssueId
          Ticker        = item.IssueId |> mapTicker
          Shares        = item.Shares
          CostBasis     = item.CostBasis
          ExitPrice     = item.ExitPrice
          EntrySequence = item.EntrySequence
          EntryDate     = item.EntryDate }

    let positionsActive =
        statement.PositionsActive
        |> Seq.map mapPositionsActive
        |> Seq.sortBy (fun x -> x.IssueId, x.Sequence)
        |> Seq.toArray

    let positionsClosed =
        statement.PositionsClosed
        |> Seq.map mapPositionsClosed
        |> Seq.sortBy (fun x -> x.Sequence, x.EntrySequence)
        |> Seq.toArray

    { PositionsActive = positionsActive
      PositionsClosed = positionsClosed }
