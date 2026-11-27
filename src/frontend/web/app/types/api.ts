import type { ApiResponse, PaginationMeta } from '#layer/types/api'
import type { RenderedBlockDto, RenderedBlockItemDto, RenderedLayoutDto, RenderedSectionDto } from '#layer/types/layout'

// Shared with the admin app (layers/shared/app/types)
export type { ApiResponse, PaginationMeta, RenderedBlockDto, RenderedBlockItemDto, RenderedLayoutDto, RenderedSectionDto }

// Author
export interface AuthorDto {
  id: number
  name: string
  avatarUrl?: string
}

// Category
export interface CategoryDto {
  id: number
  name: string
  slug: string
  description?: string
}

// Tag
export interface TagDto {
  id: number
  name: string
  slug: string
}

export interface TagSummaryDto {
  id: number
  name: string
  slug: string
}

// Series
export interface SeriesDto {
  id: number
  name: string
  slug: string
  description?: string
  articleCount: number
  themeSlug?: string
}

export interface SeriesDetailDto {
  id: number
  name: string
  slug: string
  description?: string
  articles: SeriesArticleDto[]
  themeSlug?: string
}

export interface SeriesArticleDto {
  id: number
  title: string
  slug: string
  excerpt?: string
  publishedAt?: string
  order: number
}

export interface ArticleSeriesSummaryDto {
  id: number
  name: string
  slug: string
}

export interface ArticleSeriesInfoDto {
  seriesId: number
  seriesName: string
  seriesSlug: string
  currentOrder: number
  totalInSeries: number
  previous?: SeriesNavArticleDto
  next?: SeriesNavArticleDto
}

export interface SeriesNavArticleDto {
  id: number
  title: string
  slug: string
}

// Article List (summary for lists)
export interface ArticleListDto {
  id: number
  title: string
  slug: string
  excerpt?: string
  featuredImageUrl?: string
  featuredImageThumbnailUrl?: string
  publishedAt?: string
  author?: AuthorDto
  authorName?: string
  category?: CategoryDto
  tags: TagSummaryDto[]
  series?: ArticleSeriesSummaryDto
  requiresSignIn: boolean
}

// Site search (GET /api/v1/search)
export interface SearchSnippetPartDto {
  text: string
  /** true for words that matched the query */
  match: boolean
}

/** A search-as-you-type suggestion from GET /api/v1/search/suggest */
export interface SearchSuggestionDto {
  type: 'article' | 'tag' | 'category' | 'series'
  title: string
  /** Website path, e.g. /blog/{slug} or /tag/{slug} */
  url: string
}

export interface SearchResultDto {
  article: ArticleListDto
  snippet: SearchSnippetPartDto[]
}

// Article Detail (full content)
export interface ArticleDetailDto {
  id: number
  title: string
  slug: string
  content: string
  layout?: RenderedLayoutDto
  excerpt?: string
  featuredImageUrl?: string
  featuredImageMediumUrl?: string
  featuredImageDisplayUrl?: string
  featuredImageSize?: 'extra-small' | 'small' | 'medium' | 'original'
  publishedAt?: string
  metaTitle?: string
  metaDescription?: string
  author?: AuthorDto
  authorName?: string
  category?: CategoryDto
  tags: TagSummaryDto[]
  series?: ArticleSeriesInfoDto
  commentsEnabled: boolean
  requiresSignIn: boolean
  pageStyle?: string
}

// Comments
export interface CommentDto {
  id: number
  authorName: string
  content: string
  createdAt: string
  parentCommentId?: number
  replies: CommentDto[]
}

// Likes
export interface LikeStatusDto {
  count: number
  hasLiked: boolean
}
