module TradeOps.Processing

open System
open TradeOps.Types

//-------------------------------------------------------------------------------------------------

let private dateStart = Persistence.selectStartDate ()
let private holidays = Persistence.selectHolidays ()

let private isWeekendSat (date : DateTime) = date.DayOfWeek = DayOfWeek.Saturday
let private isWeekendSun (date : DateTime) = date.DayOfWeek = DayOfWeek.Sunday

let private isWeekendOrHoliday = function
    | date when isWeekendSat date -> true
    | date when isWeekendSun date -> true
    | date -> Array.contains date holidays

let private isNotWeekendOrHoliday = (isWeekendOrHoliday >> not)

let private generator (date : DateTime) =
    let next = date.AddDays(1.0)
    Some (date, next)

let generateDates dateFinal =
    dateStart
    |> Seq.unfold generator
    |> Seq.filter isNotWeekendOrHoliday
    |> Seq.takeWhile (fun x -> x <= dateFinal)

//-------------------------------------------------------------------------------------------------

let private mapSequence = function
    | Divid transaction -> transaction.Sequence
    | Split transaction -> transaction.Sequence
    | Trade transaction -> transaction.Sequence

let getTransactions date =

    let transactions =
        [ Persistence.selectTransactionsDivid date
          Persistence.selectTransactionsSplit date
          Persistence.selectTransactionsTrade date ]

    let transactions =
        transactions
        |> Array.concat
        |> Array.sortBy mapSequence

    transactions
