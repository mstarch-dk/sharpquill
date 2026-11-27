<script setup lang="ts">
interface Props {
  variant?: 'gray' | 'green' | 'yellow' | 'red' | 'blue'
  size?: 'sm' | 'md'
}

withDefaults(defineProps<Props>(), {
  variant: 'gray',
  size: 'md'
})

const variantClasses = {
  gray: 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200',
  green: 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300',
  yellow: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300',
  red: 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300',
  blue: 'bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300'
}

const sizeClasses = {
  sm: 'px-2 py-0.5 text-xs',
  md: 'px-2.5 py-1 text-sm'
}
</script>

<template>
  <span
    class="inline-flex items-center font-medium rounded-full"
    :class="[variantClasses[variant], sizeClasses[size]]"
  >
    <slot />
  </span>
</template>
