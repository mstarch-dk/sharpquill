const STORAGE_KEY = 'sidebar-collapsed'

const isCollapsed = ref(false)

export function useSidebarCollapse() {
  // Restore from localStorage on first call (client-side only)
  if (import.meta.client) {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored !== null) {
      isCollapsed.value = stored === 'true'
    }
  }

  function toggle() {
    isCollapsed.value = !isCollapsed.value
    if (import.meta.client) {
      localStorage.setItem(STORAGE_KEY, String(isCollapsed.value))
    }
  }

  function collapse() {
    isCollapsed.value = true
    if (import.meta.client) {
      localStorage.setItem(STORAGE_KEY, 'true')
    }
  }

  function expand() {
    isCollapsed.value = false
    if (import.meta.client) {
      localStorage.setItem(STORAGE_KEY, 'false')
    }
  }

  // Keyboard shortcut: Ctrl/Cmd+B
  onMounted(() => {
    function handleKeydown(e: KeyboardEvent) {
      if ((e.ctrlKey || e.metaKey) && e.key === 'b') {
        e.preventDefault()
        toggle()
      }
    }
    window.addEventListener('keydown', handleKeydown)
    onUnmounted(() => {
      window.removeEventListener('keydown', handleKeydown)
    })
  })

  return {
    isCollapsed: readonly(isCollapsed),
    toggle,
    collapse,
    expand
  }
}
