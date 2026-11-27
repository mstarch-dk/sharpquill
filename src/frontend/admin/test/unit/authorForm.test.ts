import { describe, expect, it } from 'vitest'
import { MIN_AUTHOR_PASSWORD_LENGTH, validateAuthorForm } from '../../app/utils/authorForm'

const valid = { email: 'writer@example.com', name: 'Writer', password: 'a-long-password', isEdit: false }

describe('validateAuthorForm', () => {
  it('accepts a complete new author', () => {
    expect(validateAuthorForm(valid)).toEqual({})
  })

  it('requires the policy minimum of 12 characters', () => {
    expect(MIN_AUTHOR_PASSWORD_LENGTH).toBe(12)
    expect(validateAuthorForm({ ...valid, password: 'x'.repeat(11) }).password).toMatch(/at least 12/)
    expect(validateAuthorForm({ ...valid, password: 'x'.repeat(12) }).password).toBeUndefined()
  })

  it('rejects a password of only spaces', () => {
    expect(validateAuthorForm({ ...valid, password: ' '.repeat(20) }).password).toMatch(/not only spaces/)
  })

  it('requires a password on create but not on edit', () => {
    expect(validateAuthorForm({ ...valid, password: '' }).password).toBe('Password is required')
    expect(validateAuthorForm({ ...valid, password: '', isEdit: true })).toEqual({})
  })

  it('checks a new password on edit', () => {
    expect(validateAuthorForm({ ...valid, password: 'short', isEdit: true }).password).toMatch(/at least 12/)
  })

  it.each(['', '   ', 'not-an-email', 'a@b', 'a b@example.com'])('rejects email %j', (email) => {
    expect(validateAuthorForm({ ...valid, email }).email).toBeDefined()
  })

  it('accepts an email with surrounding spaces', () => {
    expect(validateAuthorForm({ ...valid, email: '  writer@example.com ' }).email).toBeUndefined()
  })

  it('rejects a blank or over-long name', () => {
    expect(validateAuthorForm({ ...valid, name: '   ' }).name).toBe('Name is required')
    expect(validateAuthorForm({ ...valid, name: 'n'.repeat(101) }).name).toMatch(/100/)
  })
})
