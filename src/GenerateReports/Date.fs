module TradeOps.Date

open System
open FSharp.Control.Reactive

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

let rec private findNearest increment date =
    if (date |> isWeekendOrHoliday) then
        date |> addDays increment |> findNearest increment
    else
        date

//-------------------------------------------------------------------------------------------------

let generateDates dateFinal =
    id
    |> Observable.generate dateStart (fun date -> date <= dateFinal) (addDays +1)
    |> Observable.filter (isWeekendOrHoliday >> not)

let getMaximumDate () =

    let timeZoneEst = "Eastern Standard Time"
    let hour08PmEst = 20

    let dateTime = DateTime.Now
    let dateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, timeZoneEst)
    let dateTime =
        if (dateTime.Hour < hour08PmEst) then
            dateTime |> addDays -1
        else
            dateTime

    dateTime.Date |> findNearest -1
