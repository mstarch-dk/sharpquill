import { describe, expect, it } from 'vitest'
import { formatFileSize, mediaUsageLabel, mediaUsageLink } from '../../app/utils/mediaUsage'
import type { MediaUsageDto } from '../../app/types/media'

function usage(overrides: Partial<MediaUsageDto>): MediaUsageDto {
  return { kind: 'content', title: 'Title', isPage: false, ...overrides }
}

describe('mediaUsageLabel', () => {
  it('describes each kind of usage', () => {
    expect(mediaUsageLabel(usage({ kind: 'featured_image' }))).toBe('Featured image')
    expect(mediaUsageLabel(usage({ kind: 'content' }))).toBe('Inline in content')
    expect(mediaUsageLabel(usage({ kind: 'layout' }))).toBe('Layout block')
    expect(mediaUsageLabel(usage({ kind: 'setting' }))).toBe('Site setting')
    expect(mediaUsageLabel(usage({ kind: 'author_avatar' }))).toBe('Author avatar')
  })

  it('falls back to the raw kind for a kind it does not know', () => {
    expect(mediaUsageLabel(usage({ kind: 'something_new' as MediaUsageDto['kind'] }))).toBe('something_new')
  })
})

describe('mediaUsageLink', () => {
  it('links articles and pages to their editor', () => {
    expect(mediaUsageLink(usage({ kind: 'featured_image', articleId: 7 }))).toBe('/articles/7/edit')
    expect(mediaUsageLink(usage({ kind: 'content', articleId: 8, isPage: true }))).toBe('/pages/8/edit')
  })

  it('links settings to the settings page and has no link for avatars', () => {
    expect(mediaUsageLink(usage({ kind: 'setting', title: 'site_logo' }))).toBe('/settings')
    expect(mediaUsageLink(usage({ kind: 'author_avatar', title: 'Admin' }))).toBeNull()
  })
})

describe('formatFileSize', () => {
  it('uses B, KB and MB', () => {
    expect(formatFileSize(512)).toBe('512 B')
    expect(formatFileSize(1536)).toBe('1.5 KB')
    expect(formatFileSize(5 * 1024 * 1024)).toBe('5.0 MB')
  })
})
