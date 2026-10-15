import { useState } from 'react'
import type { ReactNode } from 'react'
import { Workspace } from '../services/workspace'
export function WorkspaceProvider({ children }: { children: ReactNode }) {
  const [actorId, setActorId] = useState(1)
  const [message, setMessage] = useState('')
  return (
    <Workspace.Provider value={{ actorId, setActorId, notify: setMessage }}>
      {children}
      {message && (
        <div className="toast" role="status">
          {message}
          <button aria-label="Dismiss notification" onClick={() => setMessage('')}>
            ×
          </button>
        </div>
      )}
    </Workspace.Provider>
  )
}
