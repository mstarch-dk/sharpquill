/** Same minimum as the API (AuthorRules.MinPasswordLength) and db-init. */
export const MIN_AUTHOR_PASSWORD_LENGTH = 12

const EMAIL_SHAPE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export interface AuthorFormInput {
  email: string
  name: string
  password: string
  isEdit: boolean
}

/**
 * Checks the user form before it is sent; the API applies the same rules and its message is shown if they differ.
 * On edit an empty password keeps the current one. Returns field name -> message, empty when valid.
 */
export function validateAuthorForm(input: AuthorFormInput): Record<string, string> {
  const errors: Record<string, string> = {}

  const email = input.email.trim()
  if (!email) {
    errors.email = 'Email is required'
  } else if (!EMAIL_SHAPE.test(email)) {
    errors.email = 'Invalid email format'
  }

  const name = input.name.trim()
  if (!name) {
    errors.name = 'Name is required'
  } else if (name.length > 100) {
    errors.name = 'Name must be at most 100 characters'
  }

  if (!input.isEdit && !input.password) {
    errors.password = 'Password is required'
  } else if (input.password && (!input.password.trim() || input.password.length < MIN_AUTHOR_PASSWORD_LENGTH)) {
    errors.password = `Password must be at least ${MIN_AUTHOR_PASSWORD_LENGTH} characters and not only spaces`
  }

  return errors
}
