import type { ApiResponse } from '~/types/api'
import type {
  NavigationSummaryDto,
  NavigationAdminDto,
  NavigationItemsBulkSaveDto
} from '~/types/navigation'

export function useAdminNavigation() {
  const api = useApi()
  const endpoint = '/api/admin/navigations'

  async function getNavigations(): Promise<ApiResponse<NavigationSummaryDto[]>> {
    return api.get<NavigationSummaryDto[]>(endpoint)
  }

  async function getNavigationById(id: number): Promise<ApiResponse<NavigationAdminDto>> {
    return api.get<NavigationAdminDto>(`${endpoint}/${id}`)
  }

  async function saveNavigationItems(id: number, data: NavigationItemsBulkSaveDto): Promise<ApiResponse<void>> {
    return api.put<void>(`${endpoint}/${id}/items`, data)
  }

  return {
    getNavigations,
    getNavigationById,
    saveNavigationItems
  }
}
