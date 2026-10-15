import { useQuery } from '@tanstack/react-query'
import { FlaskConical, Play } from 'lucide-react'
import { api } from '../services/api'
import { useAction, useWorkspace } from '../services/workspace'
import type { Issue, Registry } from '../types'
import { Badge, State } from './Common'
export default function Verification({ issue }: { issue: Issue }) {
  const registry = useQuery({ queryKey: ['registry'], queryFn: () => api<Registry>('/regression-registry') })
  const action = useAction(); const { actorId } = useWorkspace(); const locked = issue.status === 'Resolved' || action.isPending
  return <section className="panel verification"><div className="panel-heading"><div><h2><FlaskConical size={20} /> Regression verification</h2><p>Actual assertions, recorded against this issue’s evidence revision {issue.evidenceRevision}.</p></div></div>
    <State loading={registry.isPending} error={registry.error} />
    {registry.data && <><div className="notice">Source: {registry.data.executionSource}. These are local server executions, not GitHub Actions results.<br />Build: <code>{registry.data.buildCommit || 'unversioned'}</code> · {registry.data.isClean ? 'Clean checkout' : 'Uncommitted source — cannot verify resolution'}</div>
      <form className="inline-form" onSubmit={e => { e.preventDefault(); const data = new FormData(e.currentTarget); action.mutate({ path: `/issues/${issue.id}/regression-tests`, body: { testIdentifier: data.get('testIdentifier'), actorId, version: issue.version } }) }}>
        <label>Associate a repository test<select name="testIdentifier" required disabled={locked}><option value="">Choose a registered test</option>{registry.data.tests.filter(t => !issue.regressionTests.some(r => r.testIdentifier === t.identifier)).map(t => <option key={t.identifier} value={t.identifier}>{t.name}</option>)}</select></label><button disabled={locked}>Associate test</button></form></>}
    {!issue.regressionTests.length && <p className="empty">No tests associated yet. Choose a registered test to begin.</p>}
    {issue.regressionTests.map(test => <article className="test-card" key={test.id}><div className="row"><div><h3>{test.testName}</h3><p>{test.description}</p><code>{test.testIdentifier}</code></div><Badge>{test.latestResult}</Badge></div>
      <div className="actions"><button disabled={locked} onClick={() => action.mutate({ path: `/issues/${issue.id}/regression-tests/${test.id}/executions`, body: { actorId, version: issue.version, defectiveFixture: true } })}>Run defective fixture</button><button className="primary" disabled={locked} onClick={() => action.mutate({ path: `/issues/${issue.id}/regression-tests/${test.id}/executions`, body: { actorId, version: issue.version, defectiveFixture: false } })}><Play size={14} /> Run corrected test</button></div>
      <details><summary>Execution history ({test.executions.length})</summary>{test.executions.length === 0 && <p className="muted">This test has not executed.</p>}{test.executions.map(run => <div className="execution" key={run.id}><div className="row"><Badge>{run.result}</Badge><span>{new Date(run.executedAt).toLocaleString()} · {run.durationMilliseconds} ms</span></div><p>{run.isDefectiveFixture ? 'Defective demonstration fixture' : 'Corrected implementation'} · revision {run.evidenceRevision}</p><code>{run.evidence}</code><small>{run.executionSource} · commit {run.commitHash || 'unversioned/dirty build'}</small></div>)}</details></article>)}
  </section>
}
