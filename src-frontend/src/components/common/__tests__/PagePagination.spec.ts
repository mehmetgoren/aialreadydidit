import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import PagePagination from '@/components/common/PagePagination.vue'

describe('PagePagination', () => {
  it('renders nothing when everything fits on one page', () => {
    const w = mount(PagePagination, { props: { page: 1, pageSize: 24, total: 10 } })
    expect(w.find('.page-pagination').exists()).toBe(false)
  })

  it('emits update:page when a page is clicked', async () => {
    const w = mount(PagePagination, { props: { page: 1, pageSize: 10, total: 35 } })
    const pages = w.findAll('.el-pager li')
    expect(pages).toHaveLength(4)
    await pages[2]!.trigger('click')
    expect(w.emitted('update:page')?.[0]).toEqual([3])
  })
})
