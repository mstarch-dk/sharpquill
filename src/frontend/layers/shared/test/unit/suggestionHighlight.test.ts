// The web and admin apps have identical copies of suggestionHighlight.ts; keep this file identical to the other app's
// test/unit/suggestionHighlight.test.ts until the shared Nuxt layer [TASK-82] removes the duplicate.
import { describe, expect, it } from 'vitest'
import { highlightPrefixes, queryWords, type HighlightPart } from '../../app/utils/suggestionHighlight'

const marked = (parts: HighlightPart[]) => parts.filter(p => p.match).map(p => p.text)
const joined = (parts: HighlightPart[]) => parts.map(p => p.text).join('')

describe('queryWords', () => {
  it('lower-cases and splits on anything that is not a letter or digit', () => {
    expect(queryWords('Hello, World! 2026')).toEqual(['hello', 'world', '2026'])
  })

  it('returns no words for an empty or punctuation-only query', () => {
    expect(queryWords('')).toEqual([])
    expect(queryWords(' -- !? ')).toEqual([])
  })

  it('normalises to NFC', () => {
    expect(queryWords('Café')).toEqual(['café'])
  })

  it('keeps Danish letters inside words', () => {
    expect(queryWords('Rødgrød med fløde')).toEqual(['rødgrød', 'med', 'fløde'])
  })
})

describe('highlightPrefixes', () => {
  it('returns the whole title unmarked when the query has no words', () => {
    expect(highlightPrefixes('A title', ' ')).toEqual([{ text: 'A title', match: false }])
  })

  it('marks the matched prefix of a word, case-insensitively', () => {
    expect(highlightPrefixes('Tuning PostgreSQL', 'post')).toEqual([
      { text: 'Tuning ', match: false },
      { text: 'Post', match: true },
      { text: 'greSQL', match: false }
    ])
  })

  it('marks every word a query word matches, and only word starts', () => {
    const parts = highlightPrefixes('Postgres posts and reposts', 'post')
    expect(marked(parts)).toEqual(['Post', 'post'])
    expect(joined(parts)).toBe('Postgres posts and reposts')
  })

  it('uses the longest query word that matches a title word', () => {
    expect(marked(highlightPrefixes('Postgres', 'p postg'))).toEqual(['Postg'])
  })

  it('marks matches for several query words', () => {
    expect(marked(highlightPrefixes('Rødgrød med fløde', 'rød fl'))).toEqual(['Rød', 'fl'])
  })

  it('returns nothing marked when no word matches', () => {
    expect(highlightPrefixes('Hello world', 'xyz')).toEqual([{ text: 'Hello world', match: false }])
  })

  it('marks a precomposed accented title for a decomposed query', () => {
    expect(marked(highlightPrefixes('Café society', 'café'))).toEqual(['Café'])
  })

  // A title that is not in NFC form (decomposed accents, e.g. pasted from macOS) must be sliced at the same offsets it
  // was matched at, or the marked part shifts away from the matched word
  it('marks the right characters in a decomposed (non-NFC) title', () => {
    const title = 'Café society'
    const parts = highlightPrefixes(title, 'society')
    expect(marked(parts).map(t => t.normalize('NFC'))).toEqual(['society'])
    expect(joined(parts).normalize('NFC')).toBe(title.normalize('NFC'))
  })

  it('marks a whole decomposed word when the query covers it', () => {
    const parts = highlightPrefixes('Crème brûlée', 'creme crème brûlée')
    expect(marked(parts).map(t => t.normalize('NFC'))).toEqual(['Crème', 'brûlée'])
  })

  // "İ" lower-cases to two code units ("i" + combining dot), so a prefix length measured on the lower-cased word
  // must be mapped back to the title's own characters
  it('does not over-mark when lower-casing changes the length', () => {
    const parts = highlightPrefixes('İstanbul guide', 'İs')
    expect(marked(parts)).toEqual(['İs'])
    expect(joined(parts)).toBe('İstanbul guide')
  })
})
