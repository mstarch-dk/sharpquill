import type { AnyExtension } from '@tiptap/vue-3'
import Image from '@tiptap/extension-image'
import StarterKit from '@tiptap/starter-kit'
import { marked } from 'marked'
import TurndownService from 'turndown'
import { sanitizeHtml } from '#layer/utils/sanitizeHtml'

/**
 * The Markdown <-> HTML conversion behind the article editor. Articles are stored as Markdown; the editor (TipTap)
 * works on HTML, so every save is Markdown -> HTML -> TipTap document -> HTML -> Markdown. A change here can silently
 * alter content on save, so test/unit/markdownConversion.test.ts runs that whole round trip.
 * No Vue and explicit imports only, so the unit tests can load it without the Nuxt runtime.
 */

/** Markdown to sanitised HTML for the editor; empty input gives an empty document. */
export function markdownToHtml(markdown: string): string {
  if (!markdown) return ''
  return sanitizeHtml(marked.parse(markdown) as string)
}

const turndown = new TurndownService({
  headingStyle: 'atx',
  codeBlockStyle: 'fenced'
})

// Preserve sized images as raw HTML so data-size survives the round-trip.
// Default-full images have no data-size attr and fall through to the standard img rule.
turndown.addRule('sizedImage', {
  filter: (node) => node.nodeName === 'IMG' && !!(node as HTMLElement).getAttribute('data-size'),
  replacement: (_content, node) => {
    const el = node as HTMLElement
    // Every value is written inside double quotes, so a quote in it would end the attribute early
    const attr = (name: string) => (el.getAttribute(name) || '').replace(/"/g, '&quot;')
    const src = attr('src')
    const alt = attr('alt')
    const size = attr('data-size')
    const srcOriginal = attr('data-src-original')
    const srcMedium = attr('data-src-medium')
    const srcThumb = attr('data-src-thumb')
    let attrs = `src="${src}" alt="${alt}" data-size="${size}"`
    if (srcOriginal) attrs += ` data-src-original="${srcOriginal}"`
    if (srcMedium) attrs += ` data-src-medium="${srcMedium}"`
    if (srcThumb) attrs += ` data-src-thumb="${srcThumb}"`
    return `<img ${attrs} />`
  }
})

// Turndown has no strikethrough rule (it lives in the GFM plugin), so ~~text~~ used to lose its marks on every save.
// TipTap's Strike mark renders <s>; marked renders <del>.
turndown.addRule('strikethrough', {
  filter: ['s', 'del'],
  replacement: (content) => content ? `~~${content}~~` : ''
})

/** The editor's HTML (TipTap getHTML()) back to the Markdown that is stored. */
export function htmlToMarkdown(html: string): string {
  return turndown.turndown(html)
}

/** Image extension extended with size + variant URL attributes */
export const SizedImage = Image.extend({
  addAttributes() {
    return {
      ...this.parent?.(),
      size: {
        default: 'full',
        parseHTML: (el) => el.getAttribute('data-size') || 'full',
        renderHTML: (attrs) => attrs.size && attrs.size !== 'full' ? { 'data-size': attrs.size } : {},
      },
      srcOriginal: {
        default: null,
        parseHTML: (el) => el.getAttribute('data-src-original'),
        renderHTML: (attrs) => attrs.srcOriginal ? { 'data-src-original': attrs.srcOriginal } : {},
      },
      srcMedium: {
        default: null,
        parseHTML: (el) => el.getAttribute('data-src-medium'),
        renderHTML: (attrs) => attrs.srcMedium ? { 'data-src-medium': attrs.srcMedium } : {},
      },
      srcThumb: {
        default: null,
        parseHTML: (el) => el.getAttribute('data-src-thumb'),
        renderHTML: (attrs) => attrs.srcThumb ? { 'data-src-thumb': attrs.srcThumb } : {},
      },
    }
  },
})

/**
 * The extensions that define what the editor's document can hold. Anything they do not know is dropped when HTML
 * is loaded, so they are part of the conversion. The editor adds UI-only extensions (Placeholder) on top.
 */
export function editorContentExtensions(): AnyExtension[] {
  return [
    // StarterKit 3 bundles Link and Underline. Underline stays off: Markdown has no underline syntax.
    StarterKit.configure({
      heading: {
        levels: [1, 2, 3, 4]
      },
      link: {
        openOnClick: false,
        HTMLAttributes: {
          class: 'text-primary-600 underline'
        }
      },
      underline: false
    }),
    SizedImage.configure({
      inline: false,
      allowBase64: false,
    })
  ]
}
