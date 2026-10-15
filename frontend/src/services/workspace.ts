import { createContext, useContext } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from './api'
import type { Developer } from '../types'
export const Workspace = createContext<{ actorId: number; setActorId: (id: number) => void; notify: (message: string) => void }>({ actorId: 1, setActorId: () => {}, notify: () => {} })
export const useWorkspace = () => useContext(Workspace)
export const useDevelopers = () => useQuery({ queryKey: ['developers'], queryFn: () => api<Developer[]>('/developers') })
export function useAction() {
  const cache = useQueryClient()
  const { notify } = useWorkspace()
  return useMutation({ mutationFn: ({ path, method = 'POST', body }: { path: string; method?: string; body?: unknown }) => api(path, method, body),
    onSuccess: async () => { await cache.invalidateQueries(); notify('Changes saved.'); }, onError: (error) => notify(error.message) })
}
