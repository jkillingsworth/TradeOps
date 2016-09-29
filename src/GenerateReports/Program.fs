module Program

open System
open FSharp.Control.Reactive
open TradeOps
open TradeOps.Types
open TradeOps.Reports

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
        |> Observable.map StatementTransactions.render
        |> Observable.subscribe Output.writeStatementTransactions

    use subscription =
        statements
        |> Observable.last
        |> Observable.map StatementStops.render
        |> Observable.subscribe Output.writeStatementStops

    use subscription =
        statements
        |> Observable.last
        |> Observable.map StatementPositions.render
        |> Observable.subscribe Output.writeStatementPositions

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
