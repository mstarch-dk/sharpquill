export default defineNuxtRouteMiddleware(async () => {
  const { isAuthenticated, initAuth } = useAuth()

  // Restore the session (via the refresh cookie after a page load)
  await initAuth()

  if (isAuthenticated.value) {
    return navigateTo('/')
  }
})
