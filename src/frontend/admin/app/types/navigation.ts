export enum NavigationItemType {
  Page = 0,
  SystemRoute = 1,
  ExternalUrl = 2
}

export interface NavigationSummaryDto {
  id: number
  name: string
  slug: string
}

export interface NavigationAdminDto {
  id: number
  name: string
  slug: string
  items: NavigationItemAdminDto[]
}

export interface NavigationItemAdminDto {
  id: number
  label: string
  itemType: NavigationItemType
  articleId?: number
  articleTitle?: string
  articleSlug?: string
  systemRoute?: string
  url?: string
  parentId?: number
  position: number
  openInNewTab: boolean
}

export interface NavigationItemSaveDto {
  label: string
  itemType: NavigationItemType
  articleId?: number
  systemRoute?: string
  url?: string
  parentId?: number
  position: number
  openInNewTab: boolean
}

export interface NavigationItemsBulkSaveDto {
  items: NavigationItemSaveDto[]
}
