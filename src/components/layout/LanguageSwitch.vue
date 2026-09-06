<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { SUPPORTED_LOCALES, setLocale, type AppLocale } from '@/boot/i18n'
import { isDark, toggleDark } from '@/boot/theme'
import { localeDef } from '@/i18n/locales'

const { locale, t } = useI18n()
function onCommand(cmd: string) {
  if (cmd === 'theme') return toggleDark()
  void setLocale(cmd as AppLocale)
}
</script>

<template>
  <ElDropdown trigger="click" @command="onCommand">
    <span class="lang" :title="localeDef(String(locale)).label">
      <span class="lang__code">{{ localeDef(String(locale)).short }}</span>
      <ElIcon><ArrowDown /></ElIcon>
    </span>
    <template #dropdown>
      <ElDropdownMenu>
        <ElDropdownItem v-for="l in SUPPORTED_LOCALES" :key="l.value" :command="l.value" :disabled="locale === l.value">
          <span class="lang__code lang__code--menu">{{ l.short }}</span>{{ l.label }}
        </ElDropdownItem>
        <ElDropdownItem command="theme" divided>
          <ElIcon><component :is="isDark ? 'Sunny' : 'Moon'" /></ElIcon>{{ isDark ? t('light_theme') : t('dark_theme') }}
        </ElDropdownItem>
      </ElDropdownMenu>
    </template>
  </ElDropdown>
</template>

<style scoped>
.lang {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  cursor: pointer;
  outline: none;
}
.lang__code {
  display: inline-block;
  min-width: 26px;
  padding: 1px 5px;
  border: 1px solid var(--gm-border);
  border-radius: 6px;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.04em;
  line-height: 16px;
  text-align: center;
  color: var(--gm-text);
}
.lang__code--menu {
  margin-inline-end: 8px;
}
</style>
