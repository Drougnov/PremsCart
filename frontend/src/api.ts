export async function api<T = any>(path: string, method = 'GET', data?: unknown): Promise<T> {
  const token = sessionStorage.getItem('premscart-token') ?? ''
  const response = await fetch(path, { method, headers: { Authorization: `Bearer ${token}`, ...(data instanceof FormData ? {} : data !== undefined ? { 'Content-Type': 'application/json' } : {}) }, body: data instanceof FormData ? data : data !== undefined ? JSON.stringify(data) : undefined })
  const result = await response.json().catch(() => ({}))
  if (!response.ok) {
    const fallback=response.status===401?'Your session has expired. Sign in again to continue.':response.status===403?'Your account does not have access to this action.':response.status===404?'This item is unavailable or could not be found.':response.status>=500?'The campus service is temporarily unavailable. Please try again shortly.':`We could not complete this request (${response.status}).`
    throw new Error(response.status>=500?fallback:result.error??result.title??fallback)
  }
  return result
}
export function go(path: string) { history.pushState({}, '', path); window.dispatchEvent(new PopStateEvent('popstate')); window.scrollTo(0, 0) }
export function signout() { sessionStorage.removeItem('premscart-token'); window.dispatchEvent(new Event('premscart-auth')); go('/login') }
