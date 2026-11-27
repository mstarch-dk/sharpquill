<script setup lang="ts">
const sidebarOpen = ref(false)
const { isCollapsed } = useSidebarCollapse()
</script>

<template>
  <div class="min-h-screen bg-gray-100 dark:bg-gray-900 font-body">
    <!-- Mobile sidebar backdrop -->
    <div
      v-if="sidebarOpen"
      class="fixed inset-0 bg-gray-600 bg-opacity-75 dark:bg-gray-900 dark:bg-opacity-80 z-20 lg:hidden"
      @click="sidebarOpen = false"
    />

    <!-- Sidebar -->
    <LayoutSidebar
      :mobile-open="sidebarOpen"
      @close="sidebarOpen = false"
    />

    <!-- Main content -->
    <div
      class="transition-[padding] duration-300 ease-in-out"
      :class="isCollapsed ? 'lg:pl-0' : 'lg:pl-64'"
    >
      <LayoutHeader @toggle-sidebar="sidebarOpen = !sidebarOpen" />

      <main class="py-6">
        <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <slot />
        </div>
      </main>
    </div>
  </div>
</template>
