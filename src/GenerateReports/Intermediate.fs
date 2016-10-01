module TradeOps.Intermediate

open System
open TradeOps.Types

//-------------------------------------------------------------------------------------------------

type PositionActiveToday =
    { Sequence             : int
      Date                 : DateTime
      IssueId              : int
      Direction            : Direction
      Shares               : int
      Basis                : decimal
      Divid                : decimal
      Final                : decimal
      Upper                : decimal
      Lower                : decimal
      DeltaDivid           : decimal
      DeltaFinal           : decimal }

type PositionClosedToday =
    { Reference            : int
      Sequence             : int
      Date                 : DateTime
      IssueId              : int
      Direction            : Direction
      Shares               : int
      Basis                : decimal
      Divid                : decimal
      Final                : decimal
      Upper                : decimal
      Lower                : decimal
      DeltaDivid           : decimal
      DeltaFinal           : decimal }

type PositionClosedPrior =
    { Reference            : int
      Sequence             : int
      Date                 : DateTime
      IssueId              : int
      Direction            : Direction
      Shares               : int
      Basis                : decimal
      Divid                : decimal
      Final                : decimal }

type Model =
    { Date                 : DateTime
      Transactions         : Transaction[]
      Stops                : Map<int, decimal>
      PositionsActiveToday : Set<PositionActiveToday>
      PositionsClosedToday : Set<PositionClosedToday>
      PositionsClosedPrior : Set<PositionClosedPrior> }

let empty =
    { Date                 = DateTime.MinValue
      Transactions         = Array.empty
      Stops                = Map.empty
      PositionsActiveToday = Set.empty
      PositionsClosedToday = Set.empty
      PositionsClosedPrior = Set.empty }

//-------------------------------------------------------------------------------------------------

let private processTradeOpening (intermediate : Model) (trade : TransactionTrade) =

    let positionActive : PositionActiveToday =

        { Sequence   = trade.Sequence
          Date       = trade.Date
          IssueId    = trade.IssueId
          Direction  = trade.Direction
          Shares     = trade.Shares
          Basis      = trade.Price
          Divid      = Decimal.Zero
          Final      = trade.Price
          Upper      = trade.Price
          Lower      = trade.Price
          DeltaDivid = Decimal.Zero
          DeltaFinal = Decimal.Zero }

    { intermediate with PositionsActiveToday = intermediate.PositionsActiveToday |> Set.add positionActive }

let private processTradeClosing (intermediate : Model) (trade : TransactionTrade) =

    let computeUpper quote =
        match trade.Direction, intermediate.Stops.[trade.IssueId] with
        | Bullish, stop -> quote.Hi
        | Bearish, stop -> quote.Hi |> min stop |> max trade.Price

    let computeLower quote =
        match trade.Direction, intermediate.Stops.[trade.IssueId] with
        | Bearish, stop -> quote.Lo
        | Bullish, stop -> quote.Lo |> max stop |> min trade.Price

    let updatePositions intermediate shares =

        let quote = Persistence.selectQuote trade.IssueId intermediate.Date

        let positionsActive = intermediate.PositionsActiveToday
        let positionsClosed = intermediate.PositionsClosedToday

        let positionToClose =
            intermediate.PositionsActiveToday
            |> Seq.sortBy (fun x -> x.Sequence)
            |> Seq.filter (fun x -> x.IssueId = trade.IssueId)
            |> Seq.filter (fun x -> x.Direction = trade.Direction)
            |> Seq.head

        let positionClosed : PositionClosedToday =

            { Reference  = positionToClose.Sequence
              Sequence   = trade.Sequence
              Date       = trade.Date
              IssueId    = trade.IssueId
              Direction  = trade.Direction
              Shares     = min shares positionToClose.Shares
              Basis      = positionToClose.Basis
              Divid      = positionToClose.Divid
              Final      = trade.Price
              Upper      = computeUpper quote
              Lower      = computeLower quote
              DeltaDivid = positionToClose.DeltaDivid
              DeltaFinal = trade.Price - positionToClose.Final }

        let reinstateIfSharesRemaining positionsActive =
            if (positionToClose.Shares > shares) then
                positionsActive |> Set.add { positionToClose with Shares = positionToClose.Shares - shares }
            else
                positionsActive

        let positionsClosed = positionsClosed |> Set.add positionClosed
        let positionsActive =
            positionsActive
            |> Set.remove positionToClose
            |> reinstateIfSharesRemaining

        { intermediate with
            PositionsActiveToday = positionsActive
            PositionsClosedToday = positionsClosed }, (shares - positionClosed.Shares)

    let rec loop intermediate = function
        | shares when shares = 0 -> intermediate
        | shares -> updatePositions intermediate shares ||> loop

    loop intermediate trade.Shares

//-------------------------------------------------------------------------------------------------

let private processDivid (intermediate : Model) (divid : TransactionDivid) =

    let adjustAmount value =
        value + divid.Amount

    let apply (position : PositionActiveToday) =
        { position with
            Divid = adjustAmount position.Divid
            DeltaDivid = adjustAmount position.DeltaDivid }

    let folder positionsActiveToday positionToApply =
        let positionsActiveToday = (positionToApply, positionsActiveToday) ||> Set.remove
        let positionToApply = apply positionToApply
        let positionsActiveToday = (positionToApply, positionsActiveToday) ||> Set.add
        positionsActiveToday

    let positionsActive =
        intermediate.PositionsActiveToday
        |> Seq.filter (fun x -> x.IssueId = divid.IssueId)
        |> Seq.filter (fun x -> x.Direction = divid.Direction)
        |> Seq.fold folder intermediate.PositionsActiveToday

    { intermediate with PositionsActiveToday = positionsActive }

let private processSplit (intermediate : Model) (split : TransactionSplit) =

    let adjustShares (value : int) =
        let ratio = decimal split.New / decimal split.Old
        let value = ratio * decimal value
        int value

    let adjustAmount (value : decimal) =
        let ratio = decimal split.Old / decimal split.New
        ratio * value

    let apply (position : PositionActiveToday) =
        { position with
            Shares = adjustShares position.Shares
            Basis = adjustAmount position.Basis
            Divid = adjustAmount position.Divid
            Final = adjustAmount position.Final
            Upper = adjustAmount position.Upper
            Lower = adjustAmount position.Lower
            DeltaDivid = adjustAmount position.DeltaDivid
            DeltaFinal = adjustAmount position.DeltaFinal }

    let folder positionsActiveToday positionToApply =
        let positionsActiveToday = (positionToApply, positionsActiveToday) ||> Set.remove
        let positionToApply = apply positionToApply
        let positionsActiveToday = (positionToApply, positionsActiveToday) ||> Set.add
        positionsActiveToday

    let positionsActive =
        intermediate.PositionsActiveToday
        |> Seq.filter (fun x -> x.IssueId = split.IssueId)
        |> Seq.filter (fun x -> x.Direction = split.Direction)
        |> Seq.fold folder intermediate.PositionsActiveToday

    { intermediate with PositionsActiveToday = positionsActive }

let private processTrade (intermediate : Model) (trade : TransactionTrade) =

    match trade.Operation with
    | Opening -> processTradeOpening intermediate trade
    | Closing -> processTradeClosing intermediate trade

let private processTransaction (intermediate : Model) = function
    | Divid transaction -> processDivid intermediate transaction
    | Split transaction -> processSplit intermediate transaction
    | Trade transaction -> processTrade intermediate transaction

//-------------------------------------------------------------------------------------------------

let beginDay (adjustments : Adjustments) (intermediate : Model) =

    let ofPositionsActiveToday (positionActiveToday : PositionActiveToday) =

        { positionActiveToday with
            DeltaDivid = Decimal.Zero
            DeltaFinal = Decimal.Zero }

    let ofPositionsClosedToday (positionClosedToday : PositionClosedToday) =

        { Reference = positionClosedToday.Reference
          Sequence  = positionClosedToday.Sequence
          Date      = positionClosedToday.Date
          IssueId   = positionClosedToday.IssueId
          Direction = positionClosedToday.Direction
          Shares    = positionClosedToday.Shares
          Basis     = positionClosedToday.Basis
          Divid     = positionClosedToday.Divid
          Final     = positionClosedToday.Final }

    let positionsActiveToday =
        intermediate.PositionsActiveToday
        |> Set.map ofPositionsActiveToday

    let positionsClosedToday = Set.empty
    let positionsClosedPrior =
        intermediate.PositionsClosedToday
        |> Set.map ofPositionsClosedToday
        |> Set.union intermediate.PositionsClosedPrior

    let adjustStops =
        let adjust stops (stoploss : Stoploss) = stops |> Map.add stoploss.IssueId stoploss.Price
        Array.fold adjust intermediate.Stops

    { intermediate with
        Date  = adjustments.Date
        Stops = adjustments.Stoplosses |> adjustStops
        PositionsActiveToday = positionsActiveToday
        PositionsClosedToday = positionsClosedToday
        PositionsClosedPrior = positionsClosedPrior }

//-------------------------------------------------------------------------------------------------

let applyTransactions (adjustments : Adjustments) (intermediate : Model) =

    let applyTransaction intermediate transaction =

        let intermediate = processTransaction intermediate transaction
        let transactions = [| transaction |] |> Array.append intermediate.Transactions
        { intermediate with Transactions = transactions }

    adjustments.Transactions |> Array.fold applyTransaction intermediate

//-------------------------------------------------------------------------------------------------

let closeDay (adjustments : Adjustments) (intermediate : Model) =

    let mapping (positionActiveToday : PositionActiveToday) =

        let quote = Persistence.selectQuote positionActiveToday.IssueId intermediate.Date

        { positionActiveToday with
            Final      = quote.Close
            Upper      = quote.Hi
            Lower      = quote.Lo
            DeltaFinal = quote.Close - positionActiveToday.Final }

    { intermediate with PositionsActiveToday = intermediate.PositionsActiveToday |> Set.map mapping }
