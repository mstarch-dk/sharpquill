type AutoSaveStatus = 'idle' | 'saving' | 'saved' | 'error'

export function useAutoSave(saveFn: () => Promise<void>, debounceMs = 30000) {
  const status = ref<AutoSaveStatus>('idle')
  let timer: ReturnType<typeof setTimeout> | null = null
  let paused = false
  let savedTimer: ReturnType<typeof setTimeout> | null = null

  function clearTimer() {
    if (timer) {
      clearTimeout(timer)
      timer = null
    }
  }

  function clearSavedTimer() {
    if (savedTimer) {
      clearTimeout(savedTimer)
      savedTimer = null
    }
  }

  async function doSave() {
    if (paused) return
    status.value = 'saving'
    try {
      await saveFn()
      status.value = 'saved'
      clearSavedTimer()
      savedTimer = setTimeout(() => {
        if (status.value === 'saved') {
          status.value = 'idle'
        }
      }, 3000)
    } catch {
      status.value = 'error'
    }
  }

  function notifyChange() {
    if (paused) return
    clearTimer()
    timer = setTimeout(doSave, debounceMs)
  }

  function pause() {
    paused = true
    clearTimer()
  }

  function resume() {
    paused = false
  }

  async function flush() {
    clearTimer()
    await doSave()
  }

  onUnmounted(() => {
    clearTimer()
    clearSavedTimer()
  })

  return {
    status: readonly(status),
    notifyChange,
    pause,
    resume,
    flush
  }
}
