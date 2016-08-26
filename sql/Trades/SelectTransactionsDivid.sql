select
    [transaction].[Sequence] as [Sequence],
    [transaction].[Date]     as [Date],
    [transaction].[IssueId]  as [IssueId],
    [transaction].[Position] as [Position],
    [transaction].[Shares]   as [Shares],
    [transaction].[Amount]   as [Amount],
    [transaction].[PayDate]  as [PayDate]
from
    [TransactionDivid] [transaction]
where
    [transaction].[Date] = @date
