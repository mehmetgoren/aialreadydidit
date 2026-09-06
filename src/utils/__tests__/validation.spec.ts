import { describe, expect, it } from 'vitest'
import type { FormItemRule } from 'element-plus'
import { createRules, email, maxLength, maxValue, minLength, minValue, pattern, required, url } from '@/utils/validation/validation'

/** Runs an Element Plus validator rule and resolves with the error message (or null when valid). */
function run(rule: FormItemRule, value: unknown): string | null {
  let out: string | null = null
  const validator = rule.validator as (r: unknown, v: unknown, cb: (e?: Error) => void) => void
  validator(rule, value, (e) => {
    out = e?.message ?? null
  })
  return out
}

describe('validation rules', () => {
  it('required / minLength / maxLength carry default i18n messages', () => {
    expect(required()).toMatchObject({ required: true, message: 'This field is required' })
    expect(required('Custom')).toMatchObject({ message: 'Custom' })
    expect(minLength(3)).toMatchObject({ min: 3, message: 'At least 3 characters' })
    expect(maxLength(80)).toMatchObject({ max: 80, message: 'At most 80 characters' })
  })

  it('pattern skips empty values and reports the message otherwise', () => {
    const r = pattern(/^[a-z]+$/, 'letters only')
    expect(run(r, '')).toBeNull()
    expect(run(r, null)).toBeNull()
    expect(run(r, [])).toBeNull()
    expect(run(r, 'abc')).toBeNull()
    expect(run(r, 'ab1')).toBe('letters only')
  })

  it('email', () => {
    const r = email()
    expect(run(r, 'someone@example.com')).toBeNull()
    expect(run(r, 'someone@example')).toBe('Enter a valid e-mail address')
    expect(run(r, 'no spaces@example.com')).not.toBeNull()
  })

  it('url accepts only http(s)', () => {
    const r = url()
    expect(run(r, 'https://github.com/x/y')).toBeNull()
    expect(run(r, 'HTTP://example.org')).toBeNull()
    expect(run(r, 'ftp://example.org')).toBe('Enter a valid URL (https://…)')
    expect(run(r, 'github.com/x')).not.toBeNull()
  })

  it('minValue / maxValue coerce numbers', () => {
    expect(run(minValue(10), '10')).toBeNull()
    expect(run(minValue(10), 9)).toBe('Must be at least 10')
    expect(run(maxValue(100), 100)).toBeNull()
    expect(run(maxValue(100), '101')).toBe('Must be at most 100')
    expect(run(maxValue(100), '')).toBeNull()
  })

  it('createRules is an identity for typing', () => {
    const rules = { name: [required(), maxLength(5)] }
    expect(createRules(rules)).toBe(rules)
  })
})
