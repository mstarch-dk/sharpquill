import { formatDate as formatDateFor } from '~/utils/date'

// Date formatting bound to the active UI locale (the `language` site setting)
export function useLocaleDate() {
  const { locale, localeProperties } = useI18n()
  const languageTag = computed(() => localeProperties.value.language || locale.value)

  const formatDate = (dateString: string | undefined) => formatDateFor(dateString, languageTag.value)

  return { formatDate }
}
