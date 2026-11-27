<script setup lang="ts">
// Sharpquill logo (mark and wordmark). "auto" follows the admin's dark mode, "dark" is for dark surfaces.
const props = withDefaults(defineProps<{ surface?: 'auto' | 'light' | 'dark' }>(), { surface: 'auto' })

const showLight = computed(() => props.surface !== 'dark')
const showDark = computed(() => props.surface !== 'light')
</script>

<template>
  <span class="inline-flex items-center">
    <img
      v-if="showLight"
      src="/brand/logo.svg"
      alt="Sharpquill"
      class="h-full w-auto"
      :class="{ 'dark:hidden': surface === 'auto' }"
    >
    <img
      v-if="showDark"
      src="/brand/logo-dark.svg"
      alt="Sharpquill"
      class="h-full w-auto"
      :class="{ 'hidden dark:block': surface === 'auto' }"
    >
  </span>
</template>
