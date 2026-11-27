// Dates are formatted for the active UI locale; pass its language tag (useLocaleDate() does that for components)
export function formatDate(dateString: string | undefined, locale: string): string {
  if (!dateString) return ''

  const date = new Date(dateString)
  return date.toLocaleDateString(locale, {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

export function formatDateShort(dateString: string | undefined, locale: string): string {
  if (!dateString) return ''

  const date = new Date(dateString)
  return date.toLocaleDateString(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

export function formatDateISO(dateString: string | undefined): string {
  if (!dateString) return ''

  const date = new Date(dateString)
  return date.toISOString().split('T')[0] || ''
}
