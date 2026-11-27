import { describe, expect, it } from 'vitest'
// The size helpers shared with the website are tested in the layer (layers/shared/test/unit/featuredImage.test.ts)
import { getVariantFallbackMessage, resolveDisplayedVariant } from '../../app/utils/featuredImageVariants'

const original = '/uploads/2026/11/photo.jpg'
const medium = '/uploads/2026/11/photo_medium.jpg'
const thumb = '/uploads/2026/11/photo_thumb.jpg'

describe('resolveDisplayedVariant', () => {
  it('returns null without a display URL', () => {
    expect(resolveDisplayedVariant(undefined, original, medium)).toBeNull()
    expect(resolveDisplayedVariant('', original, medium)).toBeNull()
  })

  it('recognises the original and medium URLs by identity', () => {
    expect(resolveDisplayedVariant(original, original, medium)).toBe('original')
    expect(resolveDisplayedVariant(medium, original, medium)).toBe('medium')
  })

  it('recognises variants by their file name suffix', () => {
    expect(resolveDisplayedVariant(thumb, original, medium)).toBe('small')
    expect(resolveDisplayedVariant(medium)).toBe('medium')
  })

  it('returns null for an unrecognised URL', () => {
    expect(resolveDisplayedVariant('/uploads/other.jpg', original, medium)).toBeNull()
  })
})

describe('getVariantFallbackMessage', () => {
  it('returns null when nothing is selected or shown', () => {
    expect(getVariantFallbackMessage(undefined, thumb, original, medium)).toBeNull()
    expect(getVariantFallbackMessage('small', undefined, original, medium)).toBeNull()
  })

  it('returns null when the shown variant is the selected one', () => {
    expect(getVariantFallbackMessage('small', thumb, original, medium)).toBeNull()
    expect(getVariantFallbackMessage('medium', medium, original, medium)).toBeNull()
    expect(getVariantFallbackMessage('original', original, original, medium)).toBeNull()
  })

  it('accepts the thumbnail for extra-small', () => {
    expect(getVariantFallbackMessage('extra-small', thumb, original, medium)).toBeNull()
  })

  it.each(['small', 'extra-small'] as const)('explains a medium fallback for %s', size => {
    expect(getVariantFallbackMessage(size, medium, original, medium)).toMatch(/Thumbnail variant missing/)
  })

  it.each(['small', 'extra-small'] as const)('explains an original fallback for %s', size => {
    expect(getVariantFallbackMessage(size, original, original, medium)).toMatch(/No resized variants exist/)
  })

  it('explains an original fallback for medium', () => {
    expect(getVariantFallbackMessage('medium', original, original, undefined)).toMatch(/no medium variant exists/)
  })

  it('stays quiet when a larger size shows a smaller variant or the variant is unknown', () => {
    expect(getVariantFallbackMessage('original', medium, original, medium)).toBeNull()
    expect(getVariantFallbackMessage('medium', '/uploads/other.jpg', original, medium)).toBeNull()
  })
})
