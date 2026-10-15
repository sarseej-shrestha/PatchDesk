import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { ArrowUpRight, CheckCheck, FlaskConical } from 'lucide-react'
import { api } from '../services/api'
import type { Summary, Page, Issue } from '../types'
import { ActivityList, Heading, IssueTable, State } from '../components/Common'
export default function Overview() {
  const summary = useQuery({ queryKey: ['summary'], queryFn: () => api<Summary>('/dashboard/summary') })
  const queue = useQuery({ queryKey: ['reviewQueue'], queryFn: () => api<Page<Issue>>('/issues?status=InReview&sort=priority&pageSize=5') })
  if (!summary.data || !queue.data) return <State loading={summary.isPending || queue.isPending} error={summary.error || queue.error} />
  const s = summary.data
  return <><Heading eyebrow="ENGINEERING WORKSPACE" title="Every fix needs evidence."><Link className="button primary" to="/issues/new">+ Report issue</Link></Heading>
    <p className="page-intro">A clear path from the first bug report to a reviewed, verified resolution.</p>
    <div className="metrics">{[['Total issues', s.total], ['Open', s.open], ['In progress', s.inProgress], ['Awaiting review', s.inReview], ['Resolved', s.resolved], ['Critical bugs', s.criticalBugs]].map(([label, value]) => <div className="metric" key={label}><span>{label}</span><strong>{value}</strong></div>)}</div>
    <div className="overview-grid"><section className="panel"><div className="panel-heading"><div><h2>Ready for review</h2><p>Review changes. Check the evidence. Close the loop.</p></div><Link to="/issues?status=InReview">View all <ArrowUpRight size={15} /></Link></div><IssueTable issues={queue.data.items} /></section>
    <section className="feature-panel"><FlaskConical size={28} /><span className="eyebrow">REGRESSION SPOTLIGHT</span><h2>A $100 boundary.<br />A $10 mistake.</h2><p>Run the same test against the defective fixture and the corrected calculator. Inspect the actual results.</p><Link className="button" to="/verification/104">Open verification <ArrowUpRight size={16} /></Link><small><CheckCheck size={15} /> Evidence comes from executed assertions.</small></section></div>
    <section className="panel"><div className="panel-heading"><h2>Recent engineering activity</h2><Link to="/activity">Full history <ArrowUpRight size={15} /></Link></div><ActivityList items={s.recentActivity} /></section></>
}
