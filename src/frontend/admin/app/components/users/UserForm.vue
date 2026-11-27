<script setup lang="ts">
import type { AuthorAdminDto, AuthorCreateDto, AuthorUpdateDto } from '~/types/author'
import { MIN_AUTHOR_PASSWORD_LENGTH, validateAuthorForm } from '~/utils/authorForm'

interface Props {
  author?: AuthorAdminDto
  loading?: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
  submit: [data: AuthorCreateDto | AuthorUpdateDto]
  cancel: []
}>()

const isEdit = computed(() => !!props.author)

const email = ref(props.author?.email || '')
const name = ref(props.author?.name || '')
const password = ref('')
const role = ref(props.author?.role || 'user')
const bio = ref(props.author?.bio || '')
const certificateThumbprint = ref(props.author?.certificateThumbprint || '')

const errors = ref<Record<string, string>>({})

const roleOptions = [
  { value: 'admin', label: 'Admin' },
  { value: 'user', label: 'User' }
]

function validate(): boolean {
  errors.value = validateAuthorForm({
    email: email.value,
    name: name.value,
    password: password.value,
    isEdit: isEdit.value
  })
  return Object.keys(errors.value).length === 0
}

function handleSubmit() {
  if (!validate()) return

  if (isEdit.value) {
    const data: AuthorUpdateDto = {
      email: email.value.trim(),
      name: name.value.trim(),
      role: role.value,
      password: password.value || undefined,
      bio: bio.value.trim() || undefined,
      certificateThumbprint: certificateThumbprint.value.trim() || undefined
    }
    emit('submit', data)
  } else {
    const data: AuthorCreateDto = {
      email: email.value.trim(),
      password: password.value,
      name: name.value.trim(),
      role: role.value,
      bio: bio.value.trim() || undefined,
      certificateThumbprint: certificateThumbprint.value.trim() || undefined
    }
    emit('submit', data)
  }
}
</script>

<template>
  <form @submit.prevent="handleSubmit" class="space-y-6">
    <div class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-6">
      <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-6">
        {{ isEdit ? 'Edit User' : 'Create User' }}
      </h2>

      <div class="grid grid-cols-1 gap-6">
        <UiInput
          v-model="email"
          label="Email"
          type="email"
          placeholder="user@example.com"
          :error="errors.email"
          required
        />

        <UiInput
          v-model="name"
          label="Name"
          placeholder="Enter name"
          :error="errors.name"
          :maxlength="100"
          required
        />

        <UiInput
          v-model="password"
          label="Password"
          type="password"
          :placeholder="isEdit ? 'Leave blank to keep current password' : 'Enter password'"
          :error="errors.password"
          :hint="`At least ${MIN_AUTHOR_PASSWORD_LENGTH} characters`"
          :required="!isEdit"
        />

        <UiSelect
          v-model="role"
          label="Role"
          :options="roleOptions"
          required
        />

        <UiTextarea
          v-model="bio"
          label="Bio"
          placeholder="Brief bio"
          :maxlength="1000"
          :rows="3"
        />

        <UiInput
          v-model="certificateThumbprint"
          label="Certificate Thumbprint"
          placeholder="Optional certificate thumbprint for cert-based login"
        />
      </div>
    </div>

    <div class="flex justify-end gap-3">
      <UiButton type="button" variant="secondary" @click="$emit('cancel')">
        Cancel
      </UiButton>
      <UiButton type="submit" :loading="loading">
        {{ isEdit ? 'Update User' : 'Create User' }}
      </UiButton>
    </div>
  </form>
</template>
