import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import MarkdownEditor from '@/components/common/MarkdownEditor.vue'
import { i18n } from '@/boot/i18n'

const render = (modelValue = '', extra: Record<string, unknown> = {}) =>
  mount(MarkdownEditor, { props: { modelValue, ...extra }, global: { plugins: [i18n] }, attachTo: document.body })

const emitted = (w: ReturnType<typeof render>) => (w.emitted('update:modelValue')?.at(-1) as string[] | undefined)?.[0]
const select = (w: ReturnType<typeof render>, start: number, end: number) => {
  const el = w.find('textarea').element as HTMLTextAreaElement
  el.setSelectionRange(start, end)
}

describe('MarkdownEditor', () => {
  it('emits the typed value', async () => {
    const w = render('')
    await w.find('textarea').setValue('hello')
    expect(emitted(w)).toBe('hello')
    w.unmount()
  })

  it('wraps the selection in bold and italic', async () => {
    const w = render('make this bold')
    select(w, 5, 9)
    await w.find('.md-editor__tool--md_bold').trigger('click')
    expect(emitted(w)).toBe('make **this** bold')
    select(w, 0, 4)
    await w.find('.md-editor__tool--md_italic').trigger('click')
    expect(emitted(w)).toBe('_make_ this bold')
    w.unmount()
  })

  it('inserts a placeholder when nothing is selected', async () => {
    const w = render('')
    select(w, 0, 0)
    await w.find('.md-editor__tool--md_link').trigger('click')
    expect(emitted(w)).toBe('[link text](https://)')
    w.unmount()
  })

  it('prefixes every selected line for headings and numbered lists', async () => {
    const w = render('one\ntwo\nthree')
    select(w, 0, 3)
    await w.find('.md-editor__tool--md_heading').trigger('click')
    expect(emitted(w)).toBe('## one\ntwo\nthree')
    const w2 = render('one\ntwo\nthree')
    select(w2, 4, 13)
    await w2.find('.md-editor__tool--md_numbers').trigger('click')
    expect(emitted(w2)).toBe('one\n1. two\n2. three')
    w.unmount()
    w2.unmount()
  })

  it('uses a fenced block for multi-line code and backticks otherwise', async () => {
    const w = render('a\nb')
    select(w, 0, 3)
    await w.find('.md-editor__tool--md_code').trigger('click')
    expect(emitted(w)).toBe('```\na\nb\n```')
    const w2 = render('x')
    select(w2, 0, 1)
    await w2.find('.md-editor__tool--md_code').trigger('click')
    expect(emitted(w2)).toBe('`x`')
    w.unmount()
    w2.unmount()
  })

  it('ctrl+b is a shortcut for bold', async () => {
    const w = render('hi')
    select(w, 0, 2)
    await w.find('textarea').trigger('keydown', { key: 'b', ctrlKey: true })
    expect(emitted(w)).toBe('**hi**')
    w.unmount()
  })

  it('preview renders sanitised markdown and disables the tools', async () => {
    const w = render('# Title\n\n<script>alert(1)</script>\n\n**b**')
    await w.findAll('.md-editor__mode')[1]!.trigger('click')
    await nextTick()
    expect(w.find('.md-editor__preview h1').text()).toBe('Title')
    expect(w.find('.md-editor__preview strong').text()).toBe('b')
    expect(w.find('.md-editor__preview').html()).not.toContain('<script')
    expect((w.find('.md-editor__tool--md_bold').element as HTMLButtonElement).disabled).toBe(true)
    expect(w.find('textarea').isVisible()).toBe(false)
    w.unmount()
  })

  it('shows the empty-preview hint and the character count', async () => {
    const w = render('', { maxlength: 100 })
    expect(w.find('.md-editor__count').text()).toBe('0 / 100')
    await w.findAll('.md-editor__mode')[1]!.trigger('click')
    expect(w.find('.md-editor__empty').exists()).toBe(true)
    w.unmount()
  })
})
