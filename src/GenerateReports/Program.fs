module Program

open System
open FSharp.Control.Reactive
open TradeOps
open TradeOps.Types
open TradeOps.Models

//-------------------------------------------------------------------------------------------------

let generateReports dateFinal =

    let statements =
        dateFinal
        |> Processing.generateDates
        |> Observable.map Processing.getOperations
        |> Observable.scanInit Statement.empty Processing.computeStatement
        |> Observable.publish

    use subscription =
        statements
        |> Observable.last
        |> Observable.map Processing.renderStatementTransactions
        |> Observable.subscribe Output.writeStatementTransactions

    use subscription =
        statements
        |> Observable.last
        |> Observable.map Processing.renderStatementPositions
        |> Observable.subscribe Output.writeStatementPositions

    use subscription =
        statements
        |> Observable.last
        |> Observable.map Processing.renderStatementStops
        |> Observable.subscribe Output.writeStatementStops

    use connection =
        statements
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
