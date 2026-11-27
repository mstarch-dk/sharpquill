// @vitest-environment happy-dom
// TipTap needs a DOM to parse HTML into its document, so this file runs in happy-dom instead of plain Node.
import { Editor } from '@tiptap/vue-3'
import { afterEach, describe, expect, it } from 'vitest'
import { editorContentExtensions, htmlToMarkdown, markdownToHtml } from '../../app/utils/markdownConversion'

// The editor's save path: stored Markdown -> sanitised HTML -> TipTap document (drops what the schema does not
// know) -> editor HTML -> Markdown. These tests run the whole path, not just marked and Turndown.

const editors: Editor[] = []

afterEach(() => {
  editors.splice(0).forEach(editor => editor.destroy())
})

function loadIntoEditor(markdown: string): string {
  const editor = new Editor({ extensions: editorContentExtensions(), content: markdownToHtml(markdown) })
  editors.push(editor)
  return editor.getHTML()
}

/** What the editor stores after opening and saving the Markdown unchanged. */
function save(markdown: string): string {
  return htmlToMarkdown(loadIntoEditor(markdown))
}

/** Saving must not change the Markdown, and saving again must not either. */
function expectStable(markdown: string) {
  expect(save(markdown)).toBe(markdown)
}

/** Saving normalises the Markdown once; after that it is a fixed point. */
function expectNormalised(markdown: string, normalised: string) {
  expect(save(markdown)).toBe(normalised)
  expect(save(normalised)).toBe(normalised)
}

describe('markdownToHtml', () => {
  it('returns an empty document for empty input', () => {
    expect(markdownToHtml('')).toBe('')
  })

  it('renders GitHub-flavoured Markdown', () => {
    expect(markdownToHtml('# Title\n\nSome **bold** and ~~struck~~ text.'))
      .toBe('<h1>Title</h1>\n<p>Some <strong>bold</strong> and <del>struck</del> text.</p>\n')
  })
})

describe('round trip: stable constructs', () => {
  it.each([
    ['headings 1-4', '# One\n\n## Two\n\n### Three\n\n#### Four'],
    ['paragraphs', 'Para one\n\nPara two'],
    ['emphasis and strong', 'Some _em_, **strong** and **bold _nested em_ bold**.'],
    ['strikethrough', 'Some ~~struck~~ text.'],
    ['inline code', 'Use `a < b && c` here'],
    ['inline formatting in a heading', '## Heading with _em_ and `code`'],
    ['links with and without a title', '[link](https://example.com "Title") and [relative](/blog/post)'],
    ['mailto links', '[mail](mailto:someone@example.com)'],
    ['fenced code with a language', '```ts\nconst a = 1\nif (a < 2) console.log("<b>")\n```'],
    ['fenced code without a language', '```\nplain text\n```'],
    ['fenced code containing backticks and HTML', '```html\n<b>bold</b> & `tick`\n```'],
    ['blockquotes with several paragraphs', '> quote\n> \n> more'],
    ['hard line breaks', 'line one  \nline two'],
    ['characters that look like HTML', '1 < 2 > 0 & done'],
    ['escaped Markdown characters', 'Text with \\*not emphasis\\*'],
    ['plain images', '![alt text](/uploads/2026/11/a.png)'],
    ['plain images with a title', '![alt](/uploads/a.png "Image title")']
  ])('%s', (_name, markdown) => {
    expectStable(markdown)
  })

  it('ordered lists, nested lists and a start number', () => {
    expectStable('1.  one\n    \n2.  two\n    \n    1.  two-a\n        \n3.  three')
    expectStable('3.  three\n    \n4.  four')
  })

  it('nested bullet lists', () => {
    expectStable('*   a\n    \n*   b\n    \n    *   c\n        \n    *   d\n        \n*   e')
  })

  it.each([
    ['small', '<img src="/uploads/a.png" alt="A photo" data-size="small" data-src-original="/uploads/a.png" data-src-medium="/uploads/a-medium.png" data-src-thumb="/uploads/a-thumb.png" />'],
    ['medium', '<img src="/uploads/a-medium.png" alt="A photo" data-size="medium" data-src-original="/uploads/a.png" data-src-medium="/uploads/a-medium.png" />'],
    ['large without variants', '<img src="/uploads/a.png" alt="" data-size="large" />']
  ])('sized images keep their size and variant URLs (%s)', (_name, markdown) => {
    expectStable(markdown)
  })

  it('sized images keep quotes in the alt text', () => {
    expectStable('<img src="/uploads/a.png" alt="The &quot;best&quot; photo" data-size="small" />')
  })

  it('an image the editor reports as full size is written as plain Markdown', () => {
    const editor = new Editor({
      extensions: editorContentExtensions(),
      content: '<img src="/uploads/a.png" alt="x" data-size="full">'
    })
    editors.push(editor)
    expect(htmlToMarkdown(editor.getHTML())).toBe('![x](/uploads/a.png)')
  })

  it('a long article survives unchanged', () => {
    expectStable([
      '# Release notes',
      'Intro with **bold**, _em_, ~~old~~ and `code`.',
      '## Changes',
      // A list followed by another block ends with an indented blank line; that is the form the editor writes
      '1.  First\n    \n2.  Second\n    ',
      '> Note: [docs](https://example.com/docs)',
      '```bash\nnpm test\n```',
      '<img src="/uploads/s.png" alt="Screenshot" data-size="medium" data-src-original="/uploads/s.png" data-src-medium="/uploads/s-m.png" />',
      '![Diagram](/uploads/d.png)'
    ].join('\n\n'))
  })
})

// Constructs the editor cannot represent, or writes differently. Each test pins the current behaviour so a change
// is a deliberate decision, not an accident. They are lossy for content that did not come from the editor itself
// (content packages, Markdown written elsewhere); the editor never produces them.
describe('round trip: documented normalisations', () => {
  it('writes bullet lists with * and a blank line between items (same list on the website)', () => {
    expectNormalised('- a\n- b', '*   a\n    \n*   b')
    expectNormalised('* star\n* list', '*   star\n    \n*   list')
  })

  it('writes ordered lists with two spaces after the number', () => {
    expectNormalised('1. one\n2. two', '1.  one\n    \n2.  two')
  })

  it('writes setext headings as ATX headings', () => {
    expectNormalised('Title\n=====', '# Title')
  })

  it('writes * emphasis as _ emphasis', () => {
    expectNormalised('Some *em* here', 'Some _em_ here')
  })

  it('writes horizontal rules as * * *', () => {
    expectNormalised('---', '* * *')
    expectNormalised('___', '* * *')
  })

  it('writes indented code blocks as fenced code blocks', () => {
    expectNormalised('    indented code', '```\nindented code\n```')
  })

  it('writes <del> as ~~', () => {
    expectNormalised('<del>struck</del>', '~~struck~~')
  })

  it('escapes underscores inside words (renders the same)', () => {
    expectNormalised('snake_case', 'snake\\_case')
  })

  it('escapes parentheses in link URLs (renders the same)', () => {
    expectNormalised('[paren](https://example.com/a_(b))', '[paren](https://example.com/a_\\(b\\))')
  })

  it('decodes HTML entities in text', () => {
    expectNormalised('A&amp;B and &copy;', 'A&B and ©')
  })

  it('drops trailing spaces at the end of a paragraph', () => {
    expectNormalised('ends with two spaces  ', 'ends with two spaces')
  })

  it('moves an image out of the middle of a paragraph (images are block nodes in the editor)', () => {
    expectNormalised('Before ![i](/uploads/i.png) after', 'Before\n\n![i](/uploads/i.png)\n\nafter')
  })
})

describe('round trip: documented losses', () => {
  it('turns headings 5 and 6 into paragraphs (the editor offers levels 1-4)', () => {
    expectNormalised('##### Five\n\n###### Six', 'Five\n\nSix')
  })

  it('flattens tables into their text (the editor has no table support)', () => {
    expectNormalised('| a | b |\n|---|---|\n| 1 | 2 |', 'ab12')
  })

  it('drops task list checkboxes', () => {
    expectNormalised('- [ ] todo\n- [x] done', '*   todo\n    \n*   done')
  })

  it('drops underline and other inline HTML the editor does not know', () => {
    expectNormalised('<u>under</u> <span>x</span> <mark>y</mark>', 'under x y')
  })
})

describe('hostile Markdown', () => {
  it.each([
    ['a script tag', 'Hi <script>alert(1)</script> there'],
    ['an event handler on an image', '<img src="/uploads/a.png" onerror="alert(1)">'],
    ['a javascript: link', '[click](javascript:alert(1))'],
    ['an encoded javascript: link', '[click](jav&#x61;script:alert(1))'],
    ['a javascript: image', '![x](javascript:alert(1))'],
    ['an iframe', '<iframe src="https://evil.example"></iframe>'],
    ['a data: URL link', '[x](data:text/html,<script>alert(1)</script>)'],
    ['an inline style', '<p style="background:url(javascript:alert(1))">x</p>']
  ])('is sanitised on the HTML side: %s', (_name, markdown) => {
    const html = markdownToHtml(markdown).toLowerCase()
    expect(html).not.toContain('<script')
    expect(html).not.toContain('<iframe')
    expect(html).not.toContain('javascript:')
    expect(html).not.toContain('jav&#x61;script')
    expect(html).not.toContain('data:text/html')
    expect(html).not.toMatch(/\son\w+=/)
    expect(html).not.toContain('style=')
  })

  it('does not survive the round trip', () => {
    expect(save('Hi <script>alert(1)</script> there')).toBe('Hi there')
    expect(save('[click](javascript:alert(1))')).toBe('click')
    expect(save('<img src="/uploads/a.png" onerror="alert(1)">')).toBe('![](/uploads/a.png)')
  })

  it.each([
    ['alt', '<img src="/uploads/a.png" alt="x&quot; onerror=&quot;alert(1)" data-size="small" />', 'alt'],
    ['src', '<img src="/uploads/a.png&quot; onerror=&quot;alert(1)" alt="x" data-size="small" />', 'src'],
    ['variant URL', '<img src="/uploads/a.png" alt="x" data-size="small" data-src-thumb="/t.png&quot; onerror=&quot;alert(1)" />', 'data-src-thumb']
  ])('a sized image cannot break out of its %s attribute', (_name, markdown, attribute) => {
    const saved = save(markdown)
    const container = document.createElement('div')
    // Parse the saved Markdown without the sanitiser, so the check is on what the Turndown rule wrote
    container.innerHTML = saved
    const img = container.querySelector('img')!

    expect(img.getAttributeNames()).not.toContain('onerror')
    expect(img.getAttribute(attribute) ?? '').toContain('onerror="alert(1)')
  })
})
