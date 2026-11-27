import { vi } from 'vitest'

// Shared helpers for the admin auth tests. $fetch is the only network boundary; tests replace it with a mock that
// routes by URL.

export type FetchMock = ReturnType<typeof vi.fn<(url: string, options?: Record<string, unknown>) => Promise<unknown>>>

export const MINUTE = 60 * 1000
export const author = { id: 1, name: 'Admin', email: 'admin@example.com', role: 'admin' }

export function loginResponse(token: string, expiresIn = 900) {
  return { success: true, data: { accessToken: token, expiresIn, author } }
}

export function httpError(status: number) {
  return Object.assign(new Error(`HTTP ${status}`), { statusCode: status, status })
}

export function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((res, rej) => { resolve = res; reject = rej })
  return { promise, resolve, reject }
}

export function callsTo(fetchMock: FetchMock, path: string) {
  return fetchMock.mock.calls.filter(([url]) => url.endsWith(path))
}

/**
 * Imports a fresh copy of useAuth (its restore and in-flight refresh promises live in module scope, once per page
 * load) and starts signed out, since useState survives between tests in the shared Nuxt app.
 * Call vi.resetModules() first.
 */
export async function loadAuth() {
  const { useAuth } = await import('~/composables/useAuth')
  const auth = useAuth()
  auth.clearTokens()
  return auth
}

/** Signs in through the login endpoint so the token and its expiry are set the way the app sets them. */
export async function signIn(fetchMock: FetchMock, token = 'access', expiresIn = 900) {
  const auth = await loadAuth()
  fetchMock.mockResolvedValueOnce(loginResponse(token, expiresIn))
  await auth.login({ email: author.email, password: 'secret-password' })
  fetchMock.mockClear()
  return auth
}
