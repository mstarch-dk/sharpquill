// vitest.config.ts pins TZ=UTC
import { describe, expect, it } from 'vitest'
import { formatDate, formatDateISO, formatDateShort } from '../../app/utils/date'

const noon = '2026-03-05T12:00:00Z'

describe('formatDate', () => {
  it('formats a long date for the locale', () => {
    expect(formatDate(noon, 'da')).toBe('5. marts 2026')
    expect(formatDate(noon, 'en-US')).toBe('March 5, 2026')
    expect(formatDate(noon, 'en-GB')).toBe('5 March 2026')
  })

  it('returns an empty string for a missing date', () => {
    expect(formatDate(undefined, 'da')).toBe('')
    expect(formatDate('', 'da')).toBe('')
  })
})

describe('formatDateShort', () => {
  it('formats a short date for the locale', () => {
    expect(formatDateShort(noon, 'da')).toBe('5. mar. 2026')
    expect(formatDateShort(noon, 'en-US')).toBe('Mar 5, 2026')
  })

  it('returns an empty string for a missing date', () => {
    expect(formatDateShort(undefined, 'en')).toBe('')
  })
})

describe('formatDateISO', () => {
  it('returns the UTC calendar date', () => {
    expect(formatDateISO(noon)).toBe('2026-03-05')
    expect(formatDateISO('2026-03-05T01:00:00+02:00')).toBe('2026-03-04')
  })

  it('returns an empty string for a missing date', () => {
    expect(formatDateISO(undefined)).toBe('')
  })
})
