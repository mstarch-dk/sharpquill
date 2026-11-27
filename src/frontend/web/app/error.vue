<script setup lang="ts">
const props = defineProps<{
  error: {
    statusCode: number
    message: string
  }
}>()

const { t } = useI18n()

const handleError = () => clearError({ redirect: '/' })

const errorTitle = computed(() => {
  switch (props.error.statusCode) {
    case 404:
      return t('error.notFoundTitle')
    case 500:
      return t('error.serverErrorTitle')
    default:
      return t('error.genericTitle')
  }
})

const errorMessage = computed(() => {
  switch (props.error.statusCode) {
    case 404:
      return t('error.notFoundMessage')
    case 500:
      return t('error.serverErrorMessage')
    default:
      return props.error.message || t('error.unexpectedMessage')
  }
})

useSeoMeta({
  title: errorTitle
})
</script>

<template>
  <div class="min-h-screen bg-gray-50 flex items-center justify-center px-4">
    <div class="text-center">
      <p class="text-6xl font-bold text-primary-600 mb-4">
        {{ error.statusCode }}
      </p>
      <h1 class="text-3xl font-bold text-gray-900 mb-4">
        {{ errorTitle }}
      </h1>
      <p class="text-gray-600 mb-8 max-w-md">
        {{ errorMessage }}
      </p>
      <div class="flex items-center justify-center gap-4">
        <button
          class="px-6 py-3 bg-primary-600 text-white font-semibold rounded-lg hover:bg-primary-700 transition-colors"
          @click="handleError"
        >
          {{ t('common.goHome') }}
        </button>
        <NuxtLink
          to="/blog"
          class="px-6 py-3 border border-gray-300 text-gray-700 font-semibold rounded-lg hover:bg-gray-100 transition-colors"
        >
          {{ t('error.viewBlog') }}
        </NuxtLink>
      </div>
    </div>
  </div>
</template>
