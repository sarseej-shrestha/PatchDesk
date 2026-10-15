import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { api } from '../services/api'
import type { Activity, Page } from '../types'
import { ActivityList, Heading, State } from '../components/Common'
export default function ActivityPage() {
  const [page, setPage] = useState(1)
  const query = useQuery({ queryKey: ['activity', page], queryFn: () => api<Page<Activity>>(`/activity?page=${page}`) })
  return <><Heading eyebrow="THE ENGINEERING RECORD" title="Activity history" /><p className="page-intro">Persisted decisions, ownership changes, and verification runs.</p><section className="panel"><State loading={query.isPending} error={query.error} />{query.data && <><ActivityList items={query.data.items} /><div className="pagination"><span>{query.data.total} events · Page {page}</span><div><button disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</button><button disabled={page * 30 >= query.data.total} onClick={() => setPage(page + 1)}>Next</button></div></div></>}</section></>
}
