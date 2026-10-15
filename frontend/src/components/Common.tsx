import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import type { Activity, Issue } from '../types'
export function Badge({ children }: { children: ReactNode }) {
  return (
    <span className={`badge badge-${String(children).toLowerCase()}`}>
      {typeof children === 'string' ? children.replace(/([a-z])([A-Z])/g, '$1 $2') : children}
    </span>
  )
}
export function State({ loading, error }: { loading?: boolean; error?: Error | null }) {
  if (error)
    return (
      <div className="empty error" role="alert">
        <strong>Unable to load this workspace</strong>
        <p>{error.message}</p>
        <button onClick={() => window.location.reload()}>Try again</button>
      </div>
    )
  return loading ? (
    <div className="empty" role="status">
      Loading workspace…
    </div>
  ) : null
}
export function ActivityList({ items }: { items: Activity[] }) {
  if (!items.length) return <p className="empty">No activity recorded yet.</p>
  return (
    <ol className="timeline">
      {items.map((a) => (
        <li key={a.id}>
          <span className="timeline-dot" />
          <div>
            <div className="row">
              <strong>{a.action.replace(/([a-z])([A-Z])/g, '$1 $2')}</strong>
              <time>{new Date(a.timestamp).toLocaleString()}</time>
            </div>
            <p>{a.newValue}</p>
            <small>
              {a.actor} · <Link to={`/issues/${a.issueId}`}>PD-{a.issueId}</Link>
              {a.previousValue && ` · Previously: ${a.previousValue}`}
            </small>
          </div>
        </li>
      ))}
    </ol>
  )
}
export function IssueTable({ issues }: { issues: Issue[] }) {
  if (!issues.length)
    return (
      <div className="empty">
        No issues match these filters. Adjust the filters or report a new issue.
      </div>
    )
  return (
    <div className="table-scroll">
      <table>
        <thead>
          <tr>
            <th>Issue</th>
            <th>Status</th>
            <th>Priority</th>
            <th>Assignee</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          {issues.map((i) => (
            <tr key={i.id}>
              <td>
                <Link className="issue-link" to={`/issues/${i.id}`}>
                  <small>
                    PD-{i.id} <span>· {i.issueType}</span>
                  </small>
                  <strong>{i.title}</strong>
                </Link>
              </td>
              <td>
                <Badge>{i.status}</Badge>
              </td>
              <td>
                <Badge>{i.priority}</Badge>
              </td>
              <td>{i.assignee ?? 'Unassigned'}</td>
              <td className="muted">{new Date(i.createdAt).toLocaleDateString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
export function Heading({
  eyebrow,
  title,
  children,
}: {
  eyebrow: string
  title: string
  children?: ReactNode
}) {
  return (
    <div className="page-heading">
      <div>
        <div className="eyebrow">{eyebrow}</div>
        <h1>{title}</h1>
      </div>
      {children}
    </div>
  )
}
