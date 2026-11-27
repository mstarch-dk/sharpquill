export function useUnsavedChanges(isDirty: Ref<boolean>) {
  // Browser beforeunload warning
  onMounted(() => {
    function handleBeforeUnload(e: BeforeUnloadEvent) {
      if (isDirty.value) {
        e.preventDefault()
      }
    }
    window.addEventListener('beforeunload', handleBeforeUnload)
    onUnmounted(() => {
      window.removeEventListener('beforeunload', handleBeforeUnload)
    })
  })

  // Vue Router navigation guard
  onBeforeRouteLeave(() => {
    if (isDirty.value) {
      return window.confirm('You have unsaved changes. Are you sure you want to leave?')
    }
  })
}
