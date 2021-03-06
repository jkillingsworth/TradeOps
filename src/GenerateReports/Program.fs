﻿module Program

open System
open FSharp.Control.Reactive
open TradeOps
open TradeOps.Types
open TradeOps.Reports

//-------------------------------------------------------------------------------------------------

let generateReports dateFinal =

    let intermediates =
        dateFinal
        |> Date.generateDates
        |> Observable.map Processing.getAdjustments
        |> Observable.scanInit Intermediate.empty Processing.computeIntermediate
        |> Observable.publish

    use subscription =
        intermediates
        |> Observable.last
        |> Observable.map IntermediateTransactions.render
        |> Observable.subscribe Output.writeIntermediateTransactions

    use subscription =
        intermediates
        |> Observable.last
        |> Observable.map IntermediateStops.render
        |> Observable.subscribe Output.writeIntermediateStops

    use subscription =
        intermediates
        |> Observable.last
        |> Observable.map IntermediatePositions.render
        |> Observable.subscribe Output.writeIntermediatePositions

    use subscription =
        intermediates
        |> Observable.fold Performance.render Performance.empty
        |> Observable.subscribe Output.writePerformance

    use connection =
        intermediates
        |> Observable.connect

    ()

//-------------------------------------------------------------------------------------------------

[<EntryPoint>]
let main = function

    | [| date |]
        ->
        generateReports <| DateTime.ParseExact(date, "d", null)
        0

    | _ ->
        generateReports <| Date.getMaximumDate()
        0
