<template>
  <div class="min-h-screen flex flex-col bg-page text-base font-body">
    <AppHeader />
    <Breadcrumbs />
    <main class="flex-1">
      <slot />
    </main>
    <AppFooter />
  </div>
</template>
