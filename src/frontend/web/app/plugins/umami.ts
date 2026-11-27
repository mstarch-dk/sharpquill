export default defineNuxtPlugin(() => {
  const config = useRuntimeConfig()
  const umamiHost = config.public.umamiHost as string
  const umamiWebsiteId = config.public.umamiWebsiteId as string

  if (umamiHost && umamiWebsiteId) {
    useHead({
      script: [
        {
          src: `${umamiHost}/script.js`,
          async: true,
          defer: true,
          'data-website-id': umamiWebsiteId
        }
      ]
    })
  }
})
