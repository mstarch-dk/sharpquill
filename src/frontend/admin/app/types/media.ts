export interface MediaAuthorDto {
  id: number
  name: string
}

export interface MediaDto {
  id: number
  filename: string
  originalFilename: string
  mimeType: string
  size: number
  url: string
  thumbnailUrl?: string
  mediumUrl?: string
  width?: number
  height?: number
  altText?: string
  createdAt: string
  author: MediaAuthorDto
}

export interface MediaUploadResultDto {
  id: number
  filename: string
  originalFilename: string
  mimeType: string
  size: number
  url: string
  thumbnailUrl?: string
  mediumUrl?: string
}

/** Where a media item is still used; see MediaUsageKind in the API (core/Media/MediaUsage.cs). */
export type MediaUsageKind = 'featured_image' | 'content' | 'layout' | 'setting' | 'author_avatar'

export interface MediaUsageDto {
  kind: MediaUsageKind
  /** The article or page, for featured_image, content and layout */
  articleId?: number
  /** Article or page title, setting key or author name */
  title: string
  isPage: boolean
}
