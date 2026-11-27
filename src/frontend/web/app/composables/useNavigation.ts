import type { ApiResponse } from '~/types/api'

export interface NavigationItemDto {
  id: number
  label: string
  url: string
  parentId?: number
  position: number
  openInNewTab: boolean
}

export interface NavigationDto {
  id: number
  name: string
  slug: string
  items: NavigationItemDto[]
}

export function useNavigation() {
  const apiBase = useApiBase()
  const baseUrl = `${apiBase}/api/v1/navigations`

  async function getNavigationBySlug(slug: string) {
    return useFetch<ApiResponse<NavigationDto>>(`${baseUrl}/${slug}`, {
      key: `navigation-${slug}`
    })
  }

  return {
    getNavigationBySlug
  }
}
