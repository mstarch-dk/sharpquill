import { mockNuxtImport } from '@nuxt/test-utils/runtime'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { callsTo, httpError, loginResponse, MINUTE, signIn, type FetchMock } from './helpers'

const { navigateToMock } = vi.hoisted(() => ({ navigateToMock: vi.fn() }))
mockNuxtImport('navigateTo', () => navigateToMock)

let fetchMock: FetchMock

beforeEach(() => {
  vi.resetModules()
  navigateToMock.mockReset()
  fetchMock = vi.fn()
  vi.stubGlobal('$fetch', fetchMock)
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

async function loadApi() {
  const { useApi } = await import('~/composables/useApi')
  return useApi()
}

function articleCalls() {
  return callsTo(fetchMock, '/api/admin/articles')
}

describe('useApi', () => {
  it('sends the access token and the cookies', async () => {
    await signIn(fetchMock, 'access')
    const api = await loadApi()
    fetchMock.mockResolvedValue({ success: true, data: [] })

    await api.get('/api/admin/articles', { page: 2 })

    expect(fetchMock).toHaveBeenCalledWith(expect.stringMatching(/\/api\/admin\/articles$/), expect.objectContaining({
      method: 'GET',
      headers: { Authorization: 'Bearer access' },
      query: { page: 2 },
      credentials: 'include'
    }))
  })

  it('refreshes before the request when the token is about to expire', async () => {
    vi.useFakeTimers()
    const auth = await signIn(fetchMock, 'old', 15 * 60)
    const api = await loadApi()
    vi.advanceTimersByTime(11 * MINUTE)
    fetchMock.mockImplementation(async (url: string) =>
      url.endsWith('/refresh') ? loginResponse('fresh') : { success: true, data: [] })

    await api.get('/api/admin/articles')

    expect(fetchMock.mock.calls.map(([url]) => url.replace(/^.*\/api\/admin\//, ''))).toEqual(['auth/refresh', 'articles'])
    expect(articleCalls()[0]![1]).toMatchObject({ headers: { Authorization: 'Bearer fresh' } })
    expect(auth.accessToken.value).toBe('fresh')
  })

  it('does not refresh a token that is still valid', async () => {
    await signIn(fetchMock, 'access', 15 * 60)
    const api = await loadApi()
    fetchMock.mockResolvedValue({ success: true, data: [] })

    await api.get('/api/admin/articles')

    expect(callsTo(fetchMock, '/refresh')).toHaveLength(0)
  })

  it('refreshes and retries exactly once after a 401', async () => {
    await signIn(fetchMock, 'revoked')
    const api = await loadApi()
    fetchMock.mockImplementation(async (url: string) => {
      if (url.endsWith('/refresh')) return loginResponse('fresh')
      if (articleCalls().length === 1) throw httpError(401)
      return { success: true, data: ['article'] }
    })

    expect(await api.get('/api/admin/articles')).toEqual({ success: true, data: ['article'] })
    expect(callsTo(fetchMock, '/refresh')).toHaveLength(1)
    expect(articleCalls()).toHaveLength(2)
    expect(articleCalls()[1]![1]).toMatchObject({ headers: { Authorization: 'Bearer fresh' } })
  })

  it('does not retry a second time when the retry also gets a 401', async () => {
    await signIn(fetchMock, 'revoked')
    const api = await loadApi()
    fetchMock.mockImplementation(async (url: string) => {
      if (url.endsWith('/refresh')) return loginResponse('fresh')
      throw httpError(401)
    })

    await expect(api.get('/api/admin/articles')).rejects.toMatchObject({ status: 401 })
    expect(articleCalls()).toHaveLength(2)
    expect(callsTo(fetchMock, '/refresh')).toHaveLength(1)
  })

  it('shares one refresh between parallel requests that all get a 401', async () => {
    await signIn(fetchMock, 'revoked')
    const api = await loadApi()
    fetchMock.mockImplementation(async (url: string, options?: Record<string, unknown>) => {
      if (url.endsWith('/refresh')) return loginResponse('fresh')
      const headers = options?.headers as Record<string, string>
      if (headers.Authorization === 'Bearer revoked') throw httpError(401)
      return { success: true, data: url }
    })

    await Promise.all([api.get('/api/admin/articles'), api.get('/api/admin/tags'), api.get('/api/admin/categories')])

    expect(callsTo(fetchMock, '/refresh')).toHaveLength(1)
  })

  it('signs out and goes to the login page when the refresh after a 401 fails', async () => {
    const auth = await signIn(fetchMock, 'revoked')
    const api = await loadApi()
    fetchMock.mockRejectedValue(httpError(401))

    await expect(api.del('/api/admin/articles/1')).rejects.toMatchObject({ status: 401 })
    expect(auth.isAuthenticated.value).toBe(false)
    expect(navigateToMock).toHaveBeenCalledWith('/login')
  })

  it('passes other errors through without refreshing', async () => {
    const auth = await signIn(fetchMock, 'access')
    const api = await loadApi()
    fetchMock.mockRejectedValue(httpError(404))

    await expect(api.get('/api/admin/articles/99')).rejects.toMatchObject({ status: 404 })
    expect(callsTo(fetchMock, '/refresh')).toHaveLength(0)
    expect(auth.isAuthenticated.value).toBe(true)
    expect(navigateToMock).not.toHaveBeenCalled()
  })
})

describe('uploads go through useApi', () => {
  it('a media upload is retried with a fresh token after a 401', async () => {
    await signIn(fetchMock, 'revoked')
    const { useAdminMedia } = await import('~/composables/useAdminMedia')
    const media = useAdminMedia()
    let uploads = 0
    fetchMock.mockImplementation(async (url: string) => {
      if (url.endsWith('/refresh')) return loginResponse('fresh')
      if (++uploads === 1) throw httpError(401)
      return { success: true, data: { id: 7 } }
    })

    const result = await media.uploadMedia(new File(['png'], 'a.png', { type: 'image/png' }))

    expect(result).toEqual({ success: true, data: { id: 7 } })
    expect(uploads).toBe(2)
    const [, options] = callsTo(fetchMock, '/api/admin/media')[1]!
    expect(options).toMatchObject({ method: 'POST', headers: { Authorization: 'Bearer fresh' } })
    expect(options!.body).toBeInstanceOf(FormData)
  })

  it.each([
    ['validateImport', '/api/admin/import/validate'],
    ['importContent', '/api/admin/import']
  ] as const)('%s posts the package with the access token', async (method, path) => {
    await signIn(fetchMock, 'access')
    const { useContentPackage } = await import('~/composables/useContentPackage')
    const contentPackage = useContentPackage()
    fetchMock.mockResolvedValue({ success: true, data: {} })

    await contentPackage[method](new File(['zip'], 'package.zip'))

    const [url, options] = fetchMock.mock.calls[0]!
    expect(url.endsWith(path)).toBe(true)
    expect(options).toMatchObject({ method: 'POST', headers: { Authorization: 'Bearer access' } })
    expect(options!.body).toBeInstanceOf(FormData)
  })
})
