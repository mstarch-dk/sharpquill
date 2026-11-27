<script setup lang="ts">
const props = defineProps<{
  articleId: number
}>()

const { getLikeStatus, toggleLike } = useLikes()

const count = ref(0)
const hasLiked = ref(false)
const isLoading = ref(true)

onMounted(async () => {
  try {
    const response = await getLikeStatus(props.articleId)
    count.value = response.data.count
    hasLiked.value = response.data.hasLiked
  } catch (e) {
    console.error('Failed to load like status:', e)
  } finally {
    isLoading.value = false
  }
})

async function handleToggle() {
  // Optimistic update
  const wasLiked = hasLiked.value
  hasLiked.value = !wasLiked
  count.value += wasLiked ? -1 : 1

  try {
    const response = await toggleLike(props.articleId)
    count.value = response.data.count
    hasLiked.value = response.data.hasLiked
  } catch (e) {
    // Revert on error
    hasLiked.value = wasLiked
    count.value += wasLiked ? 1 : -1
    console.error('Failed to toggle like:', e)
  }
}
</script>

<template>
  <button
    class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-sm transition-colors"
    :class="hasLiked
      ? 'bg-red-50 text-red-600 hover:bg-red-100 dark:bg-red-900/20 dark:text-red-400 dark:hover:bg-red-900/30'
      : 'bg-gray-100 text-gray-600 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-400 dark:hover:bg-gray-600'"
    :disabled="isLoading"
    @click="handleToggle"
  >
    <UiIcon name="heart" class="w-5 h-5" :filled="hasLiked" />
    <span v-if="!isLoading">{{ count }}</span>
  </button>
</template>
