<script setup lang="ts">
import type { CommentAdminDto, CommentCountsDto } from '~/types/comment'
import { CommentStatus, getCommentStatusLabel, getCommentStatusColor } from '~/types/comment'
import type { PaginationMeta } from '~/types/api'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Comments'
})

const { getComments, getCounts, moderateComment, deleteComment } = useAdminComments()

const comments = ref<CommentAdminDto[]>([])
const counts = ref<CommentCountsDto>({ pending: 0, approved: 0, rejected: 0 })
const pagination = ref<PaginationMeta | null>(null)
const isLoading = ref(true)
const error = ref('')
const currentPage = ref(1)
const activeFilter = ref<CommentStatus | null>(null)

const deleteModalOpen = ref(false)
const commentToDelete = ref<CommentAdminDto | null>(null)
const isDeleting = ref(false)

interface FilterTab {
  label: string
  value: CommentStatus | null
  count?: number
}

const filterTabs = computed<FilterTab[]>(() => [
  { label: 'All', value: null },
  { label: 'Pending', value: CommentStatus.Pending, count: counts.value.pending },
  { label: 'Approved', value: CommentStatus.Approved, count: counts.value.approved },
  { label: 'Rejected', value: CommentStatus.Rejected, count: counts.value.rejected },
])

async function fetchComments() {
  isLoading.value = true
  error.value = ''

  try {
    const [commentsResponse, countsResponse] = await Promise.all([
      getComments({ page: currentPage.value, pageSize: 20, status: activeFilter.value }),
      getCounts()
    ])
    comments.value = commentsResponse.data
    pagination.value = commentsResponse.meta ?? null
    counts.value = countsResponse.data
  } catch (e) {
    error.value = 'Failed to load comments'
    console.error('Failed to load comments:', e)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchComments()
})

function setFilter(status: CommentStatus | null) {
  activeFilter.value = status
  currentPage.value = 1
  fetchComments()
}

async function handleApprove(comment: CommentAdminDto) {
  try {
    await moderateComment(comment.id, CommentStatus.Approved)
    await fetchComments()
  } catch (e) {
    console.error('Failed to approve comment:', e)
  }
}

async function handleReject(comment: CommentAdminDto) {
  try {
    await moderateComment(comment.id, CommentStatus.Rejected)
    await fetchComments()
  } catch (e) {
    console.error('Failed to reject comment:', e)
  }
}

function handleDeleteClick(comment: CommentAdminDto) {
  commentToDelete.value = comment
  deleteModalOpen.value = true
}

async function handleDeleteConfirm() {
  if (!commentToDelete.value) return

  isDeleting.value = true
  try {
    await deleteComment(commentToDelete.value.id)
    deleteModalOpen.value = false
    commentToDelete.value = null
    await fetchComments()
  } catch (e) {
    console.error('Failed to delete comment:', e)
  } finally {
    isDeleting.value = false
  }
}

function goToPage(page: number) {
  currentPage.value = page
  fetchComments()
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('da-DK', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

function truncate(text: string, maxLength: number): string {
  return text.length > maxLength ? text.substring(0, maxLength) + '...' : text
}
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Comments</h1>
    </div>

    <!-- Filter tabs -->
    <div class="mb-6 flex gap-2">
      <button
        v-for="tab in filterTabs"
        :key="tab.label"
        class="px-4 py-2 text-sm font-medium rounded-md transition-colors"
        :class="activeFilter === tab.value
          ? 'bg-primary-600 text-white'
          : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700'"
        @click="setFilter(tab.value)"
      >
        {{ tab.label }}
        <span
          v-if="tab.count !== undefined && tab.count > 0"
          class="ml-1.5 inline-flex items-center justify-center px-2 py-0.5 text-xs font-bold rounded-full"
          :class="activeFilter === tab.value
            ? 'bg-white/20 text-white'
            : 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300'"
        >
          {{ tab.count }}
        </span>
      </button>
    </div>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading comments...</p>
    </div>

    <div v-else-if="comments.length === 0" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <p class="text-gray-500 dark:text-gray-400">No comments found.</p>
    </div>

    <div v-else class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-700">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Author</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Article</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Content</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Status</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Date</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="comment in comments" :key="comment.id" class="hover:bg-gray-50 dark:hover:bg-gray-700">
            <td class="px-6 py-4 whitespace-nowrap">
              <div class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ comment.authorName }}</div>
              <div class="text-xs text-gray-500 dark:text-gray-400">{{ comment.authorEmail }}</div>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ truncate(comment.articleTitle || 'Unknown', 30) }}
            </td>
            <td class="px-6 py-4 text-sm text-gray-700 dark:text-gray-300 max-w-xs">
              <span :title="comment.content">{{ truncate(comment.content, 80) }}</span>
              <span v-if="comment.parentCommentId" class="block text-xs text-gray-400 dark:text-gray-500 mt-0.5">Reply</span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span
                class="inline-flex px-2 py-0.5 text-xs font-semibold rounded-full"
                :class="getCommentStatusColor(comment.status)"
              >
                {{ getCommentStatusLabel(comment.status) }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ formatDate(comment.createdAt) }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                v-if="comment.status !== 1"
                class="text-green-600 dark:text-green-400 hover:text-green-800 dark:hover:text-green-300 mr-2"
                title="Approve"
                @click="handleApprove(comment)"
              >
                <UiIcon name="check" class="w-5 h-5" />
              </button>
              <button
                v-if="comment.status !== 2"
                class="text-orange-600 dark:text-orange-400 hover:text-orange-800 dark:hover:text-orange-300 mr-2"
                title="Reject"
                @click="handleReject(comment)"
              >
                <UiIcon name="x-mark" class="w-5 h-5" />
              </button>
              <button
                class="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300"
                title="Delete"
                @click="handleDeleteClick(comment)"
              >
                <UiIcon name="trash" class="w-5 h-5" />
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div v-if="pagination && pagination.totalPages > 1" class="px-6 py-3 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
        <div class="text-sm text-gray-500 dark:text-gray-400">
          Page {{ pagination.page }} of {{ pagination.totalPages }} ({{ pagination.totalCount }} total)
        </div>
        <div class="flex gap-1">
          <button
            v-for="page in pagination.totalPages"
            :key="page"
            class="px-3 py-1 text-sm rounded-md"
            :class="page === currentPage
              ? 'bg-primary-600 text-white'
              : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'"
            @click="goToPage(page)"
          >
            {{ page }}
          </button>
        </div>
      </div>
    </div>

    <!-- Delete confirmation modal -->
    <Teleport to="body">
      <div v-if="deleteModalOpen" class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="fixed inset-0 bg-black/50" @click="deleteModalOpen = false" />
        <div class="relative bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-md w-full mx-4 p-6">
          <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">Delete Comment</h3>
          <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">
            Are you sure you want to permanently delete this comment by
            <strong>{{ commentToDelete?.authorName }}</strong>? This action cannot be undone.
          </p>
          <div class="flex justify-end gap-3">
            <button
              class="px-4 py-2 text-sm text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 rounded-md hover:bg-gray-200 dark:hover:bg-gray-600"
              @click="deleteModalOpen = false"
            >
              Cancel
            </button>
            <button
              class="px-4 py-2 text-sm text-white bg-red-600 rounded-md hover:bg-red-700 disabled:opacity-50"
              :disabled="isDeleting"
              @click="handleDeleteConfirm"
            >
              {{ isDeleting ? 'Deleting...' : 'Delete' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
