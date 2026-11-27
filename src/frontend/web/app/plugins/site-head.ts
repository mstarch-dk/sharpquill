// Site-wide head entries driven by the branding settings (site name, description, language).
// Universal on purpose: the client head manager must know the title template too, otherwise the first
// client-side head update after hydration drops the " | Site name" suffix the SSR HTML had.
// The settings are fetched server-side by theme.server.ts and reach the client through useState hydration;
// everything here is reactive, so plugin order does not matter.
export default defineNuxtPlugin(() => {
  const { settings, siteName, siteDescription, language } = useSiteSettings()
  const configuredName = computed(() => settings.value.siteName.trim())

  useHead({
    htmlAttrs: { lang: language },
    // Pages set their own title. Append the site name only when one is configured (and the page title is not it)
    title: siteName,
    titleTemplate: (title) => {
      const name = configuredName.value
      return name && title && title !== name ? `${title} | ${name}` : (title ?? null)
    },
    meta: [
      { name: 'description', content: siteDescription },
      { property: 'og:site_name', content: computed(() => configuredName.value || undefined) }
    ]
  })

  useFeedLinks('site', undefined, siteName)
})
