module TradeOps.Reports.IntermediatePositions

open System
open TradeOps
open TradeOps.Types
open TradeOps.Processing

//-------------------------------------------------------------------------------------------------

type PositionActiveToday =
    { Sequence             : int
      Date                 : DateTime
      IssueId              : int
      Ticker               : string
      Direction            : string
      Shares               : int
      Basis                : decimal
      Divid                : decimal
      Final                : decimal
      Upper                : decimal
      Lower                : decimal
      DeltaDivid           : decimal
      DeltaFinal           : decimal }

type PositionClosedToday =
    { Reference            : int
      Sequence             : int
      Date                 : DateTime
      IssueId              : int
      Ticker               : string
      Direction            : string
      Shares               : int
      Basis                : decimal
      Divid                : decimal
      Final                : decimal
      Upper                : decimal
      Lower                : decimal
      DeltaDivid           : decimal
      DeltaFinal           : decimal }

type PositionClosedPrior =
    { Reference            : int
      Sequence             : int
      Date                 : DateTime
      IssueId              : int
      Ticker               : string
      Direction            : string
      Shares               : int
      Basis                : decimal
      Divid                : decimal
      Final                : decimal }

type Model =
    { PositionsActiveToday : PositionActiveToday[]
      PositionsClosedToday : PositionClosedToday[]
      PositionsClosedPrior : PositionClosedPrior[] }

//-------------------------------------------------------------------------------------------------

let render (intermediate : Intermediate.Model) =

    let mapPositionsActiveToday (item : Intermediate.PositionActiveToday) : PositionActiveToday =

        { Sequence        = item.Sequence
          Date            = item.Date
          IssueId         = item.IssueId
          Ticker          = item.IssueId |> mapTicker
          Direction       = item.Direction |> sprintf "%A"
          Shares          = item.Shares
          Basis           = item.Basis
          Divid           = item.Divid
          Final           = item.Final
          Upper           = item.Upper
          Lower           = item.Lower
          DeltaDivid      = item.DeltaDivid
          DeltaFinal      = item.DeltaFinal }

    let mapPositionsClosedToday (item : Intermediate.PositionClosedToday) : PositionClosedToday =

        { Reference       = item.Reference
          Sequence        = item.Sequence
          Date            = item.Date
          IssueId         = item.IssueId
          Ticker          = item.IssueId |> mapTicker
          Direction       = item.Direction |> sprintf "%A"
          Shares          = item.Shares
          Basis           = item.Basis
          Divid           = item.Divid
          Final           = item.Final
          Upper           = item.Upper
          Lower           = item.Lower
          DeltaDivid      = item.DeltaDivid
          DeltaFinal      = item.DeltaFinal }

    let mapPositionsClosedPrior (item : Intermediate.PositionClosedPrior) : PositionClosedPrior =

        { Reference       = item.Reference
          Sequence        = item.Sequence
          Date            = item.Date
          IssueId         = item.IssueId
          Ticker          = item.IssueId |> mapTicker
          Direction       = item.Direction |> sprintf "%A"
          Shares          = item.Shares
          Basis           = item.Basis
          Divid           = item.Divid
          Final           = item.Final }

    let positionsActiveToday =
        intermediate.PositionsActiveToday
        |> Seq.map mapPositionsActiveToday
        |> Seq.sortBy (fun x -> x.IssueId, x.Sequence)
        |> Seq.toArray

    let positionsClosedToday =
        intermediate.PositionsClosedToday
        |> Seq.map mapPositionsClosedToday
        |> Seq.sortBy (fun x -> x.Sequence, x.Reference)
        |> Seq.toArray

    let positionsClosedPrior =
        intermediate.PositionsClosedPrior
        |> Seq.map mapPositionsClosedPrior
        |> Seq.sortBy (fun x -> x.Sequence, x.Reference)
        |> Seq.toArray

    { PositionsActiveToday = positionsActiveToday
      PositionsClosedToday = positionsClosedToday
      PositionsClosedPrior = positionsClosedPrior }
