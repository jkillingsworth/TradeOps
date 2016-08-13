select
    [transaction].[Sequence] as [Sequence],
    [transaction].[IssueId]  as [IssueId],
    [transaction].[Date]     as [Date],
    [transaction].[Shares]   as [Shares],
    [transaction].[Price]    as [Price]
from
    [TransactionTrade] [transaction]
where
    [transaction].[Date] = @date
