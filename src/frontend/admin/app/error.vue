<script setup lang="ts">
import type { NuxtError } from '#app'

defineProps<{
  error: NuxtError
}>()

const handleError = () => {
  clearError({ redirect: '/' })
}
</script>

<template>
  <div class="min-h-screen bg-gray-100 flex items-center justify-center px-4">
    <div class="max-w-md w-full bg-white rounded-lg shadow-lg p-8 text-center">
      <div class="text-6xl font-bold text-primary-600 mb-4">
        {{ error.statusCode }}
      </div>
      <h1 class="text-2xl font-semibold text-gray-900 mb-2">
        {{ error.statusCode === 404 ? 'Page Not Found' : 'An Error Occurred' }}
      </h1>
      <p class="text-gray-600 mb-6">
        {{ error.message || 'Something went wrong. Please try again.' }}
      </p>
      <button
        @click="handleError"
        class="inline-flex items-center px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
      >
        Go to Dashboard
      </button>
    </div>
  </div>
</template>
