import { embedDefinitions } from '../../embeds/registry'
import { defineBlock } from '../types'

// Places an embeddable component (embeds/<name>/); the API accepts only names in its own list
export default defineBlock({
  type: 'component',
  label: 'Component',
  order: 90,
  create: () => ({ type: 'component', componentName: embedDefinitions[0]?.name })
})
