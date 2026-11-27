<script setup lang="ts">
import type { CommentDto } from '~/types/api'

const props = defineProps<{
  articleId: number
}>()

const { getComments, createComment } = useComments()
const { t } = useI18n()
const { formatDate } = useLocaleDate()

const comments = ref<CommentDto[]>([])
const isLoading = ref(true)
const error = ref('')

// Form state
const showForm = ref(false)
const replyingTo = ref<number | null>(null)
const formName = ref('')
const formEmail = ref('')
const formContent = ref('')
const formHoneypot = ref('')
const isSubmitting = ref(false)
const successMessage = ref('')
const formError = ref('')

onMounted(async () => {
  try {
    const { data } = await getComments(props.articleId)
    if (data.value?.data) {
      comments.value = data.value.data
    }
  } catch (e) {
    error.value = t('comments.loadError')
    console.error('Failed to load comments:', e)
  } finally {
    isLoading.value = false
  }
})

const totalCommentCount = computed(() => {
  let count = comments.value.length
  for (const comment of comments.value) {
    count += comment.replies?.length ?? 0
  }
  return count
})

function startReply(commentId: number) {
  replyingTo.value = commentId
  showForm.value = true
  successMessage.value = ''
  formError.value = ''
}

function showNewCommentForm() {
  replyingTo.value = null
  showForm.value = true
  successMessage.value = ''
  formError.value = ''
}

function cancelForm() {
  showForm.value = false
  replyingTo.value = null
  formError.value = ''
}

async function submitComment() {
  isSubmitting.value = true
  formError.value = ''
  successMessage.value = ''

  try {
    await createComment(props.articleId, {
      authorName: formName.value,
      authorEmail: formEmail.value,
      content: formContent.value,
      parentCommentId: replyingTo.value ?? undefined,
      honeypot: formHoneypot.value || undefined
    })

    successMessage.value = t('comments.pendingApproval')
    formContent.value = ''
    showForm.value = false
    replyingTo.value = null
  } catch (e) {
    formError.value = (e as { data?: { error?: { message?: string } } })?.data?.error?.message || t('comments.sendError')
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="mt-12 pt-8 border-t border-gray-200 dark:border-gray-700">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-6">
      {{ t('comments.title') }}
      <span v-if="!isLoading && totalCommentCount > 0" class="text-lg font-normal text-gray-500 dark:text-gray-400">
        ({{ totalCommentCount }})
      </span>
    </h2>

    <!-- Loading -->
    <div v-if="isLoading" class="text-gray-500 dark:text-gray-400">
      {{ t('comments.loading') }}
    </div>

    <!-- Error -->
    <div v-else-if="error" class="text-red-600 dark:text-red-400">
      {{ error }}
    </div>

    <template v-else>
      <!-- Comments list -->
      <div v-if="comments.length" class="space-y-6 mb-8">
        <div v-for="comment in comments" :key="comment.id">
          <!-- Top-level comment -->
          <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
            <div class="flex items-center justify-between mb-2">
              <span class="font-medium text-gray-900 dark:text-gray-100">{{ comment.authorName }}</span>
              <time class="text-sm text-gray-500 dark:text-gray-400" :datetime="comment.createdAt">
                {{ formatDate(comment.createdAt) }}
              </time>
            </div>
            <p class="text-gray-700 dark:text-gray-300 whitespace-pre-line">{{ comment.content }}</p>
            <button
              class="mt-2 text-sm text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
              @click="startReply(comment.id)"
            >
              {{ t('comments.reply') }}
            </button>
          </div>

          <!-- Replies -->
          <div v-if="comment.replies?.length" class="ml-8 mt-3 space-y-3">
            <div
              v-for="reply in comment.replies"
              :key="reply.id"
              class="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 border-l-2 border-primary-200 dark:border-primary-700"
            >
              <div class="flex items-center justify-between mb-2">
                <span class="font-medium text-gray-900 dark:text-gray-100">{{ reply.authorName }}</span>
                <time class="text-sm text-gray-500 dark:text-gray-400" :datetime="reply.createdAt">
                  {{ formatDate(reply.createdAt) }}
                </time>
              </div>
              <p class="text-gray-700 dark:text-gray-300 whitespace-pre-line">{{ reply.content }}</p>
            </div>
          </div>

          <!-- Reply form inline -->
          <div v-if="replyingTo === comment.id && showForm" class="ml-8 mt-3">
            <form class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4" @submit.prevent="submitComment">
              <h4 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
                {{ t('comments.replyTo', { name: comment.authorName }) }}
              </h4>
              <div class="space-y-3">
                <div class="grid grid-cols-2 gap-3">
                  <input
                    v-model="formName"
                    type="text"
                    :placeholder="t('comments.namePlaceholder')"
                    required
                    maxlength="100"
                    class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-primary-500 focus:border-primary-500"
                  />
                  <input
                    v-model="formEmail"
                    type="email"
                    :placeholder="t('comments.emailPlaceholder')"
                    required
                    maxlength="255"
                    class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-primary-500 focus:border-primary-500"
                  />
                </div>
                <textarea
                  v-model="formContent"
                  :placeholder="t('comments.replyPlaceholder')"
                  required
                  maxlength="5000"
                  rows="3"
                  class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-primary-500 focus:border-primary-500"
                />
                <input v-model="formHoneypot" type="text" class="hidden" tabindex="-1" autocomplete="off" />
                <div v-if="formError" class="text-sm text-red-600 dark:text-red-400">{{ formError }}</div>
                <div class="flex gap-2">
                  <button
                    type="submit"
                    :disabled="isSubmitting"
                    class="px-4 py-2 bg-primary-600 text-white text-sm rounded-md hover:bg-primary-700 disabled:opacity-50"
                  >
                    {{ isSubmitting ? t('common.sending') : t('comments.sendReply') }}
                  </button>
                  <button
                    type="button"
                    class="px-4 py-2 text-gray-600 dark:text-gray-400 text-sm hover:text-gray-800 dark:hover:text-gray-200"
                    @click="cancelForm"
                  >
                    {{ t('common.cancel') }}
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      </div>

      <div v-else class="text-gray-500 dark:text-gray-400 mb-8">
        {{ t('comments.empty') }}
      </div>

      <!-- Success message -->
      <div v-if="successMessage" class="mb-4 p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-300 rounded-lg text-sm">
        {{ successMessage }}
      </div>

      <!-- New comment form (top-level) -->
      <div v-if="!showForm || replyingTo !== null">
        <button
          class="px-4 py-2 bg-primary-600 text-white text-sm rounded-md hover:bg-primary-700"
          @click="showNewCommentForm"
        >
          {{ t('comments.add') }}
        </button>
      </div>

      <form v-if="showForm && replyingTo === null" class="mt-4 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4" @submit.prevent="submitComment">
        <div class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <input
              v-model="formName"
              type="text"
              :placeholder="t('comments.namePlaceholder')"
              required
              maxlength="100"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-primary-500 focus:border-primary-500"
            />
            <input
              v-model="formEmail"
              type="email"
              :placeholder="t('comments.emailPlaceholder')"
              required
              maxlength="255"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-primary-500 focus:border-primary-500"
            />
          </div>
          <textarea
            v-model="formContent"
            :placeholder="t('comments.commentPlaceholder')"
            required
            maxlength="5000"
            rows="4"
            class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-primary-500 focus:border-primary-500"
          />
          <input v-model="formHoneypot" type="text" class="hidden" tabindex="-1" autocomplete="off" />
          <div v-if="formError" class="text-sm text-red-600 dark:text-red-400">{{ formError }}</div>
          <div class="flex gap-2">
            <button
              type="submit"
              :disabled="isSubmitting"
              class="px-4 py-2 bg-primary-600 text-white text-sm rounded-md hover:bg-primary-700 disabled:opacity-50"
            >
              {{ isSubmitting ? t('common.sending') : t('comments.sendComment') }}
            </button>
            <button
              type="button"
              class="px-4 py-2 text-gray-600 dark:text-gray-400 text-sm hover:text-gray-800 dark:hover:text-gray-200"
              @click="cancelForm"
            >
              {{ t('common.cancel') }}
            </button>
          </div>
        </div>
      </form>
    </template>
  </div>
</template>
