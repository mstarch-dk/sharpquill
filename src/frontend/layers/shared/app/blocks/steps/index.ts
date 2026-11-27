import { defineBlock } from '../types'

export default defineBlock({
  type: 'steps',
  label: 'Steps',
  order: 40,
  create: () => ({ type: 'steps', heading: '', items: [{ number: '01', title: '', description: '' }] })
})
