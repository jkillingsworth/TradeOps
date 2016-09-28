select
    [quote].[IssueId] as [IssueId],
    [quote].[Date]    as [Date],
    [quote].[Close]   as [Close]
from
    [Quote] [quote]
where
    [quote].[IssueId] = @issueId and [quote].[Date] = @date
