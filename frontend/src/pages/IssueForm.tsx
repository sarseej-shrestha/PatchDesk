import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../services/api'
import { useDevelopers, useWorkspace } from '../services/workspace'
import { Heading, State } from '../components/Common'
import type { Issue } from '../types'
export default function IssueForm() {
  const developers = useDevelopers()
  const { actorId, notify } = useWorkspace()
  const navigate = useNavigate()
  const cache = useQueryClient()
  const mutation = useMutation({
    mutationFn: (body: unknown) => api<Issue>('/issues', 'POST', body),
    onSuccess: async (i) => {
      await cache.invalidateQueries()
      notify('Issue reported.')
      navigate(`/issues/${i.id}`)
    },
  })
  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const f = new FormData(e.currentTarget)
    mutation.mutate({
      ...Object.fromEntries(f),
      reporterId: actorId,
      assigneeId: f.get('assigneeId') ? Number(f.get('assigneeId')) : null,
    })
  }
  return (
    <>
      <Heading eyebrow="START WITH CONTEXT" title="Report an issue" />
      <p className="page-intro">
        Describe what happened, what you expected, and how to reproduce it.
      </p>
      <form className="panel form-panel" onSubmit={submit}>
        <label>
          Title
          <input
            name="title"
            required
            maxLength={200}
            placeholder="A concise description of the problem"
          />
        </label>
        <label>
          Description
          <textarea
            name="description"
            required
            maxLength={10000}
            rows={4}
            placeholder="Observed behavior and expected result"
          />
        </label>
        <label>
          Reproduction steps
          <textarea
            name="reproductionSteps"
            maxLength={10000}
            rows={4}
            placeholder="1. Open…&#10;2. Run…&#10;3. Observe…"
          />
        </label>
        <div className="form-grid">
          <label>
            Issue type
            <select name="issueType">
              {['Bug', 'Feature', 'Improvement', 'Maintenance'].map((v) => (
                <option key={v}>{v}</option>
              ))}
            </select>
          </label>
          <label>
            Priority
            <select name="priority" defaultValue="Medium">
              {['Low', 'Medium', 'High', 'Critical'].map((v) => (
                <option key={v}>{v}</option>
              ))}
            </select>
          </label>
          <label>
            Severity
            <select name="severity" defaultValue="Moderate">
              {['Minor', 'Moderate', 'Major', 'Critical'].map((v) => (
                <option key={v}>{v}</option>
              ))}
            </select>
          </label>
          <label>
            Assignee
            <select name="assigneeId">
              <option value="">Unassigned</option>
              {developers.data?.map((d) => (
                <option value={d.id} key={d.id}>
                  {d.name}
                </option>
              ))}
            </select>
          </label>
        </div>
        <p className="muted">
          Reporter: {developers.data?.find((d) => d.id === actorId)?.name}. Demo actor selection is
          simulated.
        </p>
        <State error={mutation.error || developers.error} />
        <div className="actions">
          <button type="button" onClick={() => navigate('/issues')}>
            Cancel
          </button>
          <button className="primary" disabled={mutation.isPending}>
            {mutation.isPending ? 'Saving…' : 'Create issue'}
          </button>
        </div>
      </form>
    </>
  )
}
