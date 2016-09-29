module Program

open System
open FSharp.Control.Reactive
open TradeOps
open TradeOps.Types
open TradeOps.Reports

//-------------------------------------------------------------------------------------------------

let generateReports dateFinal =

    let intermediates =
        dateFinal
        |> Processing.generateDates
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
        generateReports <| DateTime.Now
        0
