import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { signIn, type FetchMock } from './helpers'

let fetchMock: FetchMock

beforeEach(() => {
  vi.resetModules()
  fetchMock = vi.fn()
  vi.stubGlobal('$fetch', fetchMock)
})

afterEach(() => {
  vi.unstubAllGlobals()
})

async function loadMedia() {
  await signIn(fetchMock)
  const { useAdminMedia } = await import('~/composables/useAdminMedia')
  fetchMock.mockResolvedValue({ success: true, data: [] })
  return useAdminMedia()
}

function lastCall() {
  const [url, options] = fetchMock.mock.calls.at(-1)!
  return { url, method: options?.method }
}

describe('useAdminMedia', () => {
  it('reads the usages of a media item', async () => {
    const media = await loadMedia()

    await media.getMediaUsages(5)

    expect(lastCall()).toEqual({ url: expect.stringMatching(/\/api\/admin\/media\/5\/usages$/), method: 'GET' })
  })

  it('deletes without force by default, so the API refuses media that is still in use', async () => {
    const media = await loadMedia()

    await media.deleteMedia(5)

    expect(lastCall()).toEqual({ url: expect.stringMatching(/\/api\/admin\/media\/5$/), method: 'DELETE' })
  })

  it('deletes media that is in use only when forced', async () => {
    const media = await loadMedia()

    await media.deleteMedia(5, true)

    expect(lastCall()).toEqual({ url: expect.stringMatching(/\/api\/admin\/media\/5\?force=true$/), method: 'DELETE' })
  })
})
