<script setup lang="ts">
import type { RenderProps } from '../types'

defineProps<RenderProps>()
</script>

<template>
  <LinkCard
    v-if="block.url"
    :url="block.url"
    :title="block.title"
    :description="block.description"
    :image-url="block.imageUrl"
    :site-name="block.siteName"
  />
</template>
