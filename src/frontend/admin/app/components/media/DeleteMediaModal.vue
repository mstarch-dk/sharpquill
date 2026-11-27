<script setup lang="ts">
import type { MediaDto, MediaUsageDto } from '~/types/media'

interface Props {
  open: boolean
  media: MediaDto | null
  /** Null while the usages are loading */
  usages: MediaUsageDto[] | null
  loading?: boolean
  error?: string
}

const props = defineProps<Props>()

defineEmits<{
  close: []
  confirm: []
}>()

const inUse = computed(() => (props.usages?.length ?? 0) > 0)
</script>

<template>
  <UiModal :open="open" title="Delete Media" @close="$emit('close')">
    <div class="sm:flex sm:items-start">
      <div class="mx-auto flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-red-100 dark:bg-red-900/30 sm:mx-0 sm:h-10 sm:w-10">
        <UiIcon name="warning" class="h-6 w-6 text-red-600 dark:text-red-400" />
      </div>
      <div class="mt-3 min-w-0 flex-1 text-center sm:ml-4 sm:mt-0 sm:text-left">
        <p class="text-sm text-gray-500 dark:text-gray-400">
          Are you sure you want to delete "<strong class="break-all">{{ media?.originalFilename }}</strong>"?
          The original and its resized versions are removed from the server.
        </p>

        <p v-if="usages === null" class="mt-3 text-sm text-gray-500 dark:text-gray-400">
          Checking where it is used...
        </p>
        <div v-else-if="inUse" class="mt-3 text-left" data-testid="media-usages">
          <p class="text-sm font-medium text-amber-700 dark:text-amber-400">
            This file is still in use:
          </p>
          <ul class="mt-1 space-y-1 text-sm text-gray-700 dark:text-gray-300">
            <li v-for="(usage, i) in usages" :key="i">
              <span class="text-gray-500 dark:text-gray-400">{{ mediaUsageLabel(usage) }}:</span>
              <NuxtLink
                v-if="mediaUsageLink(usage)"
                :to="mediaUsageLink(usage)!"
                class="ml-1 text-primary-600 dark:text-primary-400 hover:underline"
              >
                {{ usage.title }}
              </NuxtLink>
              <span v-else class="ml-1">{{ usage.title }}</span>
            </li>
          </ul>
          <p class="mt-2 text-sm text-amber-700 dark:text-amber-400">
            Deleting it anyway removes it as featured image and leaves broken images where it is shown inline.
          </p>
        </div>

        <p v-if="error" class="mt-3 text-sm text-red-600 dark:text-red-400">
          {{ error }}
        </p>
      </div>
    </div>

    <template #footer>
      <UiButton variant="secondary" @click="$emit('close')">
        Cancel
      </UiButton>
      <UiButton variant="danger" :loading="loading" :disabled="usages === null" @click="$emit('confirm')">
        {{ inUse ? 'Delete anyway' : 'Delete' }}
      </UiButton>
    </template>
  </UiModal>
</template>
