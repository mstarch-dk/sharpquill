export type FeaturedImageSize = 'extra-small' | 'small' | 'medium' | 'original'

export function normalizeFeaturedImageSize(size?: FeaturedImageSize): FeaturedImageSize {
  if (size === 'extra-small' || size === 'small' || size === 'original') return size
  return 'medium'
}

export function getFeaturedImageContainerClass(size?: FeaturedImageSize): string {
  switch (normalizeFeaturedImageSize(size)) {
    case 'extra-small':
      // About 224px: a compact portrait or logo that should not dominate the page
      return 'max-w-[14rem] mx-auto'
    case 'small':
      return 'max-w-md mx-auto'
    case 'original':
      return 'max-w-none'
    default:
      return 'max-w-3xl mx-auto'
  }
}
