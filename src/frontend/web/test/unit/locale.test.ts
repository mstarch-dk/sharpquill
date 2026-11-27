import { describe, expect, it } from 'vitest'
import { resolveLocaleCode } from '../../app/utils/locale'

const available = ['da', 'en-GB'] as const

describe('resolveLocaleCode', () => {
  it('picks an exact match, ignoring case and surrounding whitespace', () => {
    expect(resolveLocaleCode('da', available, 'da')).toBe('da')
    expect(resolveLocaleCode('  EN-gb ', available, 'da')).toBe('en-GB')
  })

  it('falls back to a locale with the same primary language', () => {
    expect(resolveLocaleCode('en', available, 'da')).toBe('en-GB')
    expect(resolveLocaleCode('en-US', available, 'da')).toBe('en-GB')
    expect(resolveLocaleCode('da-DK', available, 'en-GB')).toBe('da')
  })

  it('prefers an exact match over a primary-language match', () => {
    expect(resolveLocaleCode('en-US', ['en-GB', 'en-US'], 'en-GB')).toBe('en-US')
  })

  it('returns the fallback for an empty or unknown language', () => {
    expect(resolveLocaleCode('', available, 'da')).toBe('da')
    expect(resolveLocaleCode('   ', available, 'da')).toBe('da')
    expect(resolveLocaleCode('de', available, 'da')).toBe('da')
  })
})
