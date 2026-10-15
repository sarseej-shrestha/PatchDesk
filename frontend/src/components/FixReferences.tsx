import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { GitBranch } from 'lucide-react'
import type { Issue, Registry } from '../types'
import { useAction, useWorkspace } from '../services/workspace'
import { api } from '../services/api'
import { Badge, State } from './Common'
const repo = 'https://github.com/sarseej-shrestha/PatchDesk'
interface PullRequest { title: string; state: string; merged: boolean; headCommit: string; fetchedAt: string; checks: { name: string; status: string; conclusion: string | null; url: string | null }[] }
export default function FixReferences({ issue }: { issue: Issue }) {
  const { actorId } = useWorkspace(); const action = useAction(); const [number, setNumber] = useState<number | null>(null)
  const registry = useQuery({ queryKey: ['registry'], queryFn: () => api<Registry>('/regression-registry') })
  const pr = useQuery({ queryKey: ['pullRequest', number], queryFn: () => api<PullRequest>(`/github/pulls/${number}`), enabled: number !== null, retry: false })
  return <section className="panel padded"><h2><GitBranch size={18} /> Linked code changes</h2><p className="muted">References point to this repository. Stored links are not proof of review or successful CI.</p>
    {issue.fixReferences.map(f => <article className="reference" key={f.id}><strong>{f.description}</strong>{f.branchName && <a href={`${repo}/tree/${encodeURIComponent(f.branchName)}`} target="_blank" rel="noreferrer"><code>{f.branchName} ↗</code></a>}{f.commitHash && <a href={`${repo}/commit/${f.commitHash}`} target="_blank" rel="noreferrer"><code>{f.commitHash} ↗</code></a>}{f.pullRequestUrl && <div className="actions"><a href={f.pullRequestUrl} target="_blank" rel="noreferrer">Open pull request ↗</a><button onClick={() => { const n = Number(f.pullRequestUrl!.split('/').pop()); if (n === number) void pr.refetch(); else setNumber(n) }}>Inspect pull request</button></div>}</article>)}
    {number !== null && <div className="notice"><State loading={pr.isPending} error={pr.error} />{pr.data && <><strong>#{number} · {pr.data.title}</strong><p>{pr.data.merged ? 'Merged' : pr.data.state} · fetched {new Date(pr.data.fetchedAt).toLocaleString()}</p><code>Head: {pr.data.headCommit}</code>{pr.data.checks.length ? pr.data.checks.map((c, index) => <p key={`${c.name}-${index}`}>{c.name} <Badge>{c.conclusion ?? c.status}</Badge></p>) : <p>No check runs returned by GitHub. This does not imply success.</p>}</>}</div>}
    {issue.fixReferences.length === 0 && <p className="muted">No code changes linked yet.</p>}
    <details><summary>Link a code change</summary><form className="form-panel" onSubmit={e => { e.preventDefault(); const f = new FormData(e.currentTarget); action.mutate({ path: `/issues/${issue.id}/fix-references`, body: { branchName: f.get('branchName') || null, commitHash: f.get('commitHash') || null, pullRequestUrl: f.get('pullRequestUrl') || null, description: f.get('description'), actorId, version: issue.version } }) }}>
      <label>Git branch<input name="branchName" maxLength={200} placeholder="feat/regression-testing" /></label><label>Full commit hash<input name="commitHash" maxLength={40} pattern="[a-fA-F0-9]{40}" defaultValue={registry.data?.buildCommit} placeholder="40-character Git commit SHA" /></label><label>Pull request URL<input name="pullRequestUrl" type="url" maxLength={500} placeholder={`${repo}/pull/…`} /></label><label>Change description<textarea name="description" required maxLength={2000} /></label><p className="muted">Link the running clean build commit to verify its regression results. Adding a reference requires re-running tests.</p><button disabled={action.isPending || issue.status === 'Resolved'}>Save code reference</button></form></details></section>
}
