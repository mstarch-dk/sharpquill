import { defineBlock } from '../types'

export default defineBlock({
  type: 'tweet',
  label: 'Tweet',
  order: 80,
  create: () => ({ type: 'tweet', url: '' })
})
