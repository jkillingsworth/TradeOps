module TradeOps.Output

open System
open System.IO
open TradeOps.Types
open TradeOps.Models

//-------------------------------------------------------------------------------------------------

let private folder = Environment.GetEnvironmentVariable("UserProfile") + @"\Desktop\Output\"

//-------------------------------------------------------------------------------------------------

let writeTransactionListing (model : TransactionListing.Model) =

    let contents =
        [ [| "Divids" |]
          model.Divids |> Array.map (sprintf "%A")
          [| "Splits" |]
          model.Splits |> Array.map (sprintf "%A")
          [| "Trades" |]
          model.Trades |> Array.map (sprintf "%A") ]

    let contents = Array.concat contents

    let path = Path.Combine(folder, "TransactionListing.txt")
    Directory.CreateDirectory(folder) |> ignore
    File.WriteAllLines(path, contents)
