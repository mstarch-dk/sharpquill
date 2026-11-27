import type { MediaUsageDto } from '~/types/media'

const kindLabels: Record<MediaUsageDto['kind'], string> = {
  featured_image: 'Featured image',
  content: 'Inline in content',
  layout: 'Layout block',
  setting: 'Site setting',
  author_avatar: 'Author avatar'
}

/** How a usage is described in the admin, e.g. "Featured image" */
export function mediaUsageLabel(usage: MediaUsageDto): string {
  return kindLabels[usage.kind] ?? usage.kind
}

/** The admin page where the usage can be fixed, or null when there is none to link to */
export function mediaUsageLink(usage: MediaUsageDto): string | null {
  if (usage.articleId != null) {
    return `/${usage.isPage ? 'pages' : 'articles'}/${usage.articleId}/edit`
  }
  if (usage.kind === 'setting') return '/settings'
  return null
}

/** Human-readable file size, e.g. "1.2 MB" */
export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}
