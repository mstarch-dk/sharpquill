<script setup lang="ts">
definePageMeta({
  middleware: 'auth'
})

// Redirect to articles page
navigateTo('/articles', { replace: true })
</script>

<template>
  <div class="flex items-center justify-center py-12">
    <div class="text-gray-500">Redirecting...</div>
  </div>
</template>
