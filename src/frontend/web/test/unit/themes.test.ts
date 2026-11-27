// The themes live in src/frontend/themes and are shared by both apps; they are tested here only (CI runs the web job
// whenever src/frontend/themes changes).
import { describe, expect, it } from 'vitest'
import {
  defaultFontId, defaultThemeSlug, fontOptions, getFontById, getFontStackById, getThemeBySlug, getThemeColors,
  themeList, themes
} from '../../../themes'

const hex = /^#[0-9a-f]{6}$/
const shades = ['50', '100', '200', '300', '400', '500', '600', '700', '800', '900', '950']

describe('themes', () => {
  it('keys every theme by its own slug', () => {
    for (const [slug, theme] of Object.entries(themes)) expect(theme.slug).toBe(slug)
    expect(themeList).toHaveLength(Object.keys(themes).length)
  })

  it('defines every colour as a hex value in light and dark mode', () => {
    for (const theme of themeList) {
      for (const colors of [theme.colors, theme.darkColors]) {
        expect(Object.keys(colors.primary).sort()).toEqual([...shades].sort())
        for (const value of [...Object.values(colors.primary), colors.background, colors.surface, colors.text,
          colors.textSecondary]) {
          expect(value, `${theme.slug}`).toMatch(hex)
        }
      }
    }
  })

  it('has a default theme', () => {
    expect(themes[defaultThemeSlug]).toBeDefined()
  })
})

describe('getThemeBySlug', () => {
  it('returns the theme for a known slug', () => {
    expect(getThemeBySlug('forest-green').name).toBe('Forest Green')
  })

  it('falls back to the default theme for an unknown slug', () => {
    expect(getThemeBySlug('no-such-theme').slug).toBe(defaultThemeSlug)
    expect(getThemeBySlug('').slug).toBe(defaultThemeSlug)
  })

  it('does not resolve inherited object properties as themes', () => {
    expect(getThemeBySlug('constructor').slug).toBe(defaultThemeSlug)
    expect(getThemeBySlug('__proto__').slug).toBe(defaultThemeSlug)
  })
})

describe('getThemeColors', () => {
  it('picks the light or dark palette', () => {
    const theme = getThemeBySlug('ocean-blue')
    expect(getThemeColors(theme, false)).toBe(theme.colors)
    expect(getThemeColors(theme, true)).toBe(theme.darkColors)
  })
})

describe('fonts', () => {
  it('keys every font by its id and has the default font', () => {
    expect(fontOptions.map(f => f.id)).toContain(defaultFontId)
    for (const font of fontOptions) expect(getFontById(font.id)).toBe(font)
  })

  it('falls back to the default font for a missing or unknown id', () => {
    for (const id of [undefined, null, '', 'comic-sans', 'constructor', '__proto__']) {
      expect(getFontById(id).id).toBe(defaultFontId)
    }
  })

  it('returns the CSS font stack', () => {
    expect(getFontStackById('source-serif-4')).toBe("'Source Serif 4', Georgia, Cambria, 'Times New Roman', serif")
    expect(getFontStackById(undefined)).toBe(getFontById(defaultFontId).stack)
  })
})
