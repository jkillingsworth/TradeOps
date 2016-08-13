module Program

open System
open TradeOps

//-------------------------------------------------------------------------------------------------

let generateReports dateFinal =

    dateFinal
    |> Processing.generateDates
    |> Seq.map Processing.getTransactions
    |> Seq.collect id
    |> Output.writeTransactions

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
