module TradeOps.Reports.StatementPositions

open System
open TradeOps.Types
open TradeOps.Processing

//-------------------------------------------------------------------------------------------------

type PositionActiveToday =
    { Sequence        : int
      Date            : DateTime
      IssueId         : int
      Ticker          : string
      Direction       : string
      Shares          : int
      Basis           : decimal }

type PositionClosedToday =
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

type PositionClosedPrior =
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
    { PositionsActiveToday : PositionActiveToday[]
      PositionsClosedToday : PositionClosedToday[]
      PositionsClosedPrior : PositionClosedPrior[] }

//-------------------------------------------------------------------------------------------------

let render (statement : Statement.Model) =

    let mapPositionsActiveToday (item : Statement.PositionActiveToday) : PositionActiveToday =

        { Sequence        = item.Sequence
          Date            = item.Date
          IssueId         = item.IssueId
          Ticker          = item.IssueId |> mapTicker
          Direction       = item.Direction |> sprintf "%A"
          Shares          = item.Shares
          Basis           = item.Basis }

    let mapPositionsClosedToday (item : Statement.PositionClosedToday) : PositionClosedToday =

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

    let mapPositionsClosedPrior (item : Statement.PositionClosedPrior) : PositionClosedPrior =

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

    let positionsActiveToday =
        statement.PositionsActiveToday
        |> Seq.map mapPositionsActiveToday
        |> Seq.sortBy (fun x -> x.IssueId, x.Sequence)
        |> Seq.toArray

    let positionsClosedToday =
        statement.PositionsClosedToday
        |> Seq.map mapPositionsClosedToday
        |> Seq.sortBy (fun x -> x.Sequence, x.OpeningSequence)
        |> Seq.toArray

    let positionsClosedPrior =
        statement.PositionsClosedPrior
        |> Seq.map mapPositionsClosedPrior
        |> Seq.sortBy (fun x -> x.Sequence, x.OpeningSequence)
        |> Seq.toArray

    { PositionsActiveToday = positionsActiveToday
      PositionsClosedToday = positionsClosedToday
      PositionsClosedPrior = positionsClosedPrior }
