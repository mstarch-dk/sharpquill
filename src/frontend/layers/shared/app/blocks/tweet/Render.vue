<script setup lang="ts">
import type { RenderProps } from '../types'

defineProps<RenderProps>()
</script>

<template>
  <TweetCard
    v-if="block.url"
    :url="block.url"
    :author-name="block.title"
    :author-handle="block.siteName"
    :tweet-text="block.description"
    :tweet-date="block.tweetDate"
    :profile-url="block.imageUrl"
  />
</template>
