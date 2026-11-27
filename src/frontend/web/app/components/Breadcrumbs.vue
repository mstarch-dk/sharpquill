<script setup lang="ts">
const { breadcrumbs } = useBreadcrumbs()
const { t } = useI18n()
</script>

<template>
  <nav
    v-if="breadcrumbs.length"
    :aria-label="t('breadcrumbs.label')"
    class="bg-white dark:bg-gray-800 border-b border-gray-100 dark:border-gray-700"
  >
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
      <ol class="flex items-center gap-1 py-2 text-sm text-gray-500 dark:text-gray-400">
        <li>
          <NuxtLink to="/" class="hover:text-primary-600 transition-colors">{{ t('breadcrumbs.home') }}</NuxtLink>
        </li>
        <li v-for="(item, index) in breadcrumbs" :key="index" class="flex items-center gap-1">
          <UiIcon name="chevron-right" class="w-3.5 h-3.5 flex-shrink-0" />
          <NuxtLink
            v-if="item.href"
            :to="item.href"
            class="hover:text-primary-600 transition-colors"
          >
            {{ item.label }}
          </NuxtLink>
          <span v-else class="text-gray-900 dark:text-gray-100 font-medium">{{ item.label }}</span>
        </li>
      </ol>
    </div>
  </nav>
</template>
