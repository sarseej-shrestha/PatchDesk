export async function api<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  const response = await fetch(`/api${path}`, { method, headers: body ? { 'Content-Type': 'application/json' } : undefined, body: body ? JSON.stringify(body) : undefined })
  if (!response.ok) {
    const problem = await response.json().catch(() => ({})) as { detail?: string; title?: string; errors?: Record<string, string[]> }
    throw new Error(problem.detail ?? (problem.errors ? Object.values(problem.errors).flat().join(' ') : problem.title) ?? `Request failed (${response.status})`)
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>
}
