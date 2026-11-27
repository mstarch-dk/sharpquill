import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

// The composable keeps its in-flight refresh, init promise and timer in module scope (one per page load), so every
// test imports a fresh copy of the module. $fetch is the only network boundary and is replaced by a mock.

const TOKEN_KEY = 'cms_website_token'
const EXPIRY_KEY = 'cms_website_token_expiry'
const MINUTE = 60 * 1000
const user = { id: 1, name: 'Reader', email: 'reader@example.com' }

type FetchMock = ReturnType<typeof vi.fn<(url: string, options?: Record<string, unknown>) => Promise<unknown>>>

let fetchMock: FetchMock

function refreshResponse(token: string, expiresIn = 900) {
  return { success: true, data: { accessToken: token, expiresIn, user } }
}

function httpError(status: number) {
  return Object.assign(new Error(`HTTP ${status}`), { statusCode: status, status })
}

function storeSession(token: string, expiry: number) {
  localStorage.setItem(TOKEN_KEY, token)
  localStorage.setItem(EXPIRY_KEY, expiry.toString())
}

function refreshCalls() {
  return fetchMock.mock.calls.filter(([url]) => url.endsWith('/api/v1/auth/refresh'))
}

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((res, rej) => { resolve = res; reject = rej })
  return { promise, resolve, reject }
}

async function loadAuth() {
  const { useWebsiteAuth } = await import('~/composables/useWebsiteAuth')
  const auth = useWebsiteAuth()
  // useState survives between tests in the shared Nuxt app; start every test signed out
  auth.clearTokens()
  return auth
}

beforeEach(() => {
  vi.resetModules()
  localStorage.clear()
  fetchMock = vi.fn()
  vi.stubGlobal('$fetch', fetchMock)
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

describe('useWebsiteAuth refreshToken', () => {
  it('shares one request between concurrent callers', async () => {
    const auth = await loadAuth()
    storeSession('expired', Date.now() - MINUTE)
    const response = deferred<unknown>()
    fetchMock.mockReturnValue(response.promise)

    const calls = [auth.refreshToken(), auth.refreshToken(), auth.refreshToken()]
    response.resolve(refreshResponse('fresh'))

    expect(await Promise.all(calls)).toEqual([true, true, true])
    expect(refreshCalls()).toHaveLength(1)
    expect(auth.accessToken.value).toBe('fresh')
    expect(localStorage.getItem(TOKEN_KEY)).toBe('fresh')
    expect(auth.user.value).toEqual(user)
  })

  it('starts a new request once the previous one has settled', async () => {
    const auth = await loadAuth()
    fetchMock.mockResolvedValueOnce(refreshResponse('first', 0)).mockResolvedValueOnce(refreshResponse('second'))

    expect(await auth.refreshToken()).toBe(true)
    expect(await auth.refreshToken()).toBe(true)

    expect(refreshCalls()).toHaveLength(2)
    expect(auth.accessToken.value).toBe('second')
  })

  it('adopts a token another tab already rotated instead of rotating again', async () => {
    const auth = await loadAuth()
    storeSession('mine', Date.now() + 10 * MINUTE)
    await auth.initAuth()
    expect(auth.accessToken.value).toBe('mine')

    // The other tab refreshed and wrote its new token to the shared localStorage
    storeSession('from-other-tab', Date.now() + 15 * MINUTE)

    expect(await auth.refreshToken()).toBe(true)
    expect(fetchMock).not.toHaveBeenCalled()
    expect(auth.accessToken.value).toBe('from-other-tab')
  })

  it('rotates when the stored token is about to expire, even if another tab wrote it', async () => {
    const auth = await loadAuth()
    storeSession('almost-expired', Date.now() + 30 * 1000)
    fetchMock.mockResolvedValue(refreshResponse('fresh'))

    expect(await auth.refreshToken()).toBe(true)
    expect(refreshCalls()).toHaveLength(1)
    expect(auth.accessToken.value).toBe('fresh')
  })

  it('keeps the session on a network error', async () => {
    const auth = await loadAuth()
    storeSession('current', Date.now() + 10 * MINUTE)
    await auth.initAuth()
    localStorage.setItem(EXPIRY_KEY, (Date.now() - MINUTE).toString())
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))

    expect(await auth.refreshToken()).toBe(false)
    expect(auth.accessToken.value).toBe('current')
    expect(localStorage.getItem(TOKEN_KEY)).toBe('current')
  })

  it('keeps the session on a server error', async () => {
    const auth = await loadAuth()
    storeSession('current', Date.now() + 10 * MINUTE)
    await auth.initAuth()
    localStorage.setItem(EXPIRY_KEY, (Date.now() - MINUTE).toString())
    fetchMock.mockRejectedValue(httpError(503))

    expect(await auth.refreshToken()).toBe(false)
    expect(auth.accessToken.value).toBe('current')
  })

  it.each([400, 401, 403])('signs the reader out when the API rejects the refresh token (%i)', async (status) => {
    const auth = await loadAuth()
    storeSession('current', Date.now() + 10 * MINUTE)
    await auth.initAuth()
    localStorage.setItem(EXPIRY_KEY, (Date.now() - MINUTE).toString())
    fetchMock.mockRejectedValue(httpError(status))

    expect(await auth.refreshToken()).toBe(false)
    expect(auth.accessToken.value).toBeNull()
    expect(auth.isAuthenticated.value).toBe(false)
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
    expect(localStorage.getItem(EXPIRY_KEY)).toBeNull()
  })

  it('signs the reader out when the refresh response carries no token', async () => {
    const auth = await loadAuth()
    storeSession('expired', Date.now() - MINUTE)
    fetchMock.mockResolvedValue({ success: false, data: null })

    expect(await auth.refreshToken()).toBe(false)
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })
})

describe('useWebsiteAuth refresh timer', () => {
  it('refreshes one minute before the access token expires', async () => {
    vi.useFakeTimers()
    const auth = await loadAuth()
    fetchMock.mockImplementation(async (url: string) => {
      if (url.endsWith('/login')) return refreshResponse('signed-in', 900)
      return refreshResponse('refreshed', 900)
    })

    expect((await auth.loginWithToken('magic')).success).toBe(true)
    expect(auth.accessToken.value).toBe('signed-in')

    await vi.advanceTimersByTimeAsync(14 * MINUTE - 1)
    expect(refreshCalls()).toHaveLength(0)

    await vi.advanceTimersByTimeAsync(1)
    expect(refreshCalls()).toHaveLength(1)
    expect(auth.accessToken.value).toBe('refreshed')

    // The new token schedules the next refresh
    await vi.advanceTimersByTimeAsync(14 * MINUTE)
    expect(refreshCalls()).toHaveLength(2)
  })

  it('stops the timer on sign-out', async () => {
    vi.useFakeTimers()
    const auth = await loadAuth()
    fetchMock.mockResolvedValue(refreshResponse('signed-in', 900))
    await auth.loginWithToken('magic')
    fetchMock.mockClear()

    await auth.logout()
    await vi.advanceTimersByTimeAsync(20 * MINUTE)

    expect(refreshCalls()).toHaveLength(0)
    expect(auth.accessToken.value).toBeNull()
  })
})

describe('useWebsiteAuth initAuth', () => {
  it('never calls the refresh endpoint for an anonymous visitor', async () => {
    const auth = await loadAuth()

    await auth.initAuth()

    expect(fetchMock).not.toHaveBeenCalled()
    expect(auth.isAuthenticated.value).toBe(false)
  })

  it('restores a valid stored session without a refresh', async () => {
    const auth = await loadAuth()
    storeSession('stored', Date.now() + 10 * MINUTE)

    await auth.initAuth()

    expect(fetchMock).not.toHaveBeenCalled()
    expect(auth.accessToken.value).toBe('stored')
  })

  it('refreshes an expired stored session once, however many callers ask', async () => {
    const auth = await loadAuth()
    storeSession('expired', Date.now() - MINUTE)
    fetchMock.mockResolvedValue(refreshResponse('fresh'))

    await Promise.all([auth.initAuth(), auth.initAuth(), auth.initAuth()])
    await auth.initAuth()

    expect(refreshCalls()).toHaveLength(1)
    expect(auth.accessToken.value).toBe('fresh')
  })
})

describe('useWebsiteAuth authFetch', () => {
  it('sends the access token', async () => {
    const auth = await loadAuth()
    storeSession('stored', Date.now() + 10 * MINUTE)
    fetchMock.mockResolvedValue({ data: 'ok' })

    await auth.authFetch('/api/v1/comments')

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/comments', expect.objectContaining({
      headers: { Authorization: 'Bearer stored' }
    }))
  })

  it('refreshes first when the token is about to expire', async () => {
    const auth = await loadAuth()
    storeSession('stored', Date.now() + 10 * MINUTE)
    await auth.initAuth()
    localStorage.setItem(EXPIRY_KEY, (Date.now() + 30 * 1000).toString())
    fetchMock.mockImplementation(async (url: string) =>
      url.endsWith('/refresh') ? refreshResponse('fresh') : { data: 'ok' })

    await auth.authFetch('/api/v1/comments')

    expect(fetchMock.mock.calls.map(([url]) => url.endsWith('/refresh') ? 'refresh' : url))
      .toEqual(['refresh', '/api/v1/comments'])
    expect(fetchMock.mock.calls[1]![1]).toMatchObject({ headers: { Authorization: 'Bearer fresh' } })
  })

  it('refreshes once and retries once after a 401', async () => {
    const auth = await loadAuth()
    storeSession('revoked', Date.now() + 10 * MINUTE)
    let commentCalls = 0
    fetchMock.mockImplementation(async (url: string) => {
      if (url.endsWith('/refresh')) return refreshResponse('fresh')
      commentCalls++
      if (commentCalls === 1) throw httpError(401)
      return { data: 'ok' }
    })

    expect(await auth.authFetch('/api/v1/comments')).toEqual({ data: 'ok' })
    expect(refreshCalls()).toHaveLength(1)
    expect(commentCalls).toBe(2)
    expect(fetchMock.mock.calls.at(-1)![1]).toMatchObject({ headers: { Authorization: 'Bearer fresh' } })
  })

  it('does not retry a second time when the retry also gets a 401', async () => {
    const auth = await loadAuth()
    storeSession('revoked', Date.now() + 10 * MINUTE)
    let commentCalls = 0
    fetchMock.mockImplementation(async (url: string) => {
      if (url.endsWith('/refresh')) return refreshResponse('fresh')
      commentCalls++
      throw httpError(401)
    })

    await expect(auth.authFetch('/api/v1/comments')).rejects.toMatchObject({ statusCode: 401 })
    expect(commentCalls).toBe(2)
    expect(refreshCalls()).toHaveLength(1)
  })

  it('signs the reader out and rethrows when the refresh after a 401 is rejected', async () => {
    const auth = await loadAuth()
    storeSession('revoked', Date.now() + 10 * MINUTE)
    fetchMock.mockRejectedValue(httpError(401))

    await expect(auth.authFetch('/api/v1/comments')).rejects.toMatchObject({ statusCode: 401 })
    expect(auth.isAuthenticated.value).toBe(false)
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })
})
