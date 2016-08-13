select
    [transaction].[Sequence] as [Sequence],
    [transaction].[IssueId]  as [IssueId],
    [transaction].[Date]     as [Date],
    [transaction].[New]      as [New],
    [transaction].[Old]      as [Old]
from
    [TransactionSplit] [transaction]
where
    [transaction].[Date] = @date
