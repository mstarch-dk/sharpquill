<script setup lang="ts">
const props = defineProps<{
  currentPage: number
  totalPages: number
  baseUrl: string
}>()

const { t } = useI18n()

const pages = computed(() => {
  const result: (number | string)[] = []
  const total = props.totalPages
  const current = props.currentPage

  if (total <= 7) {
    for (let i = 1; i <= total; i++) {
      result.push(i)
    }
  } else {
    result.push(1)
    if (current > 3) {
      result.push('...')
    }
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
      result.push(i)
    }
    if (current < total - 2) {
      result.push('...')
    }
    result.push(total)
  }

  return result
})

function getPageUrl(page: number): string {
  if (page === 1) {
    return props.baseUrl
  }
  const separator = props.baseUrl.includes('?') ? '&' : '?'
  return `${props.baseUrl}${separator}page=${page}`
}
</script>

<template>
  <nav v-if="totalPages > 1" class="flex items-center justify-center gap-1" :aria-label="t('pagination.label')">
    <NuxtLink
      v-if="currentPage > 1"
      :to="getPageUrl(currentPage - 1)"
      class="px-3 py-2 rounded-md text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
    >
      <span class="sr-only">{{ t('pagination.previous') }}</span>
      <UiIcon name="chevron-left" class="w-5 h-5" />
    </NuxtLink>

    <template v-for="(page, index) in pages" :key="index">
      <span
        v-if="page === '...'"
        class="px-3 py-2 text-gray-400 dark:text-gray-500"
      >
        ...
      </span>
      <NuxtLink
        v-else
        :to="getPageUrl(page as number)"
        :class="[
          'px-3 py-2 rounded-md transition-colors',
          page === currentPage
            ? 'bg-primary-600 text-white'
            : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'
        ]"
      >
        {{ page }}
      </NuxtLink>
    </template>

    <NuxtLink
      v-if="currentPage < totalPages"
      :to="getPageUrl(currentPage + 1)"
      class="px-3 py-2 rounded-md text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
    >
      <span class="sr-only">{{ t('pagination.next') }}</span>
      <UiIcon name="chevron-right" class="w-5 h-5" />
    </NuxtLink>
  </nav>
</template>
