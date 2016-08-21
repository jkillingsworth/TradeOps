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
        |> Observable.fold Processing.renderTransactionListing TransactionListing.empty
        |> Observable.subscribe Output.writeTransactionListing

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
