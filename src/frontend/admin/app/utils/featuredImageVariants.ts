import { normalizeFeaturedImageSize, type FeaturedImageSize } from '#layer/utils/featuredImage'

// Admin-only: explains in the editor which variant the website will actually show. The size helpers the website
// uses as well (normalizeFeaturedImageSize, getFeaturedImageContainerClass) live in the shared layer.

export function resolveDisplayedVariant(
  displayUrl?: string,
  originalUrl?: string,
  mediumUrl?: string
): FeaturedImageSize | null {
  if (!displayUrl) return null

  if (originalUrl && displayUrl === originalUrl) return 'original'
  if (mediumUrl && displayUrl === mediumUrl) return 'medium'
  if (displayUrl.includes('_thumb')) return 'small'
  if (displayUrl.includes('_medium')) return 'medium'

  return null
}

export function getVariantFallbackMessage(
  selectedSize?: FeaturedImageSize,
  displayUrl?: string,
  originalUrl?: string,
  mediumUrl?: string
): string | null {
  if (!selectedSize || !displayUrl) return null

  const selected = normalizeFeaturedImageSize(selectedSize)
  const resolved = resolveDisplayedVariant(displayUrl, originalUrl, mediumUrl)
  if (!resolved || resolved === selected) return null

  if (selected === 'extra-small' && resolved === 'small') return null

  if ((selected === 'small' || selected === 'extra-small') && resolved === 'medium') {
    return 'Thumbnail variant missing, so the medium image is shown (scaled down).'
  }

  if ((selected === 'small' || selected === 'extra-small') && resolved === 'original') {
    return 'No resized variants exist, so the original image is shown (scaled down).'
  }

  if (selected === 'medium' && resolved === 'original') {
    return 'Medium selected, but no medium variant exists. Showing original instead.'
  }

  return null
}
