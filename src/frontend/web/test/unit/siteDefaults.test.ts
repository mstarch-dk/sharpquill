import { describe, expect, it } from 'vitest'
import { emptySiteSettings, resolveFooterText } from '../../app/utils/siteDefaults'

const settings = (overrides: Partial<ReturnType<typeof emptySiteSettings>>) => ({ ...emptySiteSettings(), ...overrides })

describe('resolveFooterText', () => {
  it('uses the custom footer text and replaces every {year}', () => {
    expect(resolveFooterText(settings({ footerText: '  © {year} Acme. Since 2001, still here in {year}.  ' }), 2026))
      .toBe('© 2026 Acme. Since 2001, still here in 2026.')
  })

  it('falls back to "© <year> <site name>" when the footer text is blank', () => {
    expect(resolveFooterText(settings({ footerText: '   ', siteName: '  Acme Blog ' }), 2026)).toBe('© 2026 Acme Blog')
  })

  it('falls back to "© <year>" when footer text and site name are blank', () => {
    expect(resolveFooterText(settings({ siteName: ' ' }), 2026)).toBe('© 2026')
  })

  it('leaves custom text without a placeholder unchanged', () => {
    expect(resolveFooterText(settings({ footerText: 'All rights reserved' }), 2026)).toBe('All rights reserved')
  })
})

describe('emptySiteSettings', () => {
  it('returns a fresh object every time', () => {
    const a = emptySiteSettings()
    a.siteName = 'changed'
    expect(emptySiteSettings().siteName).toBe('')
  })
})
