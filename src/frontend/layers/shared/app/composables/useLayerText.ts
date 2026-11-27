// UI text in shared components. The website has @nuxtjs/i18n and translates the keys (every key used in the layer
// must be in each web/i18n/locales file). The admin has no i18n yet: it gets the English fallbacks below, needed only
// for text the admin preview shows (embeds are not rendered there).
const englishFallback: Record<string, string> = {
  'common.readMore': 'Read more',
  'tweet.viewOnX': 'View on X'
}

interface I18nLike {
  t: (key: string) => string
}

export function useLayerText() {
  const i18n = (useNuxtApp() as unknown as { $i18n?: I18nLike }).$i18n
  return {
    t: (key: string): string => (i18n ? i18n.t(key) : (englishFallback[key] ?? key))
  }
}
