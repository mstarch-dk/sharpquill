import type { ApiResponse } from '~/types/api'

interface WebsiteUserProfile {
  id: number
  name: string
  email: string
}

interface WebsiteLoginResponse {
  accessToken: string
  expiresIn: number
  user: WebsiteUserProfile
}

type FetchOptions = NonNullable<Parameters<typeof $fetch>[1]>

const TOKEN_KEY = 'cms_website_token'
const TOKEN_EXPIRY_KEY = 'cms_website_token_expiry'

// Refresh this long before the access token expires (it lives 15 minutes by default)
const REFRESH_MARGIN_MS = 60 * 1000

// Client-only shared state. The API rotates the refresh token on every refresh and treats a late replay of a
// rotated token as theft, so all callers in this tab share one in-flight refresh and one init.
let refreshInFlight: Promise<boolean> | null = null
let initPromise: Promise<void> | null = null
let refreshTimer: ReturnType<typeof setTimeout> | null = null
let visibilityListenerAdded = false

function readStoredSession(): { token: string; expiry: number } | null {
  const token = localStorage.getItem(TOKEN_KEY)
  const expiry = parseInt(localStorage.getItem(TOKEN_EXPIRY_KEY) ?? '', 10)
  return token && Number.isFinite(expiry) ? { token, expiry } : null
}

function statusOf(error: unknown): number | undefined {
  const e = error as { statusCode?: number; status?: number; response?: { status?: number } }
  return e?.statusCode ?? e?.status ?? e?.response?.status
}

export function useWebsiteAuth() {
  const apiBase = useApiBase()
  const baseUrl = `${apiBase}/api/v1/auth`
  const { $i18n } = useNuxtApp()

  const accessToken = useState<string | null>('website_auth_token', () => null)
  const user = useState<WebsiteUserProfile | null>('website_auth_user', () => null)
  const isAuthenticated = computed(() => !!accessToken.value)

  function scheduleRefresh(expiry: number) {
    if (refreshTimer) clearTimeout(refreshTimer)
    const delay = Math.max(expiry - REFRESH_MARGIN_MS - Date.now(), 0)
    refreshTimer = setTimeout(() => { void refreshToken() }, delay)
  }

  function setTokens(token: string, expiresIn: number) {
    accessToken.value = token
    const expiryTime = Date.now() + expiresIn * 1000

    if (import.meta.client) {
      localStorage.setItem(TOKEN_KEY, token)
      localStorage.setItem(TOKEN_EXPIRY_KEY, expiryTime.toString())
      scheduleRefresh(expiryTime)
    }
  }

  function clearTokens() {
    accessToken.value = null
    user.value = null

    if (import.meta.client) {
      localStorage.removeItem(TOKEN_KEY)
      localStorage.removeItem(TOKEN_EXPIRY_KEY)
      if (refreshTimer) clearTimeout(refreshTimer)
      refreshTimer = null
    }
  }

  /**
   * Exchanges the httpOnly refresh cookie for a new access token. Concurrent calls share one request.
   * Signs the reader out when the API rejects the refresh token; a network error keeps the stored session so a
   * later attempt can still succeed.
   */
  function refreshToken(): Promise<boolean> {
    if (!import.meta.client) return Promise.resolve(false)
    if (refreshInFlight) return refreshInFlight

    refreshInFlight = (async () => {
      // Another tab may already have refreshed: adopt its token instead of rotating again
      const stored = readStoredSession()
      if (stored && stored.expiry - REFRESH_MARGIN_MS > Date.now() && stored.token !== accessToken.value) {
        accessToken.value = stored.token
        scheduleRefresh(stored.expiry)
        return true
      }

      try {
        const response = await $fetch<ApiResponse<WebsiteLoginResponse>>(`${baseUrl}/refresh`, {
          method: 'POST',
          credentials: 'include'
        })
        if (response.data) {
          setTokens(response.data.accessToken, response.data.expiresIn)
          user.value = response.data.user
          return true
        }
        clearTokens()
        return false
      } catch (error) {
        const status = statusOf(error)
        if (status !== undefined && status >= 400 && status < 500) {
          clearTokens()
        }
        return false
      }
    })().finally(() => {
      refreshInFlight = null
    })

    return refreshInFlight
  }

  /**
   * Restores the session from localStorage, refreshing it when the stored access token has expired. Only a reader
   * who signed in before has a stored session, so anonymous visitors never call the refresh endpoint.
   * Safe to call from several components: the work runs once per page load.
   */
  function initAuth(): Promise<void> {
    if (!import.meta.client) return Promise.resolve()
    if (initPromise) return initPromise

    initPromise = (async () => {
      if (!visibilityListenerAdded) {
        visibilityListenerAdded = true
        // Timers are throttled in background tabs; catch up when the reader returns
        document.addEventListener('visibilitychange', () => {
          if (document.visibilityState === 'visible' && accessToken.value && isTokenExpiringSoon()) {
            void refreshToken()
          }
        })
      }

      const stored = readStoredSession()
      if (!stored) return

      if (stored.expiry - REFRESH_MARGIN_MS > Date.now()) {
        accessToken.value = stored.token
        scheduleRefresh(stored.expiry)
      } else {
        await refreshToken()
      }
    })()

    return initPromise
  }

  function getAuthHeader(): Record<string, string> {
    if (accessToken.value) {
      return { Authorization: `Bearer ${accessToken.value}` }
    }
    return {}
  }

  /**
   * $fetch with the reader's access token. Refreshes first when the token is about to expire, and on a 401
   * refreshes once and retries; if the refresh fails the reader is signed out and the error is rethrown.
   */
  async function authFetch<T>(url: string, options: FetchOptions = {}): Promise<T> {
    await initAuth()
    if (accessToken.value && isTokenExpiringSoon()) {
      await refreshToken()
    }

    const send = () => $fetch<T>(url, {
      ...options,
      headers: { ...(options.headers as Record<string, string> | undefined), ...getAuthHeader() }
    } as FetchOptions) as Promise<T>

    try {
      return await send()
    } catch (error) {
      if (statusOf(error) === 401 && accessToken.value && await refreshToken()) {
        return await send()
      }
      throw error
    }
  }

  async function signup(name: string, email: string): Promise<{ success: boolean; error?: string }> {
    try {
      await $fetch(`${baseUrl}/signup`, {
        method: 'POST',
        body: { name, email }
      })
      return { success: true }
    } catch (error: unknown) {
      const apiError = error as { data?: { error?: { message?: string } } }
      return {
        success: false,
        error: apiError.data?.error?.message || $i18n.t('auth.signupFailed')
      }
    }
  }

  async function loginWithToken(token: string): Promise<{ success: boolean; error?: string }> {
    try {
      const response = await $fetch<ApiResponse<WebsiteLoginResponse>>(`${baseUrl}/login`, {
        method: 'POST',
        body: { token },
        credentials: 'include'
      })

      if (response.data) {
        setTokens(response.data.accessToken, response.data.expiresIn)
        user.value = response.data.user
        return { success: true }
      }

      return { success: false, error: $i18n.t('auth.invalidResponse') }
    } catch (error: unknown) {
      const apiError = error as { data?: { error?: { message?: string } } }
      return {
        success: false,
        error: apiError.data?.error?.message || $i18n.t('auth.loginFailed')
      }
    }
  }

  async function fetchProfile(): Promise<boolean> {
    try {
      const response = await authFetch<ApiResponse<WebsiteUserProfile>>(`${baseUrl}/profile`)
      if (response.data) {
        user.value = response.data
        return true
      }
      return false
    } catch {
      return false
    }
  }

  async function logout(): Promise<void> {
    try {
      await $fetch(`${baseUrl}/logout`, {
        method: 'POST',
        headers: getAuthHeader(),
        credentials: 'include'
      })
    } catch {
      // Ignore logout errors
    } finally {
      clearTokens()
    }
  }

  function isTokenExpiringSoon(): boolean {
    if (import.meta.client) {
      const stored = readStoredSession()
      if (stored) {
        return Date.now() > stored.expiry - REFRESH_MARGIN_MS
      }
    }
    return false
  }

  return {
    accessToken: readonly(accessToken),
    user: readonly(user),
    isAuthenticated,
    initAuth,
    signup,
    loginWithToken,
    logout,
    refreshToken,
    fetchProfile,
    authFetch,
    getAuthHeader,
    isTokenExpiringSoon,
    clearTokens
  }
}
