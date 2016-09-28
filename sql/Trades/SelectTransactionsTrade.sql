select
    [transaction].[Sequence]  as [Sequence],
    [transaction].[Date]      as [Date],
    [transaction].[IssueId]   as [IssueId],
    [transaction].[Direction] as [Direction],
    [transaction].[Shares]    as [Shares],
    [transaction].[Operation] as [Operation],
    [transaction].[Price]     as [Price]
from
    [TransactionTrade] [transaction]
where
    [transaction].[Date] = @date
