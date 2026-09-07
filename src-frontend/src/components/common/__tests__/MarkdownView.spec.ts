import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import MarkdownView from '@/components/common/MarkdownView.vue'

describe('MarkdownView', () => {
  it('renders GFM markdown', () => {
    const w = mount(MarkdownView, { props: { source: '# Title\n\n- one\n- two\n\n| a | b |\n|---|---|\n| 1 | 2 |' } })
    expect(w.find('h1').text()).toBe('Title')
    expect(w.findAll('li')).toHaveLength(2)
    expect(w.find('table').exists()).toBe(true)
  })

  it('strips scripts and event handlers', () => {
    const w = mount(MarkdownView, { props: { source: 'Hi <script>alert(1)</script><img src=x onerror="alert(1)"> <a href="javascript:alert(1)">x</a>' } })
    expect(w.html()).not.toContain('<script')
    expect(w.html()).not.toContain('onerror')
    expect(w.html()).not.toContain('javascript:')
  })

  it('inline mode renders without block wrappers and empty source renders nothing', () => {
    expect(mount(MarkdownView, { props: { source: 'a **b**', inline: true } }).html()).not.toContain('<p>')
    expect(mount(MarkdownView, { props: { source: null } }).find('.gm-markdown').element.innerHTML).toBe('')
  })
})
