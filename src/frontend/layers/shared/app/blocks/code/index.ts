import { defineBlock } from '../types'

export default defineBlock({
  type: 'code',
  label: 'Code',
  order: 70,
  create: () => ({ type: 'code', content: '', language: '' })
})
