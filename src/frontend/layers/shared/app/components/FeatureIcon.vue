<script setup lang="ts">
import { featureIcons, isFeatureIcon } from '../utils/featureIcons'

// Card icon: a curated outline icon key renders as an SVG in a tinted square; anything else (legacy emoji) as text.
// The website theme swaps the primary scale in dark mode (50 is dark, 600 is light), so it needs no dark: variants;
// the admin palette is fixed and does (app.config.ts `sharedLayer.darkModeSwapsPrimary`).
const props = withDefaults(defineProps<{ icon: string, size?: 'sm' | 'md' }>(), { size: 'md' })

const swapsPrimary = useAppConfig().sharedLayer.darkModeSwapsPrimary

const def = computed(() => (isFeatureIcon(props.icon) ? featureIcons[props.icon] : undefined))
</script>

<template>
  <span
    v-if="def"
    :class="[
      'inline-flex items-center justify-center rounded-xl bg-primary-50 text-primary-600 ring-1 ring-inset ring-primary-100',
      swapsPrimary ? '' : 'dark:bg-primary-900/30 dark:text-primary-300 dark:ring-primary-800/60',
      size === 'sm' ? 'h-10 w-10' : 'h-12 w-12'
    ]"
    aria-hidden="true"
  >
    <svg
      :class="size === 'sm' ? 'h-5 w-5' : 'h-6 w-6'"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.5"
    >
      <path
        v-for="(path, i) in def.paths"
        :key="i"
        :d="path"
        stroke-linecap="round"
        stroke-linejoin="round"
      />
    </svg>
  </span>
  <span v-else :class="['block leading-none', size === 'sm' ? 'text-3xl' : 'text-4xl']" aria-hidden="true">{{ icon }}</span>
</template>
