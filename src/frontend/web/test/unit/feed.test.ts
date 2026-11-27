import { describe, expect, it } from 'vitest'
import { buildAtomFeed, buildFeed, buildRssFeed, escapeXml, feedPath } from '../../app/utils/feed'
import type { Feed } from '../../app/utils/feed'

const NOW = new Date('2026-11-23T12:00:00Z')

const feed = (overrides: Partial<Feed> = {}): Feed => ({
  title: 'Acme Blog',
  description: 'Notes & news',
  language: 'en',
  pageUrl: 'https://example.com/',
  selfUrl: 'https://example.com/rss.xml',
  siteName: 'Acme Blog',
  items: [
    {
      title: 'Older <post>',
      url: 'https://example.com/blog/older',
      summary: '  An "older" one  ',
      published: '2026-11-01T08:00:00Z',
      author: 'Ann',
      categories: ['News', 'C#']
    },
    {
      title: 'Newer post',
      url: 'https://example.com/blog/newer',
      published: '2026-11-20T09:30:00Z',
      categories: []
    }
  ],
  ...overrides
})

describe('feedPath', () => {
  it('puts the site-wide feeds at the root', () => {
    expect(feedPath('site', 'rss')).toBe('/rss.xml')
    expect(feedPath('site', 'atom')).toBe('/atom.xml')
  })

  it('nests scoped feeds under their page and encodes the slug', () => {
    expect(feedPath('category', 'rss', 'tech')).toBe('/category/tech/rss.xml')
    expect(feedPath('tag', 'atom', 'c#')).toBe('/tag/c%23/atom.xml')
    expect(feedPath('series', 'rss', 'part-one')).toBe('/series/part-one/rss.xml')
  })
})

describe('escapeXml', () => {
  it('escapes markup characters', () => {
    expect(escapeXml(`<a href="x">Tom & Jerry's</a>`)).toBe('&lt;a href=&quot;x&quot;&gt;Tom &amp; Jerry&apos;s&lt;/a&gt;')
  })

  it('drops characters XML 1.0 forbids but keeps tabs, newlines and astral characters', () => {
    expect(escapeXml('a\u0000b\u0008c￾\td\ne 😀')).toBe('abc\td\ne 😀')
  })
})

describe('buildRssFeed', () => {
  const xml = buildRssFeed(feed(), NOW)

  it('writes an RSS 2.0 channel with a self link and the newest item date', () => {
    expect(xml).toMatch(/^<\?xml version="1.0" encoding="UTF-8"\?>\n<rss version="2.0"/)
    expect(xml).toContain('<title>Acme Blog</title>')
    expect(xml).toContain('<link>https://example.com/</link>')
    expect(xml).toContain('<description>Notes &amp; news</description>')
    expect(xml).toContain('<language>en</language>')
    expect(xml).toContain('<lastBuildDate>Fri, 20 Nov 2026 09:30:00 GMT</lastBuildDate>')
    expect(xml).toContain('<atom:link href="https://example.com/rss.xml" rel="self" type="application/rss+xml"/>')
  })

  it('writes items with an escaped title, permalink guid, trimmed summary, RFC 822 date, author and categories', () => {
    expect(xml).toContain('<title>Older &lt;post&gt;</title>')
    expect(xml).toContain('<guid isPermaLink="true">https://example.com/blog/older</guid>')
    expect(xml).toContain('<description>An &quot;older&quot; one</description>')
    expect(xml).toContain('<pubDate>Sun, 01 Nov 2026 08:00:00 GMT</pubDate>')
    expect(xml).toContain('<dc:creator>Ann</dc:creator>')
    expect(xml).toContain('<category>News</category>')
    expect(xml).toContain('<category>C#</category>')
  })

  it('leaves out optional item elements that have no value', () => {
    const newer = xml.slice(xml.indexOf('<title>Newer post</title>'))
    expect(newer).not.toContain('<description>')
    expect(newer).not.toContain('<dc:creator>')
    expect(newer).not.toContain('<category>')
  })

  it('falls back to the title as the required channel description and to now for an empty feed', () => {
    const empty = buildRssFeed(feed({ description: '  ', items: [] }), NOW)
    expect(empty).toContain('<description>Acme Blog</description>')
    expect(empty).toContain('<lastBuildDate>Mon, 23 Nov 2026 12:00:00 GMT</lastBuildDate>')
    expect(empty).not.toContain('<item>')
  })

  it('ignores invalid publish dates', () => {
    const xmlWithBadDate = buildRssFeed(feed({ items: [{ title: 'X', url: 'https://example.com/blog/x', published: 'not a date', categories: [] }] }), NOW)
    expect(xmlWithBadDate).not.toContain('<pubDate>')
    expect(xmlWithBadDate).toContain('<lastBuildDate>Mon, 23 Nov 2026 12:00:00 GMT</lastBuildDate>')
  })
})

describe('buildAtomFeed', () => {
  const xml = buildAtomFeed(feed({ selfUrl: 'https://example.com/atom.xml' }), NOW)

  it('writes an Atom feed with language, links, id, updated and a feed author', () => {
    expect(xml).toContain('<feed xmlns="http://www.w3.org/2005/Atom" xml:lang="en">')
    expect(xml).toContain('<subtitle>Notes &amp; news</subtitle>')
    expect(xml).toContain('<link rel="alternate" type="text/html" href="https://example.com/"/>')
    expect(xml).toContain('<link rel="self" type="application/atom+xml" href="https://example.com/atom.xml"/>')
    expect(xml).toContain('<id>https://example.com/atom.xml</id>')
    expect(xml).toContain('  <updated>2026-11-20T09:30:00.000Z</updated>\n  <author><name>Acme Blog</name></author>')
  })

  it('writes entries with id, published and updated dates, author, text summary and categories', () => {
    expect(xml).toContain('<id>https://example.com/blog/older</id>')
    expect(xml).toContain('<published>2026-11-01T08:00:00.000Z</published>\n    <updated>2026-11-01T08:00:00.000Z</updated>')
    expect(xml).toContain('<author><name>Ann</name></author>')
    expect(xml).toContain('<summary type="text">An &quot;older&quot; one</summary>')
    expect(xml).toContain('<category term="C#"/>')
  })

  it('gives an undated entry without excerpt the feed date as updated and its title as summary', () => {
    const undated = buildAtomFeed(feed({ items: [{ title: 'X', url: 'https://example.com/blog/x', categories: [] }] }), NOW)
    expect(undated).toContain('<updated>2026-11-23T12:00:00.000Z</updated>\n    <summary type="text">X</summary>\n  </entry>')
    expect(undated).not.toContain('<published>')
    expect(undated).toContain('<summary type="text">X</summary>')
  })

  it('omits an empty subtitle', () => {
    expect(buildAtomFeed(feed({ description: '' }), NOW)).not.toContain('<subtitle>')
  })
})

describe('buildFeed', () => {
  it('picks the builder for the format', () => {
    expect(buildFeed(feed(), 'rss', NOW)).toBe(buildRssFeed(feed(), NOW))
    expect(buildFeed(feed(), 'atom', NOW)).toBe(buildAtomFeed(feed(), NOW))
  })
})
