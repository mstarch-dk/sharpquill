<script setup lang="ts">
import type { TagSummaryDto } from '~/types/api'

defineProps<{
  tag: TagSummaryDto
  size?: 'sm' | 'md'
}>()
</script>

<template>
  <NuxtLink
    :to="`/tag/${tag.slug}`"
    :class="[
      'inline-flex items-center rounded-full bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400 hover:bg-primary-100 hover:text-primary-700 transition-colors',
      size === 'sm' ? 'px-2 py-0.5 text-xs' : 'px-3 py-1 text-sm'
    ]"
  >
    {{ tag.name }}
  </NuxtLink>
</template>
