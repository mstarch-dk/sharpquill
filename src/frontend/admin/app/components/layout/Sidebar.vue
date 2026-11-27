<script setup lang="ts">
defineProps<{
  mobileOpen: boolean
}>()

const emit = defineEmits<{
  close: []
}>()

const route = useRoute()
const { isAdmin } = useAuth()
const { isCollapsed, toggle } = useSidebarCollapse()

const navigation = computed(() => {
  const items = [
    { name: 'Articles', href: '/articles', icon: 'document' },
    { name: 'Series', href: '/series', icon: 'rectangle-stack' },
    { name: 'Pages', href: '/pages', icon: 'page' },
    { name: 'Categories', href: '/categories', icon: 'folder' },
    { name: 'Tags', href: '/tags', icon: 'tag' },
    { name: 'Media', href: '/media', icon: 'photo' },
    { name: 'Navigation', href: '/navigation', icon: 'bars' },
    { name: 'Packages', href: '/packages', icon: 'package' },
    { name: 'Comments', href: '/comments', icon: 'chat' },
    { name: 'Website Users', href: '/website-users', icon: 'website-users' },
  ]
  if (isAdmin.value) {
    items.push({ name: 'Users', href: '/users', icon: 'users' })
  }
  items.push({ name: 'Settings', href: '/settings', icon: 'settings' })
  return items
})

function isActive(href: string): boolean {
  return route.path === href || route.path.startsWith(`${href}/`)
}
</script>

<template>
  <!-- Mobile sidebar -->
  <div
    class="fixed inset-y-0 left-0 z-30 w-64 bg-primary-900 dark:bg-gray-950 transform transition-transform duration-300 ease-in-out lg:hidden"
    :class="mobileOpen ? 'translate-x-0' : '-translate-x-full'"
  >
    <div class="flex h-16 items-center justify-between px-4">
      <NuxtLink to="/" class="flex h-8 items-center gap-2" @click="emit('close')">
        <BrandLogo surface="dark" class="h-8" />
        <span class="text-xs font-medium uppercase tracking-wider text-primary-200">Admin</span>
      </NuxtLink>
      <button
        type="button"
        class="text-primary-200 hover:text-white"
        @click="emit('close')"
      >
        <span class="sr-only">Close sidebar</span>
        <UiIcon name="x-mark" class="h-6 w-6" />
      </button>
    </div>
    <nav class="mt-4 px-2">
      <NuxtLink
        v-for="item in navigation"
        :key="item.name"
        :to="item.href"
        class="group flex items-center px-3 py-2 text-sm font-medium rounded-md mb-1"
        :class="isActive(item.href)
          ? 'bg-primary-800 dark:bg-gray-800 text-white'
          : 'text-primary-100 hover:bg-primary-800 dark:hover:bg-gray-800 hover:text-white'"
        @click="emit('close')"
      >
        <UiIcon :name="item.icon" class="mr-3 h-5 w-5 flex-shrink-0" />
        {{ item.name }}
      </NuxtLink>
    </nav>
  </div>

  <!-- Desktop sidebar -->
  <div
    class="hidden lg:fixed lg:inset-y-0 lg:flex lg:w-64 lg:flex-col transition-transform duration-300 ease-in-out"
    :class="isCollapsed ? '-translate-x-full' : 'translate-x-0'"
  >
    <div class="flex min-h-0 flex-1 flex-col bg-primary-900 dark:bg-gray-950">
      <div class="flex h-16 items-center justify-between px-4">
        <NuxtLink to="/" class="flex h-8 items-center gap-2">
          <BrandLogo surface="dark" class="h-8" />
          <span class="text-xs font-medium uppercase tracking-wider text-primary-200">Admin</span>
        </NuxtLink>
        <button
          type="button"
          class="text-primary-200 hover:text-white p-1 rounded"
          title="Collapse sidebar (Ctrl+B)"
          @click="toggle"
        >
          <UiIcon name="chevron-double-left" class="h-5 w-5" />
        </button>
      </div>
      <nav class="flex-1 px-2 py-4">
        <NuxtLink
          v-for="item in navigation"
          :key="item.name"
          :to="item.href"
          class="group flex items-center px-3 py-2 text-sm font-medium rounded-md mb-1"
          :class="isActive(item.href)
            ? 'bg-primary-800 dark:bg-gray-800 text-white'
            : 'text-primary-100 hover:bg-primary-800 dark:hover:bg-gray-800 hover:text-white'"
        >
          <UiIcon :name="item.icon" class="mr-3 h-5 w-5 flex-shrink-0" />
          {{ item.name }}
        </NuxtLink>
      </nav>
    </div>
  </div>
</template>
