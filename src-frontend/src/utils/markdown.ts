/** Rewrites relative src/href attributes against baseUrl; absolute, anchor, mailto and data URLs are left alone. */
export function resolveRelative(markup: string, baseUrl: string): string {
  const base = baseUrl.endsWith('/') ? baseUrl : `${baseUrl}/`
  return markup.replace(/\b(src|href)="([^"]*)"/g, (whole, attr: string, value: string) => {
    if (!value || /^(?:[a-z][a-z0-9+.-]*:|\/\/|#|\/)/i.test(value)) return whole
    return `${attr}="${base}${value.replace(/^\.\//, '')}"`
  })
}

