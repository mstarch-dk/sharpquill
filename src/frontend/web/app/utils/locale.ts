// Picks the UI locale for a `language` site setting such as "da", "en" or "en-GB": an exact match first, then a
// locale with the same primary language ("en-GB" -> "en", "en" -> "en-GB"), else the fallback (Danish)
export function resolveLocaleCode<T extends string>(language: string, available: readonly T[], fallback: T): T {
  const tag = language.trim().toLowerCase()
  if (!tag) return fallback

  const exact = available.find(code => code.toLowerCase() === tag)
  if (exact) return exact

  const primary = tag.split('-')[0]
  return available.find(code => code.toLowerCase().split('-')[0] === primary) ?? fallback
}
