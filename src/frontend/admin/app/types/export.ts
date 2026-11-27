export interface ExportRequestDto {
  contentType?: string | null
  articleIds?: number[] | null
  includeTags?: boolean
  includeCategories?: boolean
  includeSettings?: boolean
}

export interface ManifestContentCount {
  articles: number
  pages: number
}

export interface ManifestDataCount {
  tags: number
  categories: number
  settings: number
}

export interface ManifestDto {
  formatVersion: string
  exportedAt: string
  source: string
  contentCount: ManifestContentCount
  mediaCount: number
  dataCount?: ManifestDataCount | null
}

export interface ImportValidationDto {
  isValid: boolean
  manifest: ManifestDto | null
  conflicts: string[]
  warnings: string[]
  errors: string[]
}

export interface ImportResultDto {
  articlesImported: number
  pagesImported: number
  mediaImported: number
  categoriesCreated: number
  tagsCreated: number
  seriesCreated: number
  settingsImported: number
  skipped: number
  skippedSlugs: string[]
  errors: string[]
}
