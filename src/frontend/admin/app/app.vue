<script setup lang="ts">
// Favicons and the manifest are set in nuxt.config.ts (app.head)
useHead({
  title: 'Sharpquill Admin',
  titleTemplate: '%s | Sharpquill Admin',
  meta: [
    { name: 'description', content: 'Sharpquill administration panel' }
  ]
})
</script>

<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
</template>
