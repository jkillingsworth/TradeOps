select
    [transaction].[Sequence]  as [Sequence],
    [transaction].[AccountId] as [AccountId],
    [transaction].[Date]      as [Date],
    [transaction].[IssueId]   as [IssueId],
    [transaction].[Direction] as [Direction],
    [transaction].[Shares]    as [Shares],
    [transaction].[Amount]    as [Amount],
    [transaction].[PayDate]   as [PayDate]
from
    [TransactionDivid] [transaction]
where
    [transaction].[Date] = @date
