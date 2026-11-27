// Danish is the reference locale: a key missing from another locale file falls back to its Danish text
export default defineI18nConfig(() => ({
  fallbackLocale: 'da'
}))
