import * as xssModule from 'xss'

// xss is a CommonJS module: depending on the loader (Vite client bundle, Node ESM on the SSR server) its API is on
// the namespace or on its default export, and Node cannot always detect named exports. Resolve both.
const xssApi = ((xssModule as unknown as { default?: typeof xssModule }).default ?? xssModule) as typeof xssModule
const { FilterXSS, safeAttrValue: defaultSafeAttrValue } = xssApi

/**
 * Sanitising for every piece of HTML we hand to v-html: markdown rendered in the browser, and HTML composed
 * by the API. It is an allow-list: unknown tags are dropped (script/style/iframe/object including their
 * content), event-handler attributes never survive, and URLs may only be http(s), mailto, tel or relative.
 * Pure JS (no DOM), so it behaves identically during SSR and in the browser.
 */

// Entities that matter for hiding a scheme; browsers decode entities in attribute values before parsing the URL
const namedEntities: Record<string, string> = {
  amp: '&', lt: '<', gt: '>', quot: '"', apos: "'", colon: ':', tab: '\t', newline: '\n',
  nbsp: '\u00a0', lpar: '(', rpar: ')', sol: '/', num: '#', quest: '?', period: '.'
}

function decodeEntities(value: string): string {
  return value.replace(/&(?:#(\d+)|#x([0-9a-f]+)|([a-z][a-z0-9]*));?/gi, (match, dec, hex, name) => {
    if (dec || hex) {
      const code = dec ? parseInt(dec, 10) : parseInt(hex, 16)
      return code > 0 && code <= 0x10ffff ? String.fromCodePoint(code) : ''
    }
    return namedEntities[String(name).toLowerCase()] ?? match
  })
}

/** True for relative URLs and http, https, mailto and tel URLs. Mirrors the API's UrlSafety.IsSafe. */
export function isSafeUrl(url: string | null | undefined): boolean {
  if (!url || !url.trim()) return true
  // Browsers ignore whitespace/control characters inside the scheme ("java\tscript:")
  // eslint-disable-next-line no-control-regex
  const compact = decodeEntities(url).replace(/[\u0000-\u0020\u007f]/g, '')
  const schemeEnd = compact.indexOf(':')
  if (schemeEnd < 0) {
    // An entity we cannot decode could still stand for ':' - refuse it before the first path delimiter
    return !/^[^/?#]*&[a-z][a-z0-9]*;/i.test(compact)
  }
  const delimiter = compact.search(/[/?#]/)
  if (delimiter >= 0 && delimiter < schemeEnd) return true
  return ['http', 'https', 'mailto', 'tel'].includes(compact.slice(0, schemeEnd).toLowerCase())
}

/** Returns the URL if safe, otherwise '#', for use in :href / :to bindings. */
export function safeHref(url: string | null | undefined): string {
  return url && isSafeUrl(url) ? url : '#'
}

const imageVariantAttrs = ['data-src-original', 'data-src-medium', 'data-src-thumb']

const allowList: Record<string, string[]> = {
  a: ['href', 'title', 'target', 'rel'],
  abbr: ['title'],
  b: [], blockquote: [], br: [],
  code: ['class'],
  del: [], details: ['open'], dd: [], dl: [], dt: [],
  em: [],
  figcaption: [], figure: [],
  h1: [], h2: [], h3: [], h4: [], h5: [], h6: [], hr: [],
  i: [],
  img: ['src', 'alt', 'title', 'width', 'height', 'loading', 'data-size', ...imageVariantAttrs],
  ins: [], kbd: [],
  li: [], mark: [],
  ol: ['start'],
  p: [],
  pre: ['class'],
  s: [], small: [], span: ['class'], strong: [], sub: [], summary: [], sup: [],
  table: [], tbody: [], td: ['colspan', 'rowspan', 'align'], tfoot: [],
  th: ['colspan', 'rowspan', 'align', 'scope'], thead: [], tr: [],
  u: [], ul: []
}

// Only syntax-highlighting style classes; arbitrary classes could be used to restyle/overlay the page
const allowedClass = /^(language-[\w+#.-]+|hljs[\w -]*)$/

const imageSizes = new Set(['small', 'medium', 'large', 'full'])

const filter = new FilterXSS({
  whiteList: allowList,
  stripIgnoreTag: true,
  stripIgnoreTagBody: ['script', 'style', 'iframe', 'object', 'embed', 'noscript', 'template'],
  onTagAttr(_tag, name, value) {
    if (name === 'class') return allowedClass.test(value) ? undefined : ''
    if (name === 'target') return value === '_blank' ? undefined : ''
    // Size picked in the editor for inline images; the prose CSS maps it to a column-relative width
    if (name === 'data-size') return imageSizes.has(value) ? undefined : ''
    // Variant URLs the admin editor needs to swap src when the size changes after reopening an article
    if (imageVariantAttrs.includes(name)) return isSafeUrl(value) ? undefined : ''
    return undefined
  },
  safeAttrValue(tag, name, value, cssFilter) {
    if (name === 'href' || name === 'src') {
      // Allow plain relative URLs ("page-two", "../x") in addition to what xss accepts by default
      if (!isSafeUrl(value)) return ''
      if (!/^[a-z][a-z0-9+.-]*:/i.test(value.trim()) && !/^(\/|#|\.)/.test(value.trim())) {
        return value.trim().replace(/"/g, '&quot;')
      }
    }
    return defaultSafeAttrValue(tag, name, value, cssFilter)
  },
  onTag(tag, html, options) {
    // Links that open a new tab must not hand window.opener to the target
    if (tag === 'a' && !options.isClosing && /\btarget="_blank"/.test(html) && !/\brel=/.test(html)) {
      return html.replace(/>$/, ' rel="noopener noreferrer">')
    }
    return undefined
  }
})

export function sanitizeHtml(html: string | null | undefined): string {
  return html ? filter.process(html) : ''
}
