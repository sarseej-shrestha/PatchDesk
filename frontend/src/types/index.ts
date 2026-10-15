export type Status = 'Open' | 'InProgress' | 'InReview' | 'Resolved' | 'Reopened'
export type Priority = 'Low' | 'Medium' | 'High' | 'Critical'
export interface Developer { id: number; name: string; email: string; role: string }
export interface Activity { id: number; issueId: number; actorId: number; actor: string; action: string; previousValue: string | null; newValue: string | null; timestamp: string }
export interface Execution { id: number; result: string; executionSource: string; commitHash: string; executedAt: string; durationMilliseconds: number; evidence: string; evidenceRevision: number; isDefectiveFixture: boolean }
export interface RegressionTest { id: number; testName: string; description: string; testIdentifier: string; latestResult: string; lastExecutedAt: string | null; executions: Execution[] }
export interface Fix { id: number; branchName: string | null; commitHash: string | null; pullRequestUrl: string | null; description: string; createdAt: string }
export interface Issue {
  id: number; title: string; description: string; reproductionSteps: string; issueType: string; priority: Priority; severity: string; status: Status
  assigneeId: number | null; assignee: string | null; reporterId: number; reporter: string; version: number; evidenceRevision: number
  createdAt: string; updatedAt: string; resolvedAt: string | null; resolutionNote: string | null; resolutionOverridden: boolean
  comments: { id: number; author: string; content: string; createdAt: string }[]; activities: Activity[]; regressionTests: RegressionTest[]; fixReferences: Fix[]
}
export interface Page<T> { items: T[]; total: number; page: number; pageSize: number }
export interface Summary { total: number; open: number; inProgress: number; inReview: number; resolved: number; reopened: number; criticalBugs: number; recentActivity: Activity[] }
export interface Registry { tests: { identifier: string; name: string; description: string }[]; buildCommit: string; isClean: boolean; assemblySha256: string; executionSource: string }
