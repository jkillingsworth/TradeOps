select
    [stoploss].[Date]    as [Date],
    [stoploss].[IssueId] as [IssueId],
    [stoploss].[Price]   as [Price]
from
    [Stoploss] [stoploss]
where
    [stoploss].[Date] = @date
