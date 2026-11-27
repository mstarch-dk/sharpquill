import { ArticleStatus } from './article'

export interface SeriesDto {
  id: number
  name: string
  slug: string
  description?: string
  articleCount: number
  themeSlug?: string
}

export interface SeriesAdminDetailDto {
  id: number
  name: string
  slug: string
  description?: string
  createdAt: string
  updatedAt?: string
  articleCount: number
  articles: SeriesArticleAdminDto[]
  themeSlug?: string
}

export interface SeriesArticleAdminDto {
  id: number
  title: string
  slug: string
  excerpt?: string
  publishedAt?: string
  status: ArticleStatus
  authorName: string
  order: number
}

export interface SeriesCreateDto {
  name: string
  slug: string
  description?: string
  themeSlug?: string
}

export interface SeriesUpdateDto {
  name: string
  slug: string
  description?: string
  themeSlug?: string
}

export interface SeriesAddArticleDto {
  articleId: number
  position?: number
}

export interface SeriesReorderDto {
  articleIds: number[]
}
