module Program

open System
open FSharp.Control.Reactive
open TradeOps
open TradeOps.Models

//-------------------------------------------------------------------------------------------------

let generateReports dateFinal =

    let dates =
        dateFinal
        |> Processing.generateDates
        |> Observable.map Processing.getOperations
        |> Observable.publish

    use subscription =
        dates
        |> Observable.fold Processing.renderTransactions [||]
        |> Observable.subscribe Output.writeTransactions

    use subscription =
        dates
        |> Observable.fold Processing.renderTransactionListing TransactionListing.empty
        |> Observable.subscribe Output.writeTransactionListing

    use connection =
        dates
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
