<script setup lang="ts">
// `~` is the consuming app: the icon set is the app's own (see types/icons.ts), the renderer is shared
import { icons } from '~/utils/icons'

defineOptions({ inheritAttrs: false })

const props = defineProps<{
  name: string
  filled?: boolean
}>()

const icon = computed(() => icons[props.name])
</script>

<template>
  <svg
    v-if="icon"
    v-bind="$attrs"
    :viewBox="icon.viewBox || '0 0 24 24'"
    :fill="icon.style === 'fill' ? 'currentColor' : (filled ? 'currentColor' : 'none')"
    :stroke="icon.style === 'stroke' || !icon.style ? 'currentColor' : undefined"
    :stroke-width="icon.style === 'stroke' || !icon.style ? (icon.strokeWidth || 1.5) : undefined"
  >
    <template v-for="(path, i) in icon.paths" :key="i">
      <circle v-if="path.cx !== undefined" :cx="path.cx" :cy="path.cy" :r="path.r" />
      <path
        v-else
        :d="path.d"
        :fill-rule="path.fillRule"
        :clip-rule="path.clipRule"
        :transform="path.transform"
        stroke-linecap="round"
        stroke-linejoin="round"
      />
    </template>
  </svg>
</template>
