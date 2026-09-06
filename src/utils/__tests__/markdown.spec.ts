import { describe, expect, it } from 'vitest'
import { resolveRelative } from '@/utils/markdown'

const base = 'https://raw.githubusercontent.com/acme/tool/HEAD'

describe('resolveRelative', () => {
  it('rewrites relative image and link paths against the base', () => {
    expect(resolveRelative('<img src="docs/cpu.png">', base)).toBe('<img src="https://raw.githubusercontent.com/acme/tool/HEAD/docs/cpu.png">')
    expect(resolveRelative('<a href="./CHANGELOG.md">x</a>', `${base}/`)).toBe('<a href="https://raw.githubusercontent.com/acme/tool/HEAD/CHANGELOG.md">x</a>')
  })

  it('leaves absolute, protocol-relative, root, anchor, mailto and data URLs alone', () => {
    for (const v of ['https://x/y.png', '//cdn/x.png', '/root.png', '#section', 'mailto:a@b.c', 'data:image/png;base64,AAA', '']) {
      const markup = `<img src="${v}">`
      expect(resolveRelative(markup, base)).toBe(markup)
    }
  })
})
