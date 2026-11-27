import { defaultThemeSlug } from '../../../themes'
import { defaultFontId } from '../../../themes/fonts'

export function useDarkMode() {
  const themeSlug = useState<string>('siteThemeSlug', () => defaultThemeSlug)
  const globalFont = useState<string>('siteGlobalFont', () => defaultFontId)

  const isDark = ref(false)
  if (import.meta.client) {
    isDark.value = document.documentElement.classList.contains('dark')
  }

  function toggle() {
    if (!import.meta.client) return
    isDark.value = !isDark.value
    const { applyTheme, getThemeBySlug } = useTheme()
    const theme = getThemeBySlug(themeSlug.value)
    applyTheme(theme, isDark.value, globalFont.value)
    document.documentElement.classList.toggle('dark', isDark.value)
  }

  return { isDark, toggle }
}
