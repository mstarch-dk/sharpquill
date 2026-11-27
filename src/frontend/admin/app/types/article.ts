import type { AuthorDto, CategoryDto, TagDto } from './api'
import type { FeaturedImageSize } from '#layer/utils/featuredImage'
import type {
  BlockInputDto, BlockItemDto, LayoutInputDto, RenderedBlockDto, RenderedBlockItemDto, RenderedLayoutDto,
  RenderedSectionDto, SectionInputDto
} from '#layer/types/layout'

// Shared with the website (layers/shared/app)
export type {
  BlockInputDto, BlockItemDto, FeaturedImageSize, LayoutInputDto, RenderedBlockDto, RenderedBlockItemDto,
  RenderedLayoutDto, RenderedSectionDto, SectionInputDto
}

export enum ArticleStatus {
  Draft = 0,
  Published = 1,
  Archived = 2
}

export enum ContentType {
  Article = 0,
  Page = 1
}

export interface ArticlePreviewDto {
  id: number
  title: string
  slug: string
  content?: string
  layout?: RenderedLayoutDto
  excerpt?: string
  featuredImageUrl?: string
  featuredImageMediumUrl?: string
  featuredImageDisplayUrl?: string
  featuredImageSize?: FeaturedImageSize
  pageStyle?: string
  publishedAt?: string
  metaTitle?: string
  metaDescription?: string
  author?: { id: number; name: string; avatarUrl?: string }
  authorName?: string
  category?: { id: number; name: string; slug: string; description?: string }
  tags: { id: number; name: string; slug: string }[]
  commentsEnabled: boolean
}

// ---

export interface ArticleSeriesSummaryDto {
  id: number
  name: string
  slug: string
}

export interface ArticleAdminDto {
  id: number
  title: string
  slug: string
  contentType: ContentType
  content?: string
  layout?: LayoutInputDto
  excerpt?: string
  featuredImageId?: number
  featuredImageUrl?: string
  featuredImageDisplayUrl?: string
  featuredImageSize?: FeaturedImageSize
  metaTitle?: string
  metaDescription?: string
  status: ArticleStatus
  publishedAt?: string
  createdAt: string
  updatedAt: string
  author?: AuthorDto
  authorName?: string
  category?: CategoryDto
  tags: TagDto[]
  series?: ArticleSeriesSummaryDto
  seriesOrder?: number
  versionNumber?: number
  totalVersions: number
  publishedVersionId?: number
  commentsEnabled: boolean
  requiresSignIn: boolean
  pageStyle?: string
}

export interface ArticleCreateDto {
  title: string
  slug: string
  content?: string
  layout?: LayoutInputDto
  excerpt?: string
  featuredImageId?: number
  metaTitle?: string
  metaDescription?: string
  categoryId?: number
  tagIds?: number[]
  status: ArticleStatus
  commentsEnabled?: boolean
  requiresSignIn?: boolean
  pageStyle?: string
  featuredImageSize?: FeaturedImageSize
  authorName?: string
  publishedAt?: string
}

export interface ArticleUpdateDto {
  title: string
  slug: string
  content?: string
  layout?: LayoutInputDto
  excerpt?: string
  featuredImageId?: number
  metaTitle?: string
  metaDescription?: string
  categoryId?: number
  tagIds?: number[]
  status: ArticleStatus
  commentsEnabled?: boolean
  requiresSignIn?: boolean
  pageStyle?: string
  featuredImageSize?: FeaturedImageSize
  authorName?: string
  publishedAt?: string
}

export interface ArticleVersionDto {
  id: number
  versionNumber: number
  createdAt: string
}

export interface ArticleVersionDetailDto {
  id: number
  versionNumber: number
  content?: string
  layout?: LayoutInputDto
  excerpt?: string
  metaTitle?: string
  metaDescription?: string
  createdAt: string
  pageStyle?: string
  featuredImageSize?: FeaturedImageSize
}

/**
 * A Published article whose publish date is still in the future is "scheduled": the public site hides it until
 * that moment (the API compares against the database clock) and shows it automatically afterwards.
 */
export function isScheduled(status: ArticleStatus, publishedAt?: string | null): boolean {
  return status === ArticleStatus.Published && !!publishedAt && new Date(publishedAt).getTime() > Date.now()
}

/** Date and time in the admin's local time zone, e.g. "Nov 24, 2026, 9:00 AM". */
export function formatDateTime(value: string): string {
  return new Date(value).toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit'
  })
}

export function getStatusLabel(status: ArticleStatus, publishedAt?: string | null): string {
  if (isScheduled(status, publishedAt)) return 'Scheduled'
  switch (status) {
    case ArticleStatus.Draft:
      return 'Draft'
    case ArticleStatus.Published:
      return 'Published'
    case ArticleStatus.Archived:
      return 'Archived'
    default:
      return 'Unknown'
  }
}

export function getStatusColor(status: ArticleStatus, publishedAt?: string | null): string {
  if (isScheduled(status, publishedAt)) return 'bg-blue-100 text-blue-800'
  switch (status) {
    case ArticleStatus.Draft:
      return 'bg-yellow-100 text-yellow-800'
    case ArticleStatus.Published:
      return 'bg-green-100 text-green-800'
    case ArticleStatus.Archived:
      return 'bg-gray-100 text-gray-800'
    default:
      return 'bg-gray-100 text-gray-800'
  }
}

/** Status choices of the admin article list; published and scheduled split by whether the publish date has passed. */
export type ArticleListStatusFilter = 'draft' | 'published' | 'scheduled' | 'archived'

/** Sort orders of the admin article list; relevance needs a search text (otherwise it is newest). */
export type ArticleListSort = 'relevance' | 'newest' | 'oldest' | 'updated' | 'title'

/** Optional filters of GET /api/admin/articles; every set filter must match. */
export interface ArticleListFilters {
  search?: string
  status?: ArticleListStatusFilter
  categoryId?: number
  authorId?: number
  sort?: ArticleListSort
}
