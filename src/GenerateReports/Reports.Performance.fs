module TradeOps.Reports.Performance

open System
open TradeOps.Types
open TradeOps.Intermediate

//-------------------------------------------------------------------------------------------------

type Summary =
    { Date  : DateTime
      Final : decimal
      Upper : decimal
      Lower : decimal }

type Model =
    { Summaries : Summary[] }

let empty =
    { Summaries = Array.empty }

//-------------------------------------------------------------------------------------------------

let private computeValue (value, basis, divid, shares, direction) =
    let value = value - basis
    let value = value + divid
    let value = value * decimal shares
    match direction with
    | Bullish -> +value
    | Bearish -> -value

let private computeTotal positions mapping =
    positions
    |> Seq.map mapping
    |> Seq.map computeValue
    |> Seq.sum

//-------------------------------------------------------------------------------------------------

let private computeActiveToday intermediate =

    let mapping getValue (position : PositionActiveToday) =
        let value = position |> getValue
        let basis = position.Basis
        let divid = position.Divid
        (value, basis, divid, position.Shares, position.Direction)

    mapping >> computeTotal intermediate.PositionsActiveToday

let private computeClosedToday intermediate =

    let mapping getValue (position : PositionClosedToday) =
        let value = position |> getValue
        let basis = position.Basis
        let divid = position.Divid
        (value, basis, divid, position.Shares, position.Direction)

    mapping >> computeTotal intermediate.PositionsClosedToday

let private computeClosedPrior intermediate =

    let mapping getValue (position : PositionClosedPrior) =
        let value = position |> getValue
        let basis = position.Basis
        let divid = position.Divid
        (value, basis, divid, position.Shares, position.Direction)

    mapping >> computeTotal intermediate.PositionsClosedPrior

//-------------------------------------------------------------------------------------------------

let render model intermediate =

    let finalActiveToday = computeActiveToday intermediate (fun x -> x.Final)
    let upperActiveToday = computeActiveToday intermediate (fun x -> x.Upper)
    let lowerActiveToday = computeActiveToday intermediate (fun x -> x.Lower)

    let finalClosedToday = computeClosedToday intermediate (fun x -> x.Final)
    let upperClosedToday = computeClosedToday intermediate (fun x -> x.Upper)
    let lowerClosedToday = computeClosedToday intermediate (fun x -> x.Lower)

    let finalClosedPrior = computeClosedPrior intermediate (fun x -> x.Final)

    let summary =
        { Date  = intermediate.Date
          Final = finalActiveToday + finalClosedToday + finalClosedPrior
          Upper = upperActiveToday + upperClosedToday + finalClosedPrior
          Lower = lowerActiveToday + lowerClosedToday + finalClosedPrior }

    { model with Summaries = Array.append model.Summaries [| summary |] }
