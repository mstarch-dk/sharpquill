import type { ApiResponse } from '~/types/api'
import type { WebsiteUserDto, WebsiteUserCountsDto, WebsiteLoginTokenDto } from '~/types/websiteUser'
import { WebsiteUserStatus } from '~/types/websiteUser'

export function useAdminWebsiteUsers() {
  const api = useApi()
  const endpoint = '/api/admin/website-users'

  async function getUsers(params: {
    page?: number
    pageSize?: number
    status?: WebsiteUserStatus | null
  } = {}): Promise<ApiResponse<WebsiteUserDto[]>> {
    const query = new URLSearchParams()
    if (params.page) query.set('page', params.page.toString())
    if (params.pageSize) query.set('pageSize', params.pageSize.toString())
    if (params.status !== undefined && params.status !== null) query.set('status', params.status.toString())
    const qs = query.toString()
    return api.get<WebsiteUserDto[]>(`${endpoint}${qs ? `?${qs}` : ''}`)
  }

  async function getCounts(): Promise<ApiResponse<WebsiteUserCountsDto>> {
    return api.get<WebsiteUserCountsDto>(`${endpoint}/counts`)
  }

  async function moderateUser(id: number, status: WebsiteUserStatus): Promise<void> {
    await api.put<void>(`${endpoint}/${id}/moderate`, { status })
  }

  async function generateLoginToken(id: number): Promise<ApiResponse<WebsiteLoginTokenDto>> {
    return api.post<WebsiteLoginTokenDto>(`${endpoint}/${id}/login-token`)
  }

  return {
    getUsers,
    getCounts,
    moderateUser,
    generateLoginToken
  }
}
