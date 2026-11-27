import { describe, expect, it, vi } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import ArticleLayout from '../../app/components/ArticleLayout.vue'
import type { RenderedLayoutDto } from '../../app/types/layout'

function layout(...blocks: RenderedLayoutDto['sections'][number]['blocks']): RenderedLayoutDto {
  return { sections: [{ type: 'full', blocks }] }
}

describe('ArticleLayout', () => {
  it('renders each block with its registered renderer', async () => {
    const wrapper = await mountSuspended(ArticleLayout, {
      props: {
        layout: layout(
          { type: 'text', html: '<p>Hello <strong>world</strong></p>' },
          { type: 'code', html: '<pre><code>let x = 1</code></pre>', language: 'ts' },
          { type: 'cta', title: 'Join us', buttonUrl: '/contact', buttonText: 'Contact' }
        )
      }
    })

    expect(wrapper.html()).toContain('Hello <strong>world</strong>')
    expect(wrapper.text()).toContain('let x = 1')
    expect(wrapper.find('a[href="/contact"]').text()).toBe('Contact')
  })

  it('sanitises the HTML it is given', async () => {
    const wrapper = await mountSuspended(ArticleLayout, {
      props: { layout: layout({ type: 'text', html: '<p>ok</p><script>alert(1)</script><img src=x onerror="alert(1)">' }) }
    })

    expect(wrapper.html()).not.toContain('<script')
    expect(wrapper.html()).not.toContain('onerror')
  })

  it('skips blocks of an unknown type and keeps rendering the rest', async () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    const wrapper = await mountSuspended(ArticleLayout, {
      props: {
        layout: layout(
          { type: 'hologram', html: '<p>from the future</p>' },
          { type: '__proto__', html: '<p>sneaky</p>' },
          { type: 'text', html: '<p>still here</p>' }
        )
      }
    })

    expect(wrapper.text()).not.toContain('from the future')
    expect(wrapper.text()).not.toContain('sneaky')
    expect(wrapper.text()).toContain('still here')
    warn.mockRestore()
  })

  it('skips component blocks naming an unknown embed', async () => {
    const wrapper = await mountSuspended(ArticleLayout, {
      props: { layout: layout({ type: 'component', componentName: 'Unknown' }, { type: 'text', html: '<p>after</p>' }) }
    })

    expect(wrapper.text()).toBe('after')
  })

  it('shows a placeholder for embeds in the admin preview', async () => {
    const wrapper = await mountSuspended(ArticleLayout, {
      props: { layout: layout({ type: 'component', componentName: 'ContactForm' }), preview: true }
    })

    expect(wrapper.text()).toBe('Contact Form')
    expect(wrapper.find('form').exists()).toBe(false)
  })

  it('renders nested sections of a container', async () => {
    const wrapper = await mountSuspended(ArticleLayout, {
      props: {
        layout: layout({
          type: 'container',
          sections: [{ type: 'two-column', blocks: [{ type: 'text', html: '<p>left</p>' }, { type: 'text', html: '<p>right</p>' }] }]
        })
      }
    })

    expect(wrapper.findAll('section')).toHaveLength(2)
    expect(wrapper.text()).toContain('left')
    expect(wrapper.text()).toContain('right')
  })

  it('applies the section style', async () => {
    const wrapper = await mountSuspended(ArticleLayout, {
      props: { layout: { sections: [{ type: 'full', style: 'dark', blocks: [{ type: 'text', html: '<p>x</p>' }] }] } }
    })

    expect(wrapper.find('section').classes()).toContain('bg-gray-900')
  })
})
