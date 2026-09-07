import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import StatusTag from '@/components/common/StatusTag.vue'
import { i18n } from '@/boot/i18n'

const render = (props: { value: string | null | undefined; prefix?: string }) => mount(StatusTag, { props, global: { plugins: [i18n] } })

describe('StatusTag', () => {
  it('translates known statuses and picks the tag type', () => {
    const w = render({ value: 'published' })
    expect(w.text()).toBe('Published')
    expect(w.find('.el-tag--success').exists()).toBe(true)
    expect(render({ value: 'pendingReview' }).find('.el-tag--warning').exists()).toBe(true)
    expect(render({ value: 'infected' }).find('.el-tag--danger').exists()).toBe(true)
  })

  it('falls back to the raw value for unknown statuses', () => {
    const w = render({ value: 'weirdState' })
    expect(w.text()).toBe('weirdState')
    expect(w.find('.el-tag--info').exists()).toBe(true)
    expect(render({ value: null }).text()).toBe('—')
  })

  it('supports a custom key prefix', () => {
    expect(render({ value: 'published', prefix: 'nope' }).text()).toBe('published')
  })
})
