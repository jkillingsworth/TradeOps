select
    [transaction].[Sequence] as [Sequence],
    [transaction].[Date]     as [Date],
    [transaction].[IssueId]  as [IssueId],
    [transaction].[Position] as [Position],
    [transaction].[Shares]   as [Shares],
    [transaction].[Activity] as [Activity],
    [transaction].[Price]    as [Price]
from
    [TransactionTrade] [transaction]
where
    [transaction].[Date] = @date
