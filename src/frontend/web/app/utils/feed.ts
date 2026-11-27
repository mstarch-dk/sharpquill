// RSS 2.0 and Atom 1.0 feeds for the public site (served by server/routes/**/rss.xml and atom.xml).
// Pure: the server handler (server/utils/feeds.ts) loads the data, this module only builds the XML, so it is
// unit-tested without a server. Pages use feedPath() for their <link rel="alternate"> autodiscovery entries.
//
// Feeds are summary feeds: every item carries the article's excerpt and a link, never the article body. That is
// also how members-only articles appear: as the same teaser the website's article lists show, with a link to the
// article, where the sign-in prompt takes over.

export type FeedFormat = 'rss' | 'atom'
export type FeedScope = 'site' | 'category' | 'tag' | 'series'

export interface FeedItem {
  title: string
  /** Absolute URL of the article page */
  url: string
  summary?: string
  /** ISO 8601 timestamp */
  published?: string
  author?: string
  categories: string[]
}

export interface Feed {
  title: string
  description: string
  /** Language code from the site settings, e.g. da or en-GB */
  language: string
  /** Absolute URL of the HTML page the feed belongs to (front page, category page, ...) */
  pageUrl: string
  /** Absolute URL of the feed itself */
  selfUrl: string
  /** Shown as the feed author in Atom (entries without an author inherit it) */
  siteName: string
  items: FeedItem[]
}

export const FEED_CONTENT_TYPES: Record<FeedFormat, string> = {
  rss: 'application/rss+xml; charset=utf-8',
  atom: 'application/atom+xml; charset=utf-8'
}

/** Site path of a feed: /rss.xml, /atom.xml, /category/{slug}/rss.xml, /tag/{slug}/atom.xml, ... */
export function feedPath(scope: FeedScope, format: FeedFormat, slug?: string): string {
  const file = `${format}.xml`
  if (scope === 'site') return `/${file}`
  return `/${scope}/${encodeURIComponent(slug ?? '')}/${file}`
}

// Characters XML 1.0 does not allow at all (control characters other than tab, LF and CR, lone surrogates,
// U+FFFE/U+FFFF). Escaping cannot save them, so they are dropped.
const INVALID_XML_CHARS = /[^\t\n\r -퟿-�\u{10000}-\u{10FFFF}]/gu

export function escapeXml(value: string): string {
  return value
    .replace(INVALID_XML_CHARS, '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;')
}

function parseDate(value?: string): Date | undefined {
  if (!value) return undefined
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? undefined : date
}

/** Newest item date, or `now` for a feed without dated items (Atom requires a feed-level <updated>) */
function feedUpdated(feed: Feed, now: Date): Date {
  const times = feed.items.map(item => parseDate(item.published)?.getTime()).filter((t): t is number => t !== undefined)
  return times.length ? new Date(Math.max(...times)) : now
}

const element = (name: string, value: string | undefined, indent: string) =>
  value ? `${indent}<${name}>${escapeXml(value)}</${name}>\n` : ''

export function buildRssFeed(feed: Feed, now = new Date()): string {
  const items = feed.items.map((item) => {
    const published = parseDate(item.published)
    return '    <item>\n'
      + element('title', item.title, '      ')
      + element('link', item.url, '      ')
      + `      <guid isPermaLink="true">${escapeXml(item.url)}</guid>\n`
      + element('description', item.summary?.trim(), '      ')
      + (published ? `      <pubDate>${published.toUTCString()}</pubDate>\n` : '')
      + element('dc:creator', item.author, '      ')
      + item.categories.map(category => element('category', category, '      ')).join('')
      + '    </item>\n'
  }).join('')

  return '<?xml version="1.0" encoding="UTF-8"?>\n'
    + '<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom" xmlns:dc="http://purl.org/dc/elements/1.1/">\n'
    + '  <channel>\n'
    + element('title', feed.title, '    ')
    + element('link', feed.pageUrl, '    ')
    // <description> is required in RSS 2.0
    + element('description', feed.description.trim() || feed.title, '    ')
    + element('language', feed.language, '    ')
    + `    <lastBuildDate>${feedUpdated(feed, now).toUTCString()}</lastBuildDate>\n`
    + `    <atom:link href="${escapeXml(feed.selfUrl)}" rel="self" type="application/rss+xml"/>\n`
    + items
    + '  </channel>\n'
    + '</rss>\n'
}

export function buildAtomFeed(feed: Feed, now = new Date()): string {
  const updated = feedUpdated(feed, now).toISOString()

  const entries = feed.items.map((item) => {
    const published = parseDate(item.published)?.toISOString()
    return '  <entry>\n'
      + element('title', item.title, '    ')
      + `    <link rel="alternate" type="text/html" href="${escapeXml(item.url)}"/>\n`
      + element('id', item.url, '    ')
      + element('published', published, '    ')
      // <updated> is required per entry; the API exposes no edit date, so it is the publish date
      + `    <updated>${published ?? updated}</updated>\n`
      + (item.author ? `    <author><name>${escapeXml(item.author)}</name></author>\n` : '')
      // Without an excerpt the title is the summary: feed validators warn about entries with no textual content
      + `    <summary type="text">${escapeXml(item.summary?.trim() || item.title)}</summary>\n`
      + item.categories.map(category => `    <category term="${escapeXml(category)}"/>\n`).join('')
      + '  </entry>\n'
  }).join('')

  return '<?xml version="1.0" encoding="UTF-8"?>\n'
    + `<feed xmlns="http://www.w3.org/2005/Atom" xml:lang="${escapeXml(feed.language)}">\n`
    + element('title', feed.title, '  ')
    + element('subtitle', feed.description.trim(), '  ')
    + `  <link rel="alternate" type="text/html" href="${escapeXml(feed.pageUrl)}"/>\n`
    + `  <link rel="self" type="application/atom+xml" href="${escapeXml(feed.selfUrl)}"/>\n`
    + element('id', feed.selfUrl, '  ')
    + `  <updated>${updated}</updated>\n`
    + `  <author><name>${escapeXml(feed.siteName)}</name></author>\n`
    + entries
    + '</feed>\n'
}

export function buildFeed(feed: Feed, format: FeedFormat, now = new Date()): string {
  return format === 'rss' ? buildRssFeed(feed, now) : buildAtomFeed(feed, now)
}
