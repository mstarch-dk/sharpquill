import { describe, expect, it } from 'vitest'
import { isSafeUrl, safeHref, sanitizeHtml } from '../../app/utils/sanitizeHtml'

describe('isSafeUrl', () => {
  it.each([
    '',
    '   ',
    'https://example.com/a?b=c#d',
    'http://example.com',
    'HTTPS://EXAMPLE.COM',
    'mailto:someone@example.com',
    'tel:+4512345678',
    '/uploads/2026/11/a.png',
    '#heading',
    '?page=2',
    'page-two',
    '../x',
    './a:b',
    'folder/name:with-colon',
    '//cdn.example.com/a.png'
  ])('accepts %j', url => {
    expect(isSafeUrl(url)).toBe(true)
  })

  it('treats null and undefined as safe (nothing to follow)', () => {
    expect(isSafeUrl(null)).toBe(true)
    expect(isSafeUrl(undefined)).toBe(true)
  })

  it.each([
    'javascript:alert(1)',
    'JavaScript:alert(1)',
    ' javascript:alert(1)',
    'data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==',
    'data:image/svg+xml,<svg onload=alert(1)>',
    'vbscript:msgbox(1)',
    'file:///etc/passwd',
    'ftp://example.com/x'
  ])('rejects scheme %j', url => {
    expect(isSafeUrl(url)).toBe(false)
  })

  it.each([
    'java\tscript:alert(1)',
    'java\nscript:alert(1)',
    'java\rscript:alert(1)',
    '\u0000javascript:alert(1)',
    'jav\u007fascript:alert(1)',
    'javascript\t:alert(1)'
  ])('rejects whitespace/control-obfuscated scheme %j', url => {
    expect(isSafeUrl(url)).toBe(false)
  })

  it.each([
    'javascript&colon;alert(1)',
    'javascript&#58;alert(1)',
    'javascript&#058;alert(1)',
    'javascript&#x3a;alert(1)',
    'javascript&#X3A;alert(1)',
    'javascript&#58alert(1)',
    '&#106;avascript:alert(1)',
    '&#x6A;&#x61;&#x76;&#x61;&#x73;&#x63;&#x72;&#x69;&#x70;&#x74;&#x3A;alert(1)',
    'java&tab;script:alert(1)',
    'java&Tab;script:alert(1)',
    'java&newline;script:alert(1)',
    'java&#9;script:alert(1)',
    'data&colon;text/html,x'
  ])('rejects entity-obfuscated scheme %j', url => {
    expect(isSafeUrl(url)).toBe(false)
  })

  it('rejects an undecodable entity that could stand for a colon before the first path delimiter', () => {
    expect(isSafeUrl('javascript&unknownentity;alert(1)')).toBe(false)
  })

  it('accepts an unknown entity after a path delimiter', () => {
    expect(isSafeUrl('/search?q=a&b;c')).toBe(true)
  })
})

describe('safeHref', () => {
  it('returns safe URLs unchanged', () => {
    expect(safeHref('https://example.com/x')).toBe('https://example.com/x')
    expect(safeHref('/articles/hello')).toBe('/articles/hello')
  })

  it.each(['javascript:alert(1)', 'java\tscript:alert(1)', 'javascript&colon;alert(1)', 'data:text/html,x'])(
    'replaces %j with #',
    url => {
      expect(safeHref(url)).toBe('#')
    }
  )

  it('returns # for empty values', () => {
    expect(safeHref('')).toBe('#')
    expect(safeHref(null)).toBe('#')
    expect(safeHref(undefined)).toBe('#')
  })
})

describe('sanitizeHtml', () => {
  it('returns an empty string for empty input', () => {
    expect(sanitizeHtml('')).toBe('')
    expect(sanitizeHtml(null)).toBe('')
    expect(sanitizeHtml(undefined)).toBe('')
  })

  it('keeps ordinary rendered markdown', () => {
    const html = '<h2>Title</h2><p>Some <strong>bold</strong>, <em>em</em> and <code>code</code>.</p>'
      + '<ul><li>one</li></ul><blockquote><p>q</p></blockquote>'
    expect(sanitizeHtml(html)).toBe(html)
  })

  describe('dangerous elements', () => {
    it.each([
      ['<script>alert(1)</script>', 'script'],
      ['<SCRIPT SRC="https://evil.example/x.js"></SCRIPT>', 'script with src'],
      ['<style>body{display:none}</style>', 'style'],
      ['<iframe src="https://evil.example"></iframe>', 'iframe'],
      ['<object data="x.swf"></object>', 'object'],
      ['<noscript><img src=x onerror=alert(1)></noscript>', 'noscript'],
      ['<template><img src=x onerror=alert(1)></template>', 'template']
    ])('drops %s (%s) including its content', input => {
      const out = sanitizeHtml(`<p>before</p>${input}<p>after</p>`)
      expect(out).toBe('<p>before</p><p>after</p>')
    })

    it('drops a void embed (xss leaves a harmless "[removed]" marker since there is no closing tag)', () => {
      const out = sanitizeHtml('<p>before</p><embed src="x.swf"><p>after</p>')
      expect(out).not.toMatch(/<embed|x\.swf/i)
      expect(out).toContain('<p>after</p>')
    })

    it('drops svg and its event handlers', () => {
      const out = sanitizeHtml('<svg onload="alert(1)"><circle r="1"/></svg><p>ok</p>')
      expect(out).not.toMatch(/<svg|onload|<circle/i)
      expect(out).toContain('<p>ok</p>')
    })

    it('drops an svg-embedded script', () => {
      const out = sanitizeHtml('<svg><script>alert(1)</script></svg>')
      expect(out).not.toMatch(/<svg|<script|alert/i)
    })

    it.each(['<form action="https://evil.example"><input name="p"></form>', '<link rel="stylesheet" href="x.css">',
      '<meta http-equiv="refresh" content="0;url=https://evil.example">', '<base href="https://evil.example/">'])(
      'drops non-allow-listed tag in %j',
      input => {
        expect(sanitizeHtml(input)).not.toMatch(/<(form|input|link|meta|base)\b/i)
      }
    )

    it('does not let a split tag reassemble into a script', () => {
      const out = sanitizeHtml('<scr<script>ipt>alert(1)</script>')
      expect(out).not.toMatch(/<script/i)
    })
  })

  describe('event handlers and style', () => {
    it.each([
      '<img src="/a.png" onerror="alert(1)">',
      '<img src="/a.png" ONERROR="alert(1)">',
      '<p onclick="alert(1)">x</p>',
      '<a href="/x" onmouseover="alert(1)">x</a>',
      '<img src=/a.png onerror=alert(1)>',
      '<details open ontoggle="alert(1)"><summary>s</summary></details>'
    ])('removes the handler in %j', input => {
      expect(sanitizeHtml(input)).not.toMatch(/\bon[a-z]+\s*=/i)
    })

    it('removes style attributes', () => {
      expect(sanitizeHtml('<p style="position:fixed;inset:0">x</p>')).toBe('<p>x</p>')
    })
  })

  describe('URLs', () => {
    it.each([
      '<a href="javascript:alert(1)">x</a>',
      '<a href="JaVaScRiPt:alert(1)">x</a>',
      '<a href="java&#x09;script:alert(1)">x</a>',
      '<a href="javascript&colon;alert(1)">x</a>',
      '<a href="&#106;avascript:alert(1)">x</a>',
      '<a href="data:text/html;base64,PHNjcmlwdD4=">x</a>',
      '<a href="vbscript:msgbox(1)">x</a>'
    ])('neutralises the href in %j', input => {
      const out = sanitizeHtml(input)
      expect(out).not.toMatch(/script:|data:|&colon;|&#/i)
      expect(out).toMatch(/^<a( href)?>x<\/a>$/)
    })

    it.each([
      '<img src="javascript:alert(1)">',
      '<img src="data:image/svg+xml,<svg onload=alert(1)>">',
      '<img src="java\tscript:alert(1)">'
    ])('neutralises the src in %j', input => {
      expect(sanitizeHtml(input)).not.toMatch(/script:|data:|onload/i)
    })

    it('keeps http(s), mailto, tel and site-relative links', () => {
      for (const href of ['https://example.com/a', 'mailto:a@example.com', 'tel:+4512345678', '/articles/x', '#top']) {
        expect(sanitizeHtml(`<a href="${href}">x</a>`)).toBe(`<a href="${href}">x</a>`)
      }
    })

    it('keeps plain relative links', () => {
      expect(sanitizeHtml('<a href="page-two">x</a>')).toBe('<a href="page-two">x</a>')
    })

    it('escapes a quote in a plain relative link so it cannot break out of the attribute', () => {
      const out = sanitizeHtml('<a href=\'page"onmouseover="alert(1)\'>x</a>')
      expect(out).not.toMatch(/"\s*onmouseover=/i)
    })
  })

  describe('links that open a new tab', () => {
    it('adds rel="noopener noreferrer" to target="_blank"', () => {
      expect(sanitizeHtml('<a href="https://example.com" target="_blank">x</a>'))
        .toBe('<a href="https://example.com" target="_blank" rel="noopener noreferrer">x</a>')
    })

    it('keeps an explicit rel', () => {
      expect(sanitizeHtml('<a href="https://example.com" target="_blank" rel="noopener">x</a>'))
        .toBe('<a href="https://example.com" target="_blank" rel="noopener">x</a>')
    })

    it('drops other target values', () => {
      expect(sanitizeHtml('<a href="/x" target="_top">x</a>')).toBe('<a href="/x">x</a>')
    })
  })

  describe('classes', () => {
    it('keeps syntax-highlighting classes', () => {
      expect(sanitizeHtml('<pre class="hljs"><code class="language-ts">x</code></pre>'))
        .toBe('<pre class="hljs"><code class="language-ts">x</code></pre>')
      expect(sanitizeHtml('<span class="hljs-keyword">const</span>')).toBe('<span class="hljs-keyword">const</span>')
    })

    it('drops arbitrary classes that could restyle the page', () => {
      expect(sanitizeHtml('<span class="fixed inset-0 z-50">x</span>')).toBe('<span>x</span>')
    })
  })

  describe('inline image sizes', () => {
    it.each(['small', 'medium', 'large', 'full'])('keeps data-size="%s"', size => {
      expect(sanitizeHtml(`<img src="/a.png" alt="a" data-size="${size}">`))
        .toBe(`<img src="/a.png" alt="a" data-size="${size}">`)
    })

    it.each(['huge', 'SMALL', 'small onerror=alert(1)', ''])('drops data-size=%j', size => {
      expect(sanitizeHtml(`<img src="/a.png" data-size="${size}">`)).toBe('<img src="/a.png">')
    })

    it('keeps width, height, loading and the variant URLs', () => {
      const html = '<img src="/u/a_medium.png" width="640" height="480" loading="lazy" '
        + 'data-src-original="/u/a.png" data-src-medium="/u/a_medium.png" data-src-thumb="/u/a_thumb.png">'
      expect(sanitizeHtml(html)).toBe(html)
    })

    it.each(['data-src-original', 'data-src-medium', 'data-src-thumb'])('drops an unsafe %s', attr => {
      expect(sanitizeHtml(`<img src="/a.png" ${attr}="javascript:alert(1)">`)).toBe('<img src="/a.png">')
      expect(sanitizeHtml(`<img src="/a.png" ${attr}="java&#9;script:alert(1)">`)).toBe('<img src="/a.png">')
    })

    it('drops data attributes that are not allow-listed', () => {
      expect(sanitizeHtml('<img src="/a.png" data-foo="bar">')).toBe('<img src="/a.png">')
    })
  })
})
