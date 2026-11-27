export interface FontOption {
  id: string
  name: string
  stack: string
  rationale: string
}

const fontCatalog: Record<string, FontOption> = {
  inter: {
    id: 'inter',
    name: 'Inter',
    stack: "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif",
    rationale: 'Balanced and highly readable for both UI controls and longer text.'
  },
  manrope: {
    id: 'manrope',
    name: 'Manrope',
    stack: "'Manrope', system-ui, -apple-system, 'Segoe UI', sans-serif",
    rationale: 'Modern geometric sans-serif with a crisp interface feel.'
  },
  'source-serif-4': {
    id: 'source-serif-4',
    name: 'Source Serif 4',
    stack: "'Source Serif 4', Georgia, Cambria, 'Times New Roman', serif",
    rationale: 'Editorial serif style that improves long-form reading tone.'
  },
  'atkinson-hyperlegible': {
    id: 'atkinson-hyperlegible',
    name: 'Atkinson Hyperlegible',
    stack: "'Atkinson Hyperlegible', system-ui, -apple-system, 'Segoe UI', sans-serif",
    rationale: 'Accessibility-focused typeface optimized for character clarity.'
  }
}

export const defaultFontId = 'inter'
const defaultFont = fontCatalog[defaultFontId]

export const fontOptions: FontOption[] = Object.values(fontCatalog)

export function getFontById(fontId?: string | null): FontOption {
  if (!defaultFont) {
    throw new Error(`Default font "${defaultFontId}" is not defined`)
  }
  // Own keys only: an id such as "constructor" must not resolve to an Object.prototype member
  return (fontId && Object.hasOwn(fontCatalog, fontId) ? fontCatalog[fontId] : undefined) ?? defaultFont
}

export function getFontStackById(fontId?: string | null): string {
  return getFontById(fontId).stack
}
