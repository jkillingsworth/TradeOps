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
      Final                : decimal
      Upper                : decimal
      Lower                : decimal
      Delta                : decimal }

type PositionClosedToday =
    { Reference            : int
      Sequence             : int
      Date                 : DateTime
      IssueId              : int
      Direction            : Direction
      Shares               : int
      Basis                : decimal
      Final                : decimal
      Upper                : decimal
      Lower                : decimal
      Delta                : decimal }

type PositionClosedPrior =
    { Reference            : int
      Sequence             : int
      Date                 : DateTime
      IssueId              : int
      Direction            : Direction
      Shares               : int
      Basis                : decimal
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

        { Sequence  = trade.Sequence
          Date      = trade.Date
          IssueId   = trade.IssueId
          Direction = trade.Direction
          Shares    = trade.Shares
          Basis     = trade.Price
          Final     = trade.Price
          Upper     = trade.Price
          Lower     = trade.Price
          Delta     = Decimal.Zero }

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

            { Reference = positionToClose.Sequence
              Sequence  = trade.Sequence
              Date      = trade.Date
              IssueId   = trade.IssueId
              Direction = trade.Direction
              Shares    = min shares positionToClose.Shares
              Basis     = positionToClose.Basis
              Final     = trade.Price
              Upper     = computeUpper quote
              Lower     = computeLower quote
              Delta     = trade.Price - positionToClose.Final }

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

    intermediate

let private processSplit (intermediate : Model) (split : TransactionSplit) =

    intermediate

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

    let mapping (positionClosedToday : PositionClosedToday) =

        { Reference = positionClosedToday.Reference
          Sequence  = positionClosedToday.Sequence
          Date      = positionClosedToday.Date
          IssueId   = positionClosedToday.IssueId
          Direction = positionClosedToday.Direction
          Shares    = positionClosedToday.Shares
          Basis     = positionClosedToday.Basis
          Final     = positionClosedToday.Final }

    let positionsClosedPrior =
        intermediate.PositionsClosedToday
        |> Set.map mapping
        |> Set.union intermediate.PositionsClosedPrior

    let adjustStops =
        let adjust stops (stoploss : Stoploss) = stops |> Map.add stoploss.IssueId stoploss.Price
        Array.fold adjust intermediate.Stops

    { intermediate with
        Date  = adjustments.Date
        Stops = adjustments.Stoplosses |> adjustStops
        PositionsClosedPrior = positionsClosedPrior
        PositionsClosedToday = Set.empty }

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
        let final = quote.Close
        let upper = quote.Hi
        let lower = quote.Lo
        let delta = quote.Close - positionActiveToday.Final

        { positionActiveToday with
            Final = final
            Upper = upper
            Lower = lower
            Delta = delta }

    { intermediate with PositionsActiveToday = intermediate.PositionsActiveToday |> Set.map mapping }
