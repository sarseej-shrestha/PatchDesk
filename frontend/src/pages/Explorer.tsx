import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { Search } from 'lucide-react'
import { api } from '../services/api'
import { useDevelopers } from '../services/workspace'
import type { Issue, Page } from '../types'
import { Heading, IssueTable, State } from '../components/Common'
export default function Explorer() {
  const [params, setParams] = useSearchParams()
  const developers = useDevelopers()
  const page = Number(params.get('page') || 1)
  const query = useQuery({ queryKey: ['issues', params.toString()], queryFn: () => api<Page<Issue>>(`/issues?${params.toString()}`) })
  const filter = (key: string, value: string) => { const next = new URLSearchParams(params); if (value) next.set(key, value); else next.delete(key); next.delete('page'); setParams(next) }
  return <><Heading eyebrow="TRIAGE & DELIVERY" title="Issue explorer"><Link className="button primary" to="/issues/new">+ Report issue</Link></Heading><p className="page-intro">Keep the context, ownership, and next step in one place.</p>
    <section className="panel"><div className="filters"><label className="search"><Search size={17} /><input aria-label="Search issues" placeholder="Search title or description…" value={params.get('search') || ''} onChange={e => filter('search', e.target.value)} maxLength={200} /></label>
      <label><span className="sr-only">Status filter</span><select aria-label="Status filter" value={params.get('status') || ''} onChange={e => filter('status', e.target.value)}><option value="">All statuses</option>{['Open', 'InProgress', 'InReview', 'Resolved', 'Reopened'].map(s => <option key={s}>{s}</option>)}</select></label>
      <select aria-label="Priority filter" value={params.get('priority') || ''} onChange={e => filter('priority', e.target.value)}><option value="">All priorities</option>{['Low', 'Medium', 'High', 'Critical'].map(s => <option key={s}>{s}</option>)}</select>
      <select aria-label="Assignee filter" value={params.get('assigneeId') || ''} onChange={e => filter('assigneeId', e.target.value)}><option value="">All assignees</option>{developers.data?.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}</select>
      <select aria-label="Sort issues" value={params.get('sort') || 'newest'} onChange={e => filter('sort', e.target.value)}><option value="newest">Newest first</option><option value="oldest">Oldest first</option><option value="priority">Highest priority</option></select></div>
      <State loading={query.isPending} error={query.error} />{query.data && <><IssueTable issues={query.data.items} /><div className="pagination"><span>{query.data.total} issues · Page {page}</span><div><button disabled={page <= 1} onClick={() => { params.set('page', String(page - 1)); setParams(params) }}>Previous</button><button disabled={page * query.data.pageSize >= query.data.total} onClick={() => { params.set('page', String(page + 1)); setParams(params) }}>Next</button></div></div></>}</section></>
}
