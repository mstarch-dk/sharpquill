import type { LoginRequest, LoginResponse, AuthorProfile } from '~/types/auth'
import type { ApiResponse, ApiError } from '~/types/api'

// Earlier versions kept the access token in localStorage, where any script on the origin could read it.
// The token now lives in memory only; a page reload restores the session through the HttpOnly refresh cookie.
const LEGACY_TOKEN_KEYS = ['cms_admin_token', 'cms_admin_token_expiry']

// One restore attempt per page load, shared by every route middleware that asks for it
let restorePromise: Promise<void> | null = null

// The API rotates the refresh token on every refresh and treats a replay of a rotated token as theft (it revokes the
// whole session), so concurrent callers - e.g. several requests that all got a 401 - share one in-flight refresh.
let refreshInFlight: Promise<boolean> | null = null

function statusOf(error: unknown): number | undefined {
  const e = error as { statusCode?: number; status?: number; response?: { status?: number } }
  return e?.statusCode ?? e?.status ?? e?.response?.status
}

export function useAuth() {
  const config = useRuntimeConfig()
  const baseUrl = `${config.public.apiBase}/api/admin/auth`

  const accessToken = useState<string | null>('auth_token', () => null)
  const tokenExpiry = useState<number | null>('auth_token_expiry', () => null)
  const user = useState<AuthorProfile | null>('auth_user', () => null)
  const isAuthenticated = computed(() => !!accessToken.value)
  const isAdmin = computed(() => user.value?.role === 'admin')

  function purgeLegacyStorage() {
    try {
      LEGACY_TOKEN_KEYS.forEach(key => localStorage.removeItem(key))
    } catch {
      // Storage may be unavailable (private mode, blocked); nothing to clean up then
    }
  }

  /**
   * Makes sure the in-memory session is populated. After a full page load there is no token in memory, so the
   * refresh cookie is exchanged for a new one - once per page load, not on every navigation.
   */
  async function initAuth(): Promise<void> {
    if (!import.meta.client) return

    purgeLegacyStorage()

    if (accessToken.value) return

    restorePromise ??= refreshToken().then(() => undefined)
    await restorePromise
  }

  function setTokens(token: string, expiresIn: number) {
    accessToken.value = token
    tokenExpiry.value = Date.now() + expiresIn * 1000
  }

  function clearTokens() {
    accessToken.value = null
    tokenExpiry.value = null
    user.value = null
  }

  function getAuthHeader(): Record<string, string> {
    if (accessToken.value) {
      return { Authorization: `Bearer ${accessToken.value}` }
    }
    return {}
  }

  async function login(credentials: LoginRequest): Promise<{ success: boolean; error?: string }> {
    try {
      const response = await $fetch<ApiResponse<LoginResponse>>(`${baseUrl}/login`, {
        method: 'POST',
        body: credentials,
        credentials: 'include'
      })

      if (response.data) {
        setTokens(response.data.accessToken, response.data.expiresIn)
        user.value = response.data.author
        return { success: true }
      }

      return { success: false, error: 'Invalid response from server' }
    } catch (error: unknown) {
      const apiError = error as { data?: ApiError }
      return {
        success: false,
        error: apiError.data?.error?.message || 'Login failed. Please check your credentials.'
      }
    }
  }

  async function certLogin(): Promise<{ success: boolean; error?: string }> {
    try {
      const response = await $fetch<ApiResponse<LoginResponse>>(`${baseUrl}/cert-login`, {
        method: 'POST',
        credentials: 'include'
      })

      if (response.data) {
        setTokens(response.data.accessToken, response.data.expiresIn)
        user.value = response.data.author
        return { success: true }
      }

      return { success: false, error: 'Invalid response from server' }
    } catch {
      return { success: false, error: 'Certificate authentication unavailable' }
    }
  }

  async function fetchProfile(): Promise<boolean> {
    try {
      const response = await $fetch<ApiResponse<AuthorProfile>>(`${baseUrl}/profile`, {
        headers: getAuthHeader()
      })
      if (response.data) {
        user.value = response.data
        return true
      }
      return false
    } catch {
      return false
    }
  }

  /**
   * Exchanges the HttpOnly refresh cookie for a new access token. Concurrent calls share one request.
   * Signs the author out when the API rejects the refresh token; a network or server error keeps the session so a
   * later attempt can still succeed.
   */
  function refreshToken(): Promise<boolean> {
    if (refreshInFlight) return refreshInFlight

    refreshInFlight = (async () => {
      try {
        const response = await $fetch<ApiResponse<LoginResponse>>(`${baseUrl}/refresh`, {
          method: 'POST',
          credentials: 'include'
        })

        if (response.data) {
          setTokens(response.data.accessToken, response.data.expiresIn)
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
    if (tokenExpiry.value === null) return false
    const fiveMinutes = 5 * 60 * 1000
    return Date.now() > tokenExpiry.value - fiveMinutes
  }

  return {
    accessToken: readonly(accessToken),
    user: readonly(user),
    isAuthenticated,
    isAdmin,
    initAuth,
    login,
    certLogin,
    logout,
    refreshToken,
    getAuthHeader,
    isTokenExpiringSoon,
    clearTokens,
    fetchProfile
  }
}
