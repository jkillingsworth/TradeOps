﻿module TradeOps.Processing

open System
open FSharp.Control.Reactive
open TradeOps.Types
open TradeOps.Models

//-------------------------------------------------------------------------------------------------

let private dateStart = Persistence.selectStartDate ()
let private holidays = Persistence.selectHolidays ()
let private issues = Persistence.selectIssues ()

let private addDays days (date : DateTime) = date.AddDays(float days)
let private isWeekendSat (date : DateTime) = date.DayOfWeek = DayOfWeek.Saturday
let private isWeekendSun (date : DateTime) = date.DayOfWeek = DayOfWeek.Sunday

let private isWeekendOrHoliday = function
    | date when isWeekendSat date -> true
    | date when isWeekendSun date -> true
    | date -> Array.contains date holidays

let generateDates dateFinal =
    id
    |> Observable.generate dateStart (fun date -> date <= dateFinal) (addDays +1)
    |> Observable.filter (isWeekendOrHoliday >> not)

//-------------------------------------------------------------------------------------------------

let private mapSequence = function
    | Divid transaction -> transaction.Sequence
    | Split transaction -> transaction.Sequence
    | Trade transaction -> transaction.Sequence

let private mapDate = function
    | Divid transaction -> transaction.Date
    | Split transaction -> transaction.Date
    | Trade transaction -> transaction.Date

let private mapIssueId = function
    | Divid transaction -> transaction.IssueId
    | Split transaction -> transaction.IssueId
    | Trade transaction -> transaction.IssueId

let private mapTicker issueId =
    let issue = issues |> Array.find (fun x -> x.IssueId = issueId)
    issue.Ticker

//-------------------------------------------------------------------------------------------------

let getOperations date : Operations =

    let transactions =
        [ Persistence.selectTransactionsDivid date
          Persistence.selectTransactionsSplit date
          Persistence.selectTransactionsTrade date ]

    let transactions =
        transactions
        |> Array.concat
        |> Array.sortBy mapSequence

    let stoplosses = Persistence.selectStoplosses date

    { Date         = date
      Transactions = transactions
      Stoplosses   = stoplosses }

//-------------------------------------------------------------------------------------------------

let private processDivid (statement : Statement.Model) (divid : TransactionDivid) =

    statement

let private processSplit (statement : Statement.Model) (split : TransactionSplit) =

    statement

let rec private processTrade (statement : Statement.Model) (trade : TransactionTrade) =

    let shares =
        statement.PositionsActive
        |> Array.ofSeq
        |> Array.where (fun x -> x.IssueId = trade.IssueId)
        |> Array.sumBy (fun x -> x.Shares)

    let processTakePosition (statement : Statement.Model) (trade : TransactionTrade) =

        let positionActive : Statement.PositionActive =
            { Sequence     = trade.Sequence
              Date         = trade.Date
              IssueId      = trade.IssueId
              Shares       = trade.Shares
              CostBasis    = trade.Price }

        { statement with
            PositionsActive = statement.PositionsActive |> Set.add positionActive }

    let rec processExitPosition (statement : Statement.Model) (trade : TransactionTrade) =

        let positionActive =
            statement.PositionsActive
            |> Seq.sortBy (fun x -> x.Sequence)
            |> Seq.find (fun x -> x.IssueId = trade.IssueId)

        let sharesToClose =
            match positionActive.Shares with
            | shares when shares > 0 -> min positionActive.Shares -trade.Shares
            | shares when shares < 0 -> max positionActive.Shares -trade.Shares
            | _ -> failwith "Unexpected condition."

        let positionClosed : Statement.PositionClosed =
            { Sequence      = trade.Sequence
              Date          = trade.Date
              IssueId       = positionActive.IssueId
              Shares        = sharesToClose
              CostBasis     = positionActive.CostBasis
              ExitPrice     = trade.Price
              EntrySequence = positionActive.Sequence
              EntryDate     = positionActive.Date }

        match sharesToClose with

        | shares when (shares > 0 && shares < -trade.Shares) || (shares < 0 && shares > -trade.Shares)
            ->
            let positionsClosed = statement.PositionsClosed |> Set.add positionClosed
            let positionsActive = statement.PositionsActive |> Set.remove positionActive

            let statement =
                { statement with
                    PositionsActive = positionsActive
                    PositionsClosed = positionsClosed }

            let trade =
                { trade with Shares = trade.Shares + positionActive.Shares }

            processExitPosition statement trade

        | shares when (shares > 0 && shares < positionActive.Shares) || (shares < 0 && shares > positionActive.Shares)
            ->
            let positionsClosed = statement.PositionsClosed |> Set.add positionClosed
            let positionsActive = statement.PositionsActive |> Set.remove positionActive
            let positionActive = { positionActive with Shares = positionActive.Shares + trade.Shares }
            let positionsActive = positionsActive |> Set.add positionActive

            let statement =
                { statement with
                    PositionsActive = positionsActive
                    PositionsClosed = positionsClosed }

            statement

        | _
            ->
            let positionsClosed = statement.PositionsClosed |> Set.add positionClosed
            let positionsActive = statement.PositionsActive |> Set.remove positionActive

            let statement =
                { statement with
                    PositionsActive = positionsActive
                    PositionsClosed = positionsClosed }

            statement

    match shares with
    | shares when trade.Shares > 0 && shares >= 0 -> processTakePosition statement trade
    | shares when trade.Shares < 0 && shares <= 0 -> processTakePosition statement trade
    | shares when trade.Shares < 0 && shares >= -trade.Shares -> processExitPosition statement trade
    | shares when trade.Shares > 0 && shares <= -trade.Shares -> processExitPosition statement trade
    | _ -> failwith "Unexpected condition."

let private processTransaction (statement : Statement.Model) = function
    | Divid transaction -> processDivid statement transaction
    | Split transaction -> processSplit statement transaction
    | Trade transaction -> processTrade statement transaction

let private applyTransaction (statement : Statement.Model) transaction =

    let statement = processTransaction statement transaction
    let transactions = [| transaction |] |> Array.append statement.Transactions
    { statement with Transactions = transactions }

let computeStatement (statement : Statement.Model) operations : Statement.Model =

    let updateStop stops stoploss = stops |> Map.add stoploss.IssueId stoploss.Price

    let statement = { statement with Date  = operations.Date }
    let statement = { statement with Stops = operations.Stoplosses |> Array.fold updateStop statement.Stops }

    operations.Transactions
    |> Array.fold applyTransaction statement

//-------------------------------------------------------------------------------------------------

let renderStatementTransactions (statement : Statement.Model) =

    let mapDivid (item : TransactionDivid) : StatementTransactions.Divid =

        { Sequence = item.Sequence
          Date     = item.Date
          IssueId  = item.IssueId
          Ticker   = item.IssueId |> mapTicker
          Amount   = item.Amount
          PayDate  = item.PayDate }

    let mapSplit (item : TransactionSplit) : StatementTransactions.Split =

        { Sequence = item.Sequence
          Date     = item.Date
          IssueId  = item.IssueId
          Ticker   = item.IssueId |> mapTicker
          New      = item.New
          Old      = item.Old }

    let mapTrade (item : TransactionTrade) : StatementTransactions.Trade =

        { Sequence = item.Sequence
          Date     = item.Date
          IssueId  = item.IssueId
          Ticker   = item.IssueId |> mapTicker
          Shares   = item.Shares
          Price    = item.Price }

    let accumulate (model : StatementTransactions.Model) = function
        | Divid transaction -> { model with Divids = Array.append model.Divids [| mapDivid transaction |] }
        | Split transaction -> { model with Splits = Array.append model.Splits [| mapSplit transaction |] }
        | Trade transaction -> { model with Trades = Array.append model.Trades [| mapTrade transaction |] }

    let model : StatementTransactions.Model =
        { Divids = Array.empty
          Splits = Array.empty
          Trades = Array.empty }

    statement.Transactions |> Array.fold accumulate model

//-------------------------------------------------------------------------------------------------

let renderStatementPositions (statement : Statement.Model) : StatementPositions.Model =

    let mapPositionsActive (item : Statement.PositionActive) : StatementPositions.PositionActive =

        { Sequence      = item.Sequence
          Date          = item.Date
          IssueId       = item.IssueId
          Ticker        = item.IssueId |> mapTicker
          Shares        = item.Shares
          CostBasis     = item.CostBasis }

    let mapPositionsClosed (item : Statement.PositionClosed) : StatementPositions.PositionClosed =

        { Sequence      = item.Sequence
          Date          = item.Date
          IssueId       = item.IssueId
          Ticker        = item.IssueId |> mapTicker
          Shares        = item.Shares
          CostBasis     = item.CostBasis
          ExitPrice     = item.ExitPrice
          EntrySequence = item.EntrySequence
          EntryDate     = item.EntryDate }

    let positionsActive =
        statement.PositionsActive
        |> Seq.map mapPositionsActive
        |> Seq.sortBy (fun x -> x.IssueId, x.Sequence)
        |> Seq.toArray

    let positionsClosed =
        statement.PositionsClosed
        |> Seq.map mapPositionsClosed
        |> Seq.sortBy (fun x -> x.Sequence, x.EntrySequence)
        |> Seq.toArray

    { PositionsActive = positionsActive
      PositionsClosed = positionsClosed }

//-------------------------------------------------------------------------------------------------

let renderStatementStops (statement : Statement.Model) : StatementStops.Model =

    let mapStop (issueId, price) : StatementStops.Stop =

        { IssueId = issueId
          Ticker  = issueId |> mapTicker
          Price   = price }

    let isActive (issueId, _) =
        statement.PositionsActive |> Set.exists (fun x -> x.IssueId = issueId)

    let stopsActive =
        statement.Stops
        |> Map.toSeq
        |> Seq.filter (isActive >> id)
        |> Seq.map mapStop
        |> Seq.sortBy (fun x -> x.IssueId)
        |> Seq.toArray

    let stopsClosed =
        statement.Stops
        |> Map.toSeq
        |> Seq.filter (isActive >> not)
        |> Seq.map mapStop
        |> Seq.sortBy (fun x -> x.IssueId)
        |> Seq.toArray

    { StopsActive = stopsActive
      StopsClosed = stopsClosed }
