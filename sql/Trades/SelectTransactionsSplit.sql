select
    [transaction].[Sequence]  as [Sequence],
    [transaction].[Date]      as [Date],
    [transaction].[IssueId]   as [IssueId],
    [transaction].[Direction] as [Direction],
    [transaction].[Shares]    as [Shares],
    [transaction].[New]       as [New],
    [transaction].[Old]       as [Old]
from
    [TransactionSplit] [transaction]
where
    [transaction].[Date] = @date
