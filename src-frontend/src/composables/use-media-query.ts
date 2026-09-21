import { computed, onBeforeUnmount, onMounted, ref, type ComputedRef, type Ref } from 'vue'

/** Reactive `window.matchMedia` — false until mounted and wherever matchMedia is missing (tests, prerender). */
export function useMediaQuery(query: string): Ref<boolean> {
  const matches = ref(false)
  let mql: MediaQueryList | null = null
  const onChange = (e: MediaQueryListEvent) => (matches.value = e.matches)

  onMounted(() => {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return
    mql = window.matchMedia(query)
    matches.value = mql.matches
    mql.addEventListener('change', onChange)
  })
  onBeforeUnmount(() => mql?.removeEventListener('change', onChange))
  return matches
}

/** `fixed` value for a table's action column: pinned right on desktop, a normal column on phones where it would cover the data. */
export function useTablePin(): ComputedRef<'right' | false> {
  const narrow = useMediaQuery('(max-width: 860px)')
  return computed(() => (narrow.value ? false : 'right'))
}
