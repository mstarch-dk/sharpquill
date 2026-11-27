<script setup lang="ts">
import type { SeriesDto } from '~/types/series'

interface Props {
  open: boolean
  series: SeriesDto | null
  loading?: boolean
}

defineProps<Props>()

defineEmits<{
  close: []
  confirm: []
}>()
</script>

<template>
  <UiModal :open="open" title="Delete Series" @close="$emit('close')">
    <div class="sm:flex sm:items-start">
      <div class="mx-auto flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-red-100 dark:bg-red-900/30 sm:mx-0 sm:h-10 sm:w-10">
        <UiIcon name="warning" class="h-6 w-6 text-red-600 dark:text-red-400" />
      </div>
      <div class="mt-3 text-center sm:ml-4 sm:mt-0 sm:text-left">
        <p class="text-sm text-gray-500 dark:text-gray-400">
          Are you sure you want to delete "<strong>{{ series?.name }}</strong>"? Articles in this series will be unlinked but not deleted.
        </p>
      </div>
    </div>

    <template #footer>
      <UiButton variant="secondary" @click="$emit('close')">
        Cancel
      </UiButton>
      <UiButton variant="danger" :loading="loading" @click="$emit('confirm')">
        Delete
      </UiButton>
    </template>
  </UiModal>
</template>
