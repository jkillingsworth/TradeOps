module TradeOps.Output

open System
open System.IO
open TradeOps.Types

//-------------------------------------------------------------------------------------------------

let private folder = Environment.GetEnvironmentVariable("UserProfile") + @"\Desktop\Output\"

let private mapTransaction = function
    | Divid transaction -> sprintf "%A" transaction
    | Split transaction -> sprintf "%A" transaction
    | Trade transaction -> sprintf "%A" transaction

let writeTransactions transactions =

    let contents = transactions |> Seq.map mapTransaction
    let path = Path.Combine(folder, "transactions.txt")
    Directory.CreateDirectory(folder) |> ignore
    File.WriteAllLines(path, contents)
