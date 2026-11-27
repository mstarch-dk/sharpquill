export interface ThemeColors {
  primary: {
    50: string; 100: string; 200: string; 300: string; 400: string
    500: string; 600: string; 700: string; 800: string; 900: string; 950: string
  }
  background: string
  surface: string
  text: string
  textSecondary: string
}

export interface ThemeFonts {
  heading: string
  body: string
}

export interface ThemeConfig {
  name: string
  slug: string
  description: string
  colors: ThemeColors
  darkColors: ThemeColors
  fonts: ThemeFonts
}

export function getThemeColors(theme: ThemeConfig, dark: boolean): ThemeColors {
  return dark ? theme.darkColors : theme.colors
}

const oceanBlue: ThemeConfig = {
  name: 'Ocean Blue',
  slug: 'ocean-blue',
  description: 'A clean, professional blue theme',
  colors: {
    primary: {
      50: '#f0f9ff', 100: '#e0f2fe', 200: '#bae6fd', 300: '#7dd3fc', 400: '#38bdf8',
      500: '#0ea5e9', 600: '#0284c7', 700: '#0369a1', 800: '#075985', 900: '#0c4a6e', 950: '#082f49'
    },
    background: '#f9fafb',
    surface: '#ffffff',
    text: '#111827',
    textSecondary: '#6b7280'
  },
  darkColors: {
    primary: {
      50: '#082f49', 100: '#0c4a6e', 200: '#075985', 300: '#0369a1', 400: '#0284c7',
      500: '#0ea5e9', 600: '#38bdf8', 700: '#7dd3fc', 800: '#bae6fd', 900: '#e0f2fe', 950: '#f0f9ff'
    },
    background: '#0f172a',
    surface: '#1e293b',
    text: '#f1f5f9',
    textSecondary: '#94a3b8'
  },
  fonts: {
    heading: "'Inter', system-ui, sans-serif",
    body: "'Inter', system-ui, sans-serif"
  }
}

const forestGreen: ThemeConfig = {
  name: 'Forest Green',
  slug: 'forest-green',
  description: 'Emerald greens with warm off-white background',
  colors: {
    primary: {
      50: '#ecfdf5', 100: '#d1fae5', 200: '#a7f3d0', 300: '#6ee7b7', 400: '#34d399',
      500: '#10b981', 600: '#059669', 700: '#047857', 800: '#065f46', 900: '#064e3b', 950: '#022c22'
    },
    background: '#faf9f6',
    surface: '#ffffff',
    text: '#1a1a1a',
    textSecondary: '#6b7280'
  },
  darkColors: {
    primary: {
      50: '#022c22', 100: '#064e3b', 200: '#065f46', 300: '#047857', 400: '#059669',
      500: '#10b981', 600: '#34d399', 700: '#6ee7b7', 800: '#a7f3d0', 900: '#d1fae5', 950: '#ecfdf5'
    },
    background: '#0c1a14',
    surface: '#162822',
    text: '#f0fdf4',
    textSecondary: '#86efac'
  },
  fonts: {
    heading: "'Inter', system-ui, sans-serif",
    body: "'Inter', system-ui, sans-serif"
  }
}

const sunsetAmber: ThemeConfig = {
  name: 'Sunset Amber',
  slug: 'sunset-amber',
  description: 'Warm amber and orange tones with cream background',
  colors: {
    primary: {
      50: '#fffbeb', 100: '#fef3c7', 200: '#fde68a', 300: '#fcd34d', 400: '#fbbf24',
      500: '#f59e0b', 600: '#d97706', 700: '#b45309', 800: '#92400e', 900: '#78350f', 950: '#451a03'
    },
    background: '#fdfcf9',
    surface: '#ffffff',
    text: '#1c1917',
    textSecondary: '#78716c'
  },
  darkColors: {
    primary: {
      50: '#451a03', 100: '#78350f', 200: '#92400e', 300: '#b45309', 400: '#d97706',
      500: '#f59e0b', 600: '#fbbf24', 700: '#fcd34d', 800: '#fde68a', 900: '#fef3c7', 950: '#fffbeb'
    },
    background: '#1a120a',
    surface: '#292017',
    text: '#fef3c7',
    textSecondary: '#d6d3d1'
  },
  fonts: {
    heading: "'Inter', system-ui, sans-serif",
    body: "'Inter', system-ui, sans-serif"
  }
}

const midnightPurple: ThemeConfig = {
  name: 'Midnight Purple',
  slug: 'midnight-purple',
  description: 'Deep violet tones with clean white surfaces',
  colors: {
    primary: {
      50: '#faf5ff', 100: '#f3e8ff', 200: '#e9d5ff', 300: '#d8b4fe', 400: '#c084fc',
      500: '#a855f7', 600: '#9333ea', 700: '#7e22ce', 800: '#6b21a8', 900: '#581c87', 950: '#3b0764'
    },
    background: '#fafafa',
    surface: '#ffffff',
    text: '#111827',
    textSecondary: '#6b7280'
  },
  darkColors: {
    primary: {
      50: '#3b0764', 100: '#581c87', 200: '#6b21a8', 300: '#7e22ce', 400: '#9333ea',
      500: '#a855f7', 600: '#c084fc', 700: '#d8b4fe', 800: '#e9d5ff', 900: '#f3e8ff', 950: '#faf5ff'
    },
    background: '#13071e',
    surface: '#1e1030',
    text: '#f3e8ff',
    textSecondary: '#a78bfa'
  },
  fonts: {
    heading: "'Inter', system-ui, sans-serif",
    body: "'Inter', system-ui, sans-serif"
  }
}

const roseGarden: ThemeConfig = {
  name: 'Rose Garden',
  slug: 'rose-garden',
  description: 'Dusty rose and pink with soft white accents',
  colors: {
    primary: {
      50: '#fff1f2', 100: '#ffe4e6', 200: '#fecdd3', 300: '#fda4af', 400: '#fb7185',
      500: '#f43f5e', 600: '#e11d48', 700: '#be123c', 800: '#9f1239', 900: '#881337', 950: '#4c0519'
    },
    background: '#fdfafb',
    surface: '#ffffff',
    text: '#1a1a1a',
    textSecondary: '#71717a'
  },
  darkColors: {
    primary: {
      50: '#4c0519', 100: '#881337', 200: '#9f1239', 300: '#be123c', 400: '#e11d48',
      500: '#f43f5e', 600: '#fb7185', 700: '#fda4af', 800: '#fecdd3', 900: '#ffe4e6', 950: '#fff1f2'
    },
    background: '#1a0a10',
    surface: '#2a1520',
    text: '#ffe4e6',
    textSecondary: '#fda4af'
  },
  fonts: {
    heading: "'Inter', system-ui, sans-serif",
    body: "'Inter', system-ui, sans-serif"
  }
}

const slateMinimal: ThemeConfig = {
  name: 'Slate Minimal',
  slug: 'slate-minimal',
  description: 'Neutral gray and slate with stark white',
  colors: {
    primary: {
      50: '#f8fafc', 100: '#f1f5f9', 200: '#e2e8f0', 300: '#cbd5e1', 400: '#94a3b8',
      500: '#64748b', 600: '#475569', 700: '#334155', 800: '#1e293b', 900: '#0f172a', 950: '#020617'
    },
    background: '#ffffff',
    surface: '#ffffff',
    text: '#0f172a',
    textSecondary: '#64748b'
  },
  darkColors: {
    primary: {
      50: '#020617', 100: '#0f172a', 200: '#1e293b', 300: '#334155', 400: '#475569',
      500: '#64748b', 600: '#94a3b8', 700: '#cbd5e1', 800: '#e2e8f0', 900: '#f1f5f9', 950: '#f8fafc'
    },
    background: '#0f172a',
    surface: '#1e293b',
    text: '#f1f5f9',
    textSecondary: '#94a3b8'
  },
  fonts: {
    heading: "'Inter', system-ui, sans-serif",
    body: "'Inter', system-ui, sans-serif"
  }
}

const warmEarth: ThemeConfig = {
  name: 'Warm Earth',
  slug: 'warm-earth',
  description: 'Terracotta and brown with cream background',
  colors: {
    primary: {
      50: '#fdf4f0', 100: '#fce8de', 200: '#f9ccb8', 300: '#f4a986', 400: '#ed7d4e',
      500: '#e65a24', 600: '#d4451a', 700: '#b03618', 800: '#8d2d1a', 900: '#732918', 950: '#3e120a'
    },
    background: '#faf8f5',
    surface: '#ffffff',
    text: '#292524',
    textSecondary: '#78716c'
  },
  darkColors: {
    primary: {
      50: '#3e120a', 100: '#732918', 200: '#8d2d1a', 300: '#b03618', 400: '#d4451a',
      500: '#e65a24', 600: '#ed7d4e', 700: '#f4a986', 800: '#f9ccb8', 900: '#fce8de', 950: '#fdf4f0'
    },
    background: '#1a1210',
    surface: '#292018',
    text: '#fce8de',
    textSecondary: '#d6d3d1'
  },
  fonts: {
    heading: "'Inter', system-ui, sans-serif",
    body: "'Inter', system-ui, sans-serif"
  }
}

const arcticFrost: ThemeConfig = {
  name: 'Arctic Frost',
  slug: 'arctic-frost',
  description: 'Teal and cyan with cool blue-white background',
  colors: {
    primary: {
      50: '#f0fdfa', 100: '#ccfbf1', 200: '#99f6e4', 300: '#5eead4', 400: '#2dd4bf',
      500: '#14b8a6', 600: '#0d9488', 700: '#0f766e', 800: '#115e59', 900: '#134e4a', 950: '#042f2e'
    },
    background: '#f8fafb',
    surface: '#ffffff',
    text: '#0f172a',
    textSecondary: '#64748b'
  },
  darkColors: {
    primary: {
      50: '#042f2e', 100: '#134e4a', 200: '#115e59', 300: '#0f766e', 400: '#0d9488',
      500: '#14b8a6', 600: '#2dd4bf', 700: '#5eead4', 800: '#99f6e4', 900: '#ccfbf1', 950: '#f0fdfa'
    },
    background: '#0a1a1a',
    surface: '#152828',
    text: '#ccfbf1',
    textSecondary: '#5eead4'
  },
  fonts: {
    heading: "'Inter', system-ui, sans-serif",
    body: "'Inter', system-ui, sans-serif"
  }
}

export const themes: Record<string, ThemeConfig> = {
  'ocean-blue': oceanBlue,
  'forest-green': forestGreen,
  'sunset-amber': sunsetAmber,
  'midnight-purple': midnightPurple,
  'rose-garden': roseGarden,
  'slate-minimal': slateMinimal,
  'warm-earth': warmEarth,
  'arctic-frost': arcticFrost,
}

export const themeList: ThemeConfig[] = Object.values(themes)

export const defaultThemeSlug = 'ocean-blue'

export function getThemeBySlug(slug: string): ThemeConfig {
  const fallbackTheme = themes[defaultThemeSlug]
  if (!fallbackTheme) {
    throw new Error(`Default theme "${defaultThemeSlug}" is not defined`)
  }
  // Own keys only: a slug such as "constructor" must not resolve to an Object.prototype member
  return (Object.hasOwn(themes, slug) ? themes[slug] : undefined) ?? fallbackTheme
}

export * from './fonts'
