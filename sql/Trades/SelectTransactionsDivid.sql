select
    [transaction].[Sequence] as [Sequence],
    [transaction].[IssueId]  as [IssueId],
    [transaction].[Date]     as [Date],
    [transaction].[Amount]   as [Amount],
    [transaction].[PayDate]  as [PayDate]
from
    [TransactionDivid] [transaction]
where
    [transaction].[Date] = @date
