// Dev convenience: in production Caddy routes /uploads/* to the API before requests reach Nuxt.
// Without NUXT_UPLOADS_PROXY there is no proxy and /uploads/** answers 404 as before.
export default defineEventHandler((event) => {
  const { uploadsProxy } = useRuntimeConfig(event)
  if (!uploadsProxy) {
    throw createError({ statusCode: 404, statusMessage: 'Not Found' })
  }
  const path = getRouterParam(event, 'path') ?? ''
  return proxyRequest(event, `${uploadsProxy.replace(/\/+$/, '')}/uploads/${path}`)
})
