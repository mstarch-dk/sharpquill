export default defineNuxtRouteMiddleware(async () => {
  const { isAuthenticated, initAuth, certLogin, user, fetchProfile, clearTokens } = useAuth()

  // Restore the session (via the refresh cookie after a page load)
  await initAuth()

  if (!isAuthenticated.value) {
    // Certificate login only works client-side (browser presents cert to Caddy via mTLS)
    if (import.meta.client) {
      const result = await certLogin()
      if (result.success) {
        return
      }
    }

    return navigateTo('/login')
  }

  // Restore user profile after a token-only restore
  if (import.meta.client && !user.value) {
    const success = await fetchProfile()
    if (!success) {
      clearTokens()
      return navigateTo('/login')
    }
  }
})
