export default defineEventHandler((event) => {
  const { uploadsProxy } = useRuntimeConfig(event)
  const path = getRouterParam(event, 'path') ?? ''
  return proxyRequest(event, `${uploadsProxy}/uploads/${path}`)
})
