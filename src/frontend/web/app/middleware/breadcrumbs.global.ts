export default defineNuxtRouteMiddleware(() => {
  useBreadcrumbs().set([])
})
