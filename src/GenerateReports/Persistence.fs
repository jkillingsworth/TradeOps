module TradeOps.Persistence

open FSharp.Data
open TradeOps.Types

//-------------------------------------------------------------------------------------------------

[<Literal>]
let private configFile = @"..\..\private\App.config"

[<Literal>]
let private connectionNameQuotes = @"name=databaseQuotes"

[<Literal>]
let private connectionNameTrades = @"name=databaseTrades"

//-------------------------------------------------------------------------------------------------

module private SelectHolidays =

    [<Literal>]
    let private sql = @"..\..\sql\Quotes\SelectHolidays.sql"

    type CommandProvider = SqlCommandProvider<sql, connectionNameQuotes, ConfigFile = configFile>

    let execute () =
        use command = new CommandProvider()
        let records = command.Execute()
        records
        |> Seq.toArray

let selectHolidays =
    SelectHolidays.execute

//-------------------------------------------------------------------------------------------------

module private SelectIssues =

    [<Literal>]
    let private sql = @"..\..\sql\Quotes\SelectIssues.sql"

    type CommandProvider = SqlCommandProvider<sql, connectionNameQuotes, ConfigFile = configFile>

    let private ofRecord (record : CommandProvider.Record) : Issue =

        { IssueId = record.IssueId
          Ticker  = record.Ticker }

    let execute () =
        use command = new CommandProvider()
        let records = command.Execute()
        records
        |> Seq.map ofRecord
        |> Seq.toArray

let selectIssues =
    SelectIssues.execute

//-------------------------------------------------------------------------------------------------

module private SelectQuote =

    [<Literal>]
    let private sql = @"..\..\sql\Quotes\SelectQuote.sql"

    type CommandProvider = SqlCommandProvider<sql, connectionNameQuotes, ConfigFile = configFile>

    let private ofRecord (record : CommandProvider.Record) : Quote =

        { IssueId = record.IssueId
          Date    = record.Date
          Close   = record.Close }

    let execute issueId date =
        use command = new CommandProvider()
        let records = command.Execute(issueId, date)
        records
        |> Seq.map ofRecord
        |> Seq.exactlyOne

let selectQuote =
    SelectQuote.execute

//-------------------------------------------------------------------------------------------------

module private SelectStartDate =

    [<Literal>]
    let private sql = @"..\..\sql\Trades\SelectStartDate.sql"

    type CommandProvider = SqlCommandProvider<sql, connectionNameTrades, ConfigFile = configFile>

    let execute () =
        use command = new CommandProvider()
        let records = command.Execute()
        records
        |> Seq.exactlyOne

let selectStartDate =
    SelectStartDate.execute

//-------------------------------------------------------------------------------------------------

module private SelectStoplosses =

    [<Literal>]
    let private sql = @"..\..\sql\Trades\SelectStoplosses.sql"

    type CommandProvider = SqlCommandProvider<sql, connectionNameTrades, ConfigFile = configFile>

    let private ofRecord (record : CommandProvider.Record) : Stoploss =

        { Date    = record.Date
          IssueId = record.IssueId
          Price   = record.Price }

    let execute date =
        use command = new CommandProvider()
        let records = command.Execute(date)
        records
        |> Seq.map ofRecord
        |> Seq.toArray

let selectStoplosses =
    SelectStoplosses.execute

//-------------------------------------------------------------------------------------------------

module private SelectTransactionsDivid =

    [<Literal>]
    let private sql = @"..\..\sql\Trades\SelectTransactionsDivid.sql"

    type CommandProvider = SqlCommandProvider<sql, connectionNameTrades, ConfigFile = configFile>

    let private ofRecord (record : CommandProvider.Record) : TransactionDivid =

        let mapDirection = function
            | "Bullish" -> Bullish
            | "Bearish" -> Bearish
            | erroneous -> failwith ("Invalid direction type: " + erroneous)

        { Sequence = record.Sequence
          Date     = record.Date
          IssueId  = record.IssueId
          Direction = record.Direction |> mapDirection
          Shares   = record.Shares
          Amount   = record.Amount
          PayDate  = record.PayDate }

    let execute date =
        use command = new CommandProvider()
        let records = command.Execute(date)
        records
        |> Seq.map ofRecord
        |> Seq.map Divid
        |> Seq.toArray

let selectTransactionsDivid =
    SelectTransactionsDivid.execute

//-------------------------------------------------------------------------------------------------

module private SelectTransactionsSplit =

    [<Literal>]
    let private sql = @"..\..\sql\Trades\SelectTransactionsSplit.sql"

    type CommandProvider = SqlCommandProvider<sql, connectionNameTrades, ConfigFile = configFile>

    let private ofRecord (record : CommandProvider.Record) : TransactionSplit =

        let mapDirection = function
            | "Bullish" -> Bullish
            | "Bearish" -> Bearish
            | erroneous -> failwith ("Invalid direction type: " + erroneous)

        { Sequence  = record.Sequence
          Date      = record.Date
          IssueId   = record.IssueId
          Direction = record.Direction |> mapDirection
          Shares    = record.Shares
          New       = record.New
          Old       = record.Old }

    let execute date =
        use command = new CommandProvider()
        let records = command.Execute(date)
        records
        |> Seq.map ofRecord
        |> Seq.map Split
        |> Seq.toArray

let selectTransactionsSplit =
    SelectTransactionsSplit.execute

//-------------------------------------------------------------------------------------------------

module private SelectTransactionsTrade =

    [<Literal>]
    let private sql = @"..\..\sql\Trades\SelectTransactionsTrade.sql"

    type CommandProvider = SqlCommandProvider<sql, connectionNameTrades, ConfigFile = configFile>

    let private ofRecord (record : CommandProvider.Record) : TransactionTrade =

        let mapDirection = function
            | "Bullish" -> Bullish
            | "Bearish" -> Bearish
            | erroneous -> failwith ("Invalid direction type: " + erroneous)

        let mapOperation = function
            | "Opening" -> Opening
            | "Closing" -> Closing
            | erroneous -> failwith ("Invalid operation type: " + erroneous)

        { Sequence  = record.Sequence
          Date      = record.Date
          IssueId   = record.IssueId
          Direction = record.Direction |> mapDirection
          Shares    = record.Shares
          Operation = record.Operation |> mapOperation
          Price     = record.Price }

    let execute date =
        use command = new CommandProvider()
        let records = command.Execute(date)
        records
        |> Seq.map ofRecord
        |> Seq.map Trade
        |> Seq.toArray

let selectTransactionsTrade =
    SelectTransactionsTrade.execute
