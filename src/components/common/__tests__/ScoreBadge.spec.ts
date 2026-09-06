import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import ScoreBadge from '@/components/common/ScoreBadge.vue'

const render = (props: { score: number; count?: number; large?: boolean }) => mount(ScoreBadge, { props, global: { stubs: { StarFilled: true } } })

describe('ScoreBadge', () => {
  it('shows a dash when nobody rated yet', () => {
    const w = render({ score: 0 })
    expect(w.text()).toContain('—')
    expect(w.classes()).toContain('is-none')
  })

  it('colours by score and shows the count', () => {
    expect(render({ score: 91.4, count: 12 }).classes()).toContain('is-good')
    expect(render({ score: 60, count: 1 }).classes()).toContain('is-mid')
    expect(render({ score: 54.9, count: 3 }).classes()).toContain('is-bad')
    const w = render({ score: 91.4, count: 12 })
    expect(w.text()).toContain('91')
    expect(w.text()).toContain('/100')
    expect(w.text()).toContain('(12)')
  })

  it('large variant hides the count', () => {
    const w = render({ score: 80, count: 5, large: true })
    expect(w.classes()).toContain('is-large')
    expect(w.text()).not.toContain('(5)')
  })
})
