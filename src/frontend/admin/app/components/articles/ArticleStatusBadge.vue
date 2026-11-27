<script setup lang="ts">
import { ArticleStatus, getStatusLabel, getStatusColor, isScheduled, formatDateTime } from '~/types/article'

const props = defineProps<{
  status: ArticleStatus
  publishedAt?: string | null
}>()

const title = computed(() =>
  isScheduled(props.status, props.publishedAt) ? `Goes live ${formatDateTime(props.publishedAt!)}` : undefined
)
</script>

<template>
  <span
    class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
    :class="getStatusColor(status, publishedAt)"
    :title="title"
  >
    {{ getStatusLabel(status, publishedAt) }}
  </span>
</template>
