import { mockNuxtImport } from '@nuxt/test-utils/runtime'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteLocationNormalized } from 'vue-router'
import { author, callsTo, httpError, loadAuth, loginResponse, signIn, type FetchMock } from './helpers'

const { navigateToMock } = vi.hoisted(() => ({ navigateToMock: vi.fn((path: string) => ({ redirectedTo: path })) }))
mockNuxtImport('navigateTo', () => navigateToMock)

let fetchMock: FetchMock
const route = { path: '/articles', fullPath: '/articles' } as RouteLocationNormalized

beforeEach(() => {
  vi.resetModules()
  navigateToMock.mockClear()
  fetchMock = vi.fn()
  vi.stubGlobal('$fetch', fetchMock)
})

afterEach(() => {
  vi.unstubAllGlobals()
})

async function runMiddleware(name: 'auth' | 'guest') {
  const { default: middleware } = await import(`~/middleware/${name}.ts`)
  return middleware(route, route)
}

describe('auth middleware', () => {
  it('lets a signed-in author through', async () => {
    await signIn(fetchMock)

    expect(await runMiddleware('auth')).toBeUndefined()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('restores the session from the refresh cookie and loads the profile', async () => {
    const auth = await loadAuth()
    fetchMock.mockImplementation(async (url: string) =>
      url.endsWith('/refresh') ? loginResponse('restored') : { success: true, data: author })

    expect(await runMiddleware('auth')).toBeUndefined()
    expect(auth.accessToken.value).toBe('restored')
    expect(auth.user.value).toEqual(author)
    expect(navigateToMock).not.toHaveBeenCalled()
  })

  it('signs out and redirects to login when the profile cannot be loaded', async () => {
    const auth = await loadAuth()
    fetchMock.mockImplementation(async (url: string) => {
      if (url.endsWith('/refresh')) return loginResponse('restored')
      throw httpError(500)
    })

    expect(await runMiddleware('auth')).toEqual({ redirectedTo: '/login' })
    expect(auth.isAuthenticated.value).toBe(false)
  })

  it('falls back to certificate login when there is no session', async () => {
    const auth = await loadAuth()
    fetchMock.mockImplementation(async (url: string) => {
      if (url.endsWith('/refresh')) throw httpError(401)
      if (url.endsWith('/cert-login')) return loginResponse('cert-token')
      throw new Error(`unexpected ${url}`)
    })

    expect(await runMiddleware('auth')).toBeUndefined()
    expect(callsTo(fetchMock, '/cert-login')).toHaveLength(1)
    expect(auth.accessToken.value).toBe('cert-token')
    expect(navigateToMock).not.toHaveBeenCalled()
  })

  it('redirects to login when there is no session and no certificate', async () => {
    await loadAuth()
    fetchMock.mockRejectedValue(httpError(401))

    expect(await runMiddleware('auth')).toEqual({ redirectedTo: '/login' })
    expect(callsTo(fetchMock, '/cert-login')).toHaveLength(1)
  })
})

describe('guest middleware', () => {
  it('sends a signed-in author to the dashboard', async () => {
    await signIn(fetchMock)

    expect(await runMiddleware('guest')).toEqual({ redirectedTo: '/' })
  })

  it('sends an author with a valid refresh cookie to the dashboard', async () => {
    await loadAuth()
    fetchMock.mockResolvedValue(loginResponse('restored'))

    expect(await runMiddleware('guest')).toEqual({ redirectedTo: '/' })
  })

  it('shows the login page to a signed-out visitor', async () => {
    await loadAuth()
    fetchMock.mockRejectedValue(httpError(401))

    expect(await runMiddleware('guest')).toBeUndefined()
    expect(navigateToMock).not.toHaveBeenCalled()
  })
})
