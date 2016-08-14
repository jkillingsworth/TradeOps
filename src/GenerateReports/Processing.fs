module TradeOps.Processing

open System
open FSharp.Control.Reactive
open TradeOps.Types

//-------------------------------------------------------------------------------------------------

let private dateStart = Persistence.selectStartDate ()
let private holidays = Persistence.selectHolidays ()

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

let renderTransactions transactions date =

    let items =
        [ Persistence.selectTransactionsDivid date
          Persistence.selectTransactionsSplit date
          Persistence.selectTransactionsTrade date ]

    items
    |> Array.concat
    |> Array.sortBy mapSequence
    |> Array.append transactions
