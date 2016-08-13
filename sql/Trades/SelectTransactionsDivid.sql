select
    [transaction].[Sequence] as [Sequence],
    [transaction].[IssueId]  as [IssueId],
    [transaction].[Date]     as [Date],
    [transaction].[Amount]   as [Amount]
from
    [TransactionDivid] [transaction]
where
    [transaction].[Date] = @date
