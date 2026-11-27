<script setup lang="ts">
import type { WebsiteUserDto, WebsiteUserCountsDto } from '~/types/websiteUser'
import { WebsiteUserStatus, getWebsiteUserStatusLabel, getWebsiteUserStatusColor } from '~/types/websiteUser'
import type { PaginationMeta } from '~/types/api'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Website Users'
})

const { getUsers, getCounts, moderateUser, generateLoginToken } = useAdminWebsiteUsers()
const config = useRuntimeConfig()

const users = ref<WebsiteUserDto[]>([])
const counts = ref<WebsiteUserCountsDto>({ pending: 0, approved: 0, rejected: 0 })
const pagination = ref<PaginationMeta | null>(null)
const isLoading = ref(true)
const error = ref('')
const currentPage = ref(1)
const activeFilter = ref<WebsiteUserStatus | null>(null)

const tokenModalOpen = ref(false)
const generatedToken = ref('')
const tokenCopied = ref(false)

interface FilterTab {
  label: string
  value: WebsiteUserStatus | null
  count?: number
}

const filterTabs = computed<FilterTab[]>(() => [
  { label: 'All', value: null },
  { label: 'Pending', value: WebsiteUserStatus.Pending, count: counts.value.pending },
  { label: 'Approved', value: WebsiteUserStatus.Approved, count: counts.value.approved },
  { label: 'Rejected', value: WebsiteUserStatus.Rejected, count: counts.value.rejected },
])

const loginUrl = computed(() => {
  const webBase = config.public.webBase || ''
  return `${webBase}/login?token=${generatedToken.value}`
})

async function fetchUsers() {
  isLoading.value = true
  error.value = ''

  try {
    const [usersResponse, countsResponse] = await Promise.all([
      getUsers({ page: currentPage.value, pageSize: 20, status: activeFilter.value }),
      getCounts()
    ])
    users.value = usersResponse.data
    pagination.value = usersResponse.meta ?? null
    counts.value = countsResponse.data
  } catch (e) {
    error.value = 'Failed to load website users'
    console.error('Failed to load website users:', e)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchUsers()
})

function setFilter(status: WebsiteUserStatus | null) {
  activeFilter.value = status
  currentPage.value = 1
  fetchUsers()
}

async function handleApprove(user: WebsiteUserDto) {
  try {
    await moderateUser(user.id, WebsiteUserStatus.Approved)
    await fetchUsers()
  } catch (e) {
    console.error('Failed to approve user:', e)
  }
}

async function handleReject(user: WebsiteUserDto) {
  try {
    await moderateUser(user.id, WebsiteUserStatus.Rejected)
    await fetchUsers()
  } catch (e) {
    console.error('Failed to reject user:', e)
  }
}

async function handleGenerateToken(user: WebsiteUserDto) {
  try {
    const response = await generateLoginToken(user.id)
    generatedToken.value = response.data.token
    tokenCopied.value = false
    tokenModalOpen.value = true
  } catch (e) {
    console.error('Failed to generate token:', e)
  }
}

async function copyLoginUrl() {
  try {
    await navigator.clipboard.writeText(loginUrl.value)
    tokenCopied.value = true
    setTimeout(() => { tokenCopied.value = false }, 2000)
  } catch {
    // Fallback
    const textarea = document.createElement('textarea')
    textarea.value = loginUrl.value
    document.body.appendChild(textarea)
    textarea.select()
    document.execCommand('copy')
    document.body.removeChild(textarea)
    tokenCopied.value = true
    setTimeout(() => { tokenCopied.value = false }, 2000)
  }
}

function goToPage(page: number) {
  currentPage.value = page
  fetchUsers()
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
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Website Users</h1>
      <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Manage website user signups and access</p>
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
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading users...</p>
    </div>

    <div v-else-if="users.length === 0" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <p class="text-gray-500 dark:text-gray-400">No website users found.</p>
    </div>

    <div v-else class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-700">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Name</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Email</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Status</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Signed Up</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="user in users" :key="user.id" class="hover:bg-gray-50 dark:hover:bg-gray-700">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
              {{ user.name }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ user.email }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span
                class="inline-flex px-2 py-0.5 text-xs font-semibold rounded-full"
                :class="getWebsiteUserStatusColor(user.status)"
              >
                {{ getWebsiteUserStatusLabel(user.status) }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ formatDate(user.createdAt) }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                v-if="user.status !== WebsiteUserStatus.Approved"
                class="text-green-600 dark:text-green-400 hover:text-green-800 dark:hover:text-green-300 mr-2"
                title="Approve"
                @click="handleApprove(user)"
              >
                <UiIcon name="check" class="w-5 h-5 inline" />
              </button>
              <button
                v-if="user.status !== WebsiteUserStatus.Rejected"
                class="text-orange-600 dark:text-orange-400 hover:text-orange-800 dark:hover:text-orange-300 mr-2"
                title="Reject"
                @click="handleReject(user)"
              >
                <UiIcon name="x-mark" class="w-5 h-5 inline" />
              </button>
              <button
                v-if="user.status === WebsiteUserStatus.Approved"
                class="text-primary-600 dark:text-primary-400 hover:text-primary-800 dark:hover:text-primary-300"
                title="Generate Login Token"
                @click="handleGenerateToken(user)"
              >
                <UiIcon name="key" class="w-5 h-5 inline" />
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

    <!-- Login Token Modal -->
    <Teleport to="body">
      <div v-if="tokenModalOpen" class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="fixed inset-0 bg-black/50" @click="tokenModalOpen = false" />
        <div class="relative bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-lg w-full mx-4 p-6">
          <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">Login Token Generated</h3>
          <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">
            Share this login URL with the user. It expires in 7 days and can only be used once.
          </p>
          <div class="bg-gray-50 dark:bg-gray-900 rounded-md p-3 mb-4">
            <code class="text-sm text-gray-800 dark:text-gray-200 break-all">{{ loginUrl }}</code>
          </div>
          <div class="flex justify-end gap-3">
            <button
              class="px-4 py-2 text-sm text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 rounded-md hover:bg-gray-200 dark:hover:bg-gray-600"
              @click="tokenModalOpen = false"
            >
              Close
            </button>
            <button
              class="px-4 py-2 text-sm text-white bg-primary-600 rounded-md hover:bg-primary-700"
              @click="copyLoginUrl"
            >
              {{ tokenCopied ? 'Copied!' : 'Copy URL' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
