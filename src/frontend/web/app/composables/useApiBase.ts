export function useApiBase(): string {
  const config = useRuntimeConfig()
  if (import.meta.server && config.apiBaseInternal) {
    return config.apiBaseInternal as string
  }
  return config.public.apiBase
}
