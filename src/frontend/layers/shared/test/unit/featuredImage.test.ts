import { describe, expect, it } from 'vitest'
import { getFeaturedImageContainerClass, normalizeFeaturedImageSize, type FeaturedImageSize }
  from '../../app/utils/featuredImage'

describe('normalizeFeaturedImageSize', () => {
  it.each(['extra-small', 'small', 'medium', 'original'] as const)('keeps %s', size => {
    expect(normalizeFeaturedImageSize(size)).toBe(size)
  })

  it('defaults a missing or unknown size to medium', () => {
    expect(normalizeFeaturedImageSize()).toBe('medium')
    expect(normalizeFeaturedImageSize('huge' as FeaturedImageSize)).toBe('medium')
  })
})

describe('getFeaturedImageContainerClass', () => {
  it.each([
    ['extra-small', 'max-w-[14rem] mx-auto'],
    ['small', 'max-w-md mx-auto'],
    ['medium', 'max-w-3xl mx-auto'],
    ['original', 'max-w-none']
  ] as const)('maps %s to %j', (size, expected) => {
    expect(getFeaturedImageContainerClass(size)).toBe(expected)
  })

  it('uses the medium width for a missing or unknown size', () => {
    expect(getFeaturedImageContainerClass()).toBe('max-w-3xl mx-auto')
    expect(getFeaturedImageContainerClass('huge' as FeaturedImageSize)).toBe('max-w-3xl mx-auto')
  })
})
