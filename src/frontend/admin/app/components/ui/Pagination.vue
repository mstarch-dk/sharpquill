<script setup lang="ts">
interface Props {
  currentPage: number
  totalPages: number
}

const props = defineProps<Props>()

defineEmits<{
  'update:currentPage': [page: number]
}>()

const visiblePages = computed(() => {
  const pages: (number | string)[] = []
  const current = props.currentPage
  const total = props.totalPages

  if (total <= 7) {
    for (let i = 1; i <= total; i++) {
      pages.push(i)
    }
  } else {
    pages.push(1)

    if (current > 3) {
      pages.push('...')
    }

    const start = Math.max(2, current - 1)
    const end = Math.min(total - 1, current + 1)

    for (let i = start; i <= end; i++) {
      pages.push(i)
    }

    if (current < total - 2) {
      pages.push('...')
    }

    pages.push(total)
  }

  return pages
})
</script>

<template>
  <nav v-if="totalPages > 1" class="flex items-center justify-center gap-1">
    <!-- Previous -->
    <button
      type="button"
      :disabled="currentPage <= 1"
      class="px-3 py-2 text-sm font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-100 dark:hover:bg-gray-700 dark:text-gray-300"
      @click="$emit('update:currentPage', currentPage - 1)"
    >
      Previous
    </button>

    <!-- Page numbers -->
    <template v-for="page in visiblePages" :key="page">
      <span v-if="page === '...'" class="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">...</span>
      <button
        v-else
        type="button"
        class="px-3 py-2 text-sm font-medium rounded-lg transition-colors"
        :class="page === currentPage
          ? 'bg-primary-600 text-white'
          : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'"
        @click="$emit('update:currentPage', page as number)"
      >
        {{ page }}
      </button>
    </template>

    <!-- Next -->
    <button
      type="button"
      :disabled="currentPage >= totalPages"
      class="px-3 py-2 text-sm font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-100 dark:hover:bg-gray-700 dark:text-gray-300"
      @click="$emit('update:currentPage', currentPage + 1)"
    >
      Next
    </button>
  </nav>
</template>
