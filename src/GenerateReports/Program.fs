module Program

open System
open FSharp.Control.Reactive
open TradeOps

//-------------------------------------------------------------------------------------------------

let generateReports dateFinal =

    let dates = Processing.generateDates dateFinal |> Observable.publish

    use subscription =
        dates
        |> Observable.fold Processing.renderTransactions [||]
        |> Observable.subscribe Output.writeTransactions

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
