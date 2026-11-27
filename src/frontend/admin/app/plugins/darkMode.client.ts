import { getFontStackById } from '../../../themes/fonts'

export default defineNuxtPlugin(async () => {
  const config = useRuntimeConfig()
  const root = document.documentElement
  const defaultFontStack = getFontStackById()
  root.style.setProperty('--font-heading', defaultFontStack)
  root.style.setProperty('--font-body', defaultFontStack)

  try {
    const response = await $fetch<{ data: { darkModeAdmin?: boolean; globalFont?: string } }>(
      `${config.public.apiBase}/api/v1/settings`
    )
    const selectedFontStack = getFontStackById(response.data.globalFont)
    root.style.setProperty('--font-heading', selectedFontStack)
    root.style.setProperty('--font-body', selectedFontStack)

    if (response.data.darkModeAdmin) {
      root.classList.add('dark')
    } else {
      root.classList.remove('dark')
    }
  } catch {
    // ignore - default to light mode
  }
})
