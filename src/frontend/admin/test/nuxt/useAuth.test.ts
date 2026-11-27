import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { author, callsTo, deferred, httpError, loadAuth, loginResponse, MINUTE, signIn, type FetchMock } from './helpers'

let fetchMock: FetchMock

beforeEach(() => {
  vi.resetModules()
  fetchMock = vi.fn()
  vi.stubGlobal('$fetch', fetchMock)
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

describe('useAuth refreshToken', () => {
  it('shares one request between concurrent callers', async () => {
    // Regression: each caller used to send its own refresh. The second one replayed the refresh token the first had
    // already rotated, the API treated that as theft and revoked the session, and the author was signed out.
    const auth = await signIn(fetchMock, 'old')
    const response = deferred<unknown>()
    fetchMock.mockReturnValue(response.promise)

    const calls = [auth.refreshToken(), auth.refreshToken(), auth.refreshToken()]
    response.resolve(loginResponse('fresh'))

    expect(await Promise.all(calls)).toEqual([true, true, true])
    expect(callsTo(fetchMock, '/api/admin/auth/refresh')).toHaveLength(1)
    expect(auth.accessToken.value).toBe('fresh')
  })

  it('starts a new request once the previous one has settled', async () => {
    const auth = await loadAuth()
    fetchMock.mockResolvedValueOnce(loginResponse('first')).mockResolvedValueOnce(loginResponse('second'))

    await auth.refreshToken()
    await auth.refreshToken()

    expect(callsTo(fetchMock, '/refresh')).toHaveLength(2)
    expect(auth.accessToken.value).toBe('second')
  })

  it('sends the refresh cookie', async () => {
    const auth = await loadAuth()
    fetchMock.mockResolvedValue(loginResponse('fresh'))

    await auth.refreshToken()

    expect(fetchMock).toHaveBeenCalledWith(expect.stringMatching(/\/api\/admin\/auth\/refresh$/),
      expect.objectContaining({ method: 'POST', credentials: 'include' }))
  })

  it('keeps the session on a network error', async () => {
    // Regression: any failure used to sign the author out, so a brief network drop ended the session
    const auth = await signIn(fetchMock, 'current')
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))

    expect(await auth.refreshToken()).toBe(false)
    expect(auth.accessToken.value).toBe('current')
    expect(auth.user.value).toEqual(author)
  })

  it('keeps the session on a server error', async () => {
    const auth = await signIn(fetchMock, 'current')
    fetchMock.mockRejectedValue(httpError(502))

    expect(await auth.refreshToken()).toBe(false)
    expect(auth.accessToken.value).toBe('current')
  })

  it.each([400, 401, 403])('signs the author out when the API rejects the refresh token (%i)', async (status) => {
    const auth = await signIn(fetchMock, 'current')
    fetchMock.mockRejectedValue(httpError(status))

    expect(await auth.refreshToken()).toBe(false)
    expect(auth.accessToken.value).toBeNull()
    expect(auth.user.value).toBeNull()
    expect(auth.isAuthenticated.value).toBe(false)
  })
})

describe('useAuth isTokenExpiringSoon', () => {
  it('reports a token as expiring five minutes before its expiry', async () => {
    vi.useFakeTimers()
    const auth = await signIn(fetchMock, 'access', 15 * 60)

    vi.advanceTimersByTime(10 * MINUTE - 1)
    expect(auth.isTokenExpiringSoon()).toBe(false)
    vi.advanceTimersByTime(2)
    expect(auth.isTokenExpiringSoon()).toBe(true)
  })

  it('is false without a token', async () => {
    const auth = await loadAuth()
    expect(auth.isTokenExpiringSoon()).toBe(false)
  })
})

describe('useAuth initAuth', () => {
  it('restores the session through the refresh cookie once per page load', async () => {
    const auth = await loadAuth()
    fetchMock.mockResolvedValue(loginResponse('restored'))

    await Promise.all([auth.initAuth(), auth.initAuth()])
    auth.clearTokens()
    await auth.initAuth()

    expect(callsTo(fetchMock, '/refresh')).toHaveLength(1)
  })

  it('does not refresh when a token is already in memory', async () => {
    const auth = await signIn(fetchMock)

    await auth.initAuth()

    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('removes tokens that earlier versions kept in localStorage', async () => {
    localStorage.setItem('cms_admin_token', 'leaked')
    localStorage.setItem('cms_admin_token_expiry', '1')
    const auth = await signIn(fetchMock)

    await auth.initAuth()

    expect(localStorage.getItem('cms_admin_token')).toBeNull()
    expect(localStorage.getItem('cms_admin_token_expiry')).toBeNull()
  })
})

describe('useAuth certLogin', () => {
  it('signs in with the client certificate', async () => {
    const auth = await loadAuth()
    fetchMock.mockResolvedValue(loginResponse('cert-token'))

    expect(await auth.certLogin()).toEqual({ success: true })
    expect(fetchMock).toHaveBeenCalledWith(expect.stringMatching(/\/api\/admin\/auth\/cert-login$/),
      expect.objectContaining({ method: 'POST', credentials: 'include' }))
    expect(auth.accessToken.value).toBe('cert-token')
    expect(auth.user.value).toEqual(author)
    expect(auth.isAdmin.value).toBe(true)
  })

  it('fails without signing in when no certificate is presented', async () => {
    const auth = await loadAuth()
    fetchMock.mockRejectedValue(httpError(401))

    expect(await auth.certLogin()).toMatchObject({ success: false })
    expect(auth.isAuthenticated.value).toBe(false)
  })
})

describe('useAuth logout', () => {
  it('clears the session even when the API call fails', async () => {
    const auth = await signIn(fetchMock, 'current')
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))

    await auth.logout()

    expect(fetchMock).toHaveBeenCalledWith(expect.stringMatching(/\/logout$/), expect.objectContaining({
      headers: { Authorization: 'Bearer current' }
    }))
    expect(auth.isAuthenticated.value).toBe(false)
  })
})
