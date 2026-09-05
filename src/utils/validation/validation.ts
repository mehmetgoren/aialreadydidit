import type { FormItemRule, FormRules } from 'element-plus'
import { i18n } from '@/boot/i18n'

/**
 * Tiny validation helper (prototype: `utils/validation`), adapted to Element Plus.
 * Every factory returns a plain `FormItemRule`, so rules compose with `ElForm`'s `:rules`.
 */
export type Rule = FormItemRule

const t = (key: string, params?: Record<string, unknown>) => i18n.global.t(key, params ?? {})

function isEmpty(value: unknown): boolean {
  return value === null || value === undefined || (typeof value === 'string' && value.trim().length === 0) || (Array.isArray(value) && value.length === 0)
}

export function required(message?: string): Rule {
  return { required: true, message: message ?? t('v_required'), trigger: ['blur', 'change'] }
}

export function minLength(min: number, message?: string): Rule {
  return { min, message: message ?? t('v_min_length', { n: min }), trigger: 'blur' }
}

export function maxLength(max: number, message?: string): Rule {
  return { max, message: message ?? t('v_max_length', { n: max }), trigger: 'blur' }
}

export function pattern(regex: RegExp, message?: string): Rule {
  return {
    trigger: 'blur',
    validator: (_rule, value, callback) => {
      if (isEmpty(value)) return callback()
      return regex.test(String(value)) ? callback() : callback(new Error(message ?? t('v_invalid')))
    },
  }
}

export function email(message?: string): Rule {
  return pattern(/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/, message ?? t('v_invalid_email'))
}

export function url(message?: string): Rule {
  return pattern(/^https?:\/\/[^\s]+$/i, message ?? t('v_invalid_url'))
}

export function minValue(min: number, message?: string): Rule {
  return {
    trigger: ['blur', 'change'],
    validator: (_rule, value, callback) => {
      if (isEmpty(value)) return callback()
      return Number(value) >= min ? callback() : callback(new Error(message ?? t('v_min', { n: min })))
    },
  }
}

export function maxValue(max: number, message?: string): Rule {
  return {
    trigger: ['blur', 'change'],
    validator: (_rule, value, callback) => {
      if (isEmpty(value)) return callback()
      return Number(value) <= max ? callback() : callback(new Error(message ?? t('v_max', { n: max })))
    },
  }
}

/** Identity helper that types an object literal as Element Plus form rules. */
export function createRules<T extends Record<string, Rule[]>>(map: T): FormRules {
  return map as unknown as FormRules
}
