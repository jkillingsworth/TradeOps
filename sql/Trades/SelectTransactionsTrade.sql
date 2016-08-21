﻿select
    [transaction].[Sequence] as [Sequence],
    [transaction].[Date]     as [Date],
    [transaction].[IssueId]  as [IssueId],
    [transaction].[Shares]   as [Shares],
    [transaction].[Price]    as [Price]
from
    [TransactionTrade] [transaction]
where
    [transaction].[Date] = @date
