import { NavLink, Route, Routes, Link } from 'react-router-dom'
import { Activity, Bug, FlaskConical, LayoutDashboard, GitBranch, PanelTop } from 'lucide-react'
import { useDevelopers, useWorkspace } from './services/workspace'
import Overview from './pages/Overview'
import Explorer from './pages/Explorer'
import IssueDetail from './pages/IssueDetail'
import IssueForm from './pages/IssueForm'
import ActivityPage from './pages/ActivityPage'
import './App.css'
export default function App() {
  const { actorId, setActorId } = useWorkspace(); const developers = useDevelopers()
  return <div className="app-shell"><aside className="sidebar"><Link className="brand" to="/"><span className="brand-icon"><PanelTop size={22} /></span>PatchDesk<span className="brand-dot">.</span></Link><span className="sidebar-label">WORKSPACE</span><nav><NavLink to="/" end><LayoutDashboard size={18} />Overview</NavLink><NavLink to="/issues"><Bug size={18} />Issue explorer</NavLink><NavLink to="/verification"><FlaskConical size={18} />Verification</NavLink><NavLink to="/activity"><Activity size={18} />Activity history</NavLink></nav><div className="sidebar-bottom"><GitBranch size={17} /><a href="https://github.com/sarseej-shrestha/PatchDesk" target="_blank" rel="noreferrer">Project repository ↗</a><p>From bug report<br />to verified fix.</p><span className="demo-label">LOCAL DEMO WORKSPACE</span></div></aside>
    <div className="workspace"><header className="topbar"><span>Engineering <span className="muted">/ PatchDesk</span></span><label className="actor-selector"><span>Simulated actor</span><select aria-label="Simulated actor" value={actorId} onChange={e => setActorId(Number(e.target.value))}>{developers.data?.map(d => <option value={d.id} key={d.id}>{d.name} · {d.role}</option>)}</select></label></header><main><Routes><Route path="/" element={<Overview />} /><Route path="/issues" element={<Explorer />} /><Route path="/issues/new" element={<IssueForm />} /><Route path="/issues/:id" element={<IssueDetail />} /><Route path="/verification" element={<IssueDetail verification />} /><Route path="/verification/:id" element={<IssueDetail verification />} /><Route path="/activity" element={<ActivityPage />} /><Route path="*" element={<div className="empty"><h1>Page not found</h1><Link to="/">Back to overview</Link></div>} /></Routes><footer>PatchDesk · Fictional demo data. Actor selection does not provide authentication.</footer></main></div></div>
}
