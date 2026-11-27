<script setup lang="ts">
const emit = defineEmits<{
  toggleSidebar: []
}>()

const { logout } = useAuth()
const router = useRouter()
const { isCollapsed, toggle: toggleCollapse } = useSidebarCollapse()

const isLoggingOut = ref(false)

async function handleLogout() {
  isLoggingOut.value = true
  await logout()
  router.push('/login')
}
</script>

<template>
  <header class="sticky top-0 z-10 bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30">
    <div class="flex h-16 items-center justify-between px-4 sm:px-6 lg:px-8">
      <div class="flex items-center gap-2">
        <!-- Mobile menu button -->
        <button
          type="button"
          class="lg:hidden -m-2.5 p-2.5 text-gray-700 dark:text-gray-300"
          @click="emit('toggleSidebar')"
        >
          <span class="sr-only">Open sidebar</span>
          <UiIcon name="bars" class="h-6 w-6" />
        </button>

        <!-- Desktop expand sidebar button (shown when collapsed) -->
        <button
          v-if="isCollapsed"
          type="button"
          class="hidden lg:flex items-center gap-2 -m-2.5 p-2.5 text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100"
          title="Expand sidebar (Ctrl+B)"
          @click="toggleCollapse"
        >
          <UiIcon name="chevron-double-right" class="h-5 w-5" />
        </button>

        <!-- The mark stands in for the sidebar logo while the sidebar is hidden (mobile, or collapsed on desktop) -->
        <NuxtLink to="/" class="ml-2 flex h-7 items-center" :class="isCollapsed ? '' : 'lg:hidden'">
          <BrandMark class="h-7" />
        </NuxtLink>
      </div>

      <!-- User menu -->
      <div class="flex items-center gap-4">
        <button
          type="button"
          class="inline-flex items-center px-3 py-1.5 text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 disabled:opacity-50"
          :disabled="isLoggingOut"
          @click="handleLogout"
        >
          <UiIcon name="logout" class="mr-1.5 h-4 w-4" />
          {{ isLoggingOut ? 'Logging out...' : 'Logout' }}
        </button>
      </div>
    </div>
  </header>
</template>
