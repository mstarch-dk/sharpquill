import type { ApiResponse, PaginationMeta } from '#layer/types/api'

// Shared with the website (layers/shared/app/types)
export type { ApiResponse, PaginationMeta }

export interface ApiError {
  error: {
    code: string
    message: string
    details?: { field: string; message: string }[]
  }
}

export interface CategoryDto {
  id: number
  name: string
  slug: string
  description?: string
  articleCount: number
}

export interface CategoryCreateDto {
  name: string
  slug: string
  description?: string
}

export interface CategoryUpdateDto {
  name: string
  slug: string
  description?: string
}

export interface TagDto {
  id: number
  name: string
  slug: string
  articleCount: number
}

export interface TagCreateDto {
  name: string
  slug: string
}

export interface TagUpdateDto {
  name: string
  slug: string
}

export interface AuthorDto {
  id: number
  name: string
  email: string
  bio?: string
  avatarUrl?: string
}
