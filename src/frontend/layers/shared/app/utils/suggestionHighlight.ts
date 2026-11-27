export interface HighlightPart {
  text: string
  match: boolean
}

const WORD = /[\p{L}\p{N}\p{M}]+/gu

/** The words of a query as the API compares them: NFC, lower case, runs of letters and digits. */
export function queryWords(query: string): string[] {
  return query.normalize('NFC').toLowerCase().match(WORD) ?? []
}

/**
 * Splits a suggestion title so that the start of each word matched by a query word is marked, mirroring the API's
 * word-prefix match (e.g. "post" marks "Post" in "PostgreSQL").
 */
export function highlightPrefixes(title: string, query: string): HighlightPart[] {
  // Match and slice the same (NFC) string: offsets found in the normalized title do not fit a title in another form
  const text = title.normalize('NFC')
  const words = queryWords(query)
  if (!words.length) return [{ text, match: false }]

  const parts: HighlightPart[] = []
  let position = 0
  for (const found of text.matchAll(WORD)) {
    const lower = found[0].toLowerCase()
    const prefix = Math.max(0, ...words.filter(w => lower.startsWith(w)).map(w => w.length))
    if (!prefix) continue
    const start = found.index
    const length = lengthBeforeLowerCase(found[0], prefix)
    if (start > position) parts.push({ text: text.slice(position, start), match: false })
    parts.push({ text: text.slice(start, start + length), match: true })
    position = start + length
  }
  if (position < text.length) parts.push({ text: text.slice(position), match: false })
  return parts
}

// Lower-casing can change the length ("İ" becomes "i" + combining dot): the number of characters of `word` whose
// lower-cased form is `lowerLength` long
function lengthBeforeLowerCase(word: string, lowerLength: number): number {
  let length = 0
  while (length < word.length && word.slice(0, length).toLowerCase().length < lowerLength) length++
  return length
}
