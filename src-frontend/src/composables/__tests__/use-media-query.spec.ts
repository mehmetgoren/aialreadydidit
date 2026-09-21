import { afterEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick } from 'vue'
import { mount } from '@vue/test-utils'
import { useMediaQuery, useTablePin } from '../use-media-query'

type Listener = (e: MediaQueryListEvent) => void

function stubMatchMedia(initial: boolean) {
  const listeners = new Set<Listener>()
  const mql = {
    matches: initial,
    addEventListener: vi.fn((_: string, l: Listener) => listeners.add(l)),
    removeEventListener: vi.fn((_: string, l: Listener) => listeners.delete(l)),
  }
  vi.stubGlobal('matchMedia', vi.fn(() => mql))
  return { mql, emit: (matches: boolean) => listeners.forEach((l) => l({ matches } as MediaQueryListEvent)), listeners }
}

function host<T>(factory: () => T) {
  let value!: T
  const wrapper = mount(defineComponent({ setup: () => ((value = factory()), () => h('div')) }))
  return { wrapper, value }
}

describe('useMediaQuery', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('stays false when matchMedia is not available', () => {
    vi.stubGlobal('matchMedia', undefined)
    expect(host(() => useMediaQuery('(max-width: 860px)')).value.value).toBe(false)
  })

  it('reads the initial state, follows changes and unsubscribes on unmount', async () => {
    const media = stubMatchMedia(true)
    const { wrapper, value } = host(() => useMediaQuery('(max-width: 860px)'))
    expect(window.matchMedia).toHaveBeenCalledWith('(max-width: 860px)')
    expect(value.value).toBe(true)

    media.emit(false)
    await nextTick()
    expect(value.value).toBe(false)

    wrapper.unmount()
    expect(media.listeners.size).toBe(0)
  })
})

describe('useTablePin', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('pins the action column on wide screens and releases it on narrow ones', async () => {
    const media = stubMatchMedia(false)
    const { value } = host(() => useTablePin())
    expect(value.value).toBe('right')
    media.emit(true)
    await nextTick()
    expect(value.value).toBe(false)
  })
})
