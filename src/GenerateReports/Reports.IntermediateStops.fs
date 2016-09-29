module TradeOps.Reports.IntermediateStops

open System
open TradeOps
open TradeOps.Types
open TradeOps.Processing

//-------------------------------------------------------------------------------------------------

type Stop =
    { IssueId     : int
      Ticker      : string
      Price       : decimal }

type Model =
    { StopsActive : Stop[]
      StopsClosed : Stop[] }

//-------------------------------------------------------------------------------------------------

let render (intermediate : Intermediate.Model) =

    let mapStop (issueId, price) : Stop =

        { IssueId = issueId
          Ticker  = issueId |> mapTicker
          Price   = price }

    let isActive (issueId, _) =
        intermediate.PositionsActiveToday |> Set.exists (fun x -> x.IssueId = issueId)

    let stopsActive =
        intermediate.Stops
        |> Map.toSeq
        |> Seq.filter (isActive >> id)
        |> Seq.map mapStop
        |> Seq.sortBy (fun x -> x.IssueId)
        |> Seq.toArray

    let stopsClosed =
        intermediate.Stops
        |> Map.toSeq
        |> Seq.filter (isActive >> not)
        |> Seq.map mapStop
        |> Seq.sortBy (fun x -> x.IssueId)
        |> Seq.toArray

    { StopsActive = stopsActive
      StopsClosed = stopsClosed }
