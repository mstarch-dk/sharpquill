import type { ApiResponse, ApiError } from '~/types/api'

interface FetchOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: BodyInit | object | null
  query?: Record<string, unknown>
  signal?: AbortSignal
}

export function useApi() {
  const config = useRuntimeConfig()
  const baseUrl = config.public.apiBase
  const { getAuthHeader, refreshToken, isTokenExpiringSoon, clearTokens } = useAuth()

  async function fetchWithAuth<T>(
    endpoint: string,
    options: FetchOptions = {}
  ): Promise<ApiResponse<T>> {
    // Refresh token if expiring soon
    if (isTokenExpiringSoon()) {
      await refreshToken()
    }

    const url = endpoint.startsWith('http') ? endpoint : `${baseUrl}${endpoint}`

    try {
      const response = await $fetch<ApiResponse<T>>(url, {
        method: options.method || 'GET',
        headers: getAuthHeader(),
        body: options.body,
        query: options.query,
        signal: options.signal,
        credentials: 'include'
      })

      return response
    } catch (error: unknown) {
      const fetchError = error as { status?: number; data?: ApiError }

      // Handle 401 - try refresh once
      if (fetchError.status === 401) {
        const refreshed = await refreshToken()
        if (refreshed) {
          // Retry the request with new token
          const response = await $fetch<ApiResponse<T>>(url, {
            method: options.method || 'GET',
            headers: getAuthHeader(),
            body: options.body,
            query: options.query,
            signal: options.signal,
            credentials: 'include'
          })
          return response
        } else {
          // Refresh failed, redirect to login
          clearTokens()
          if (import.meta.client) {
            navigateTo('/login')
          }
        }
      }

      throw error
    }
  }

  async function get<T>(endpoint: string, query?: Record<string, unknown>, signal?: AbortSignal): Promise<ApiResponse<T>> {
    return fetchWithAuth<T>(endpoint, { method: 'GET', query, signal })
  }

  async function post<T>(endpoint: string, body?: FetchOptions['body']): Promise<ApiResponse<T>> {
    return fetchWithAuth<T>(endpoint, { method: 'POST', body })
  }

  async function put<T>(endpoint: string, body?: FetchOptions['body']): Promise<ApiResponse<T>> {
    return fetchWithAuth<T>(endpoint, { method: 'PUT', body })
  }

  async function del<T>(endpoint: string): Promise<ApiResponse<T>> {
    return fetchWithAuth<T>(endpoint, { method: 'DELETE' })
  }

  return {
    get,
    post,
    put,
    del,
    fetchWithAuth
  }
}
