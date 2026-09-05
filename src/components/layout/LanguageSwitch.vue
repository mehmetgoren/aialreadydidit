<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { SUPPORTED_LOCALES, setLocale, type AppLocale } from '@/boot/i18n'
import { isDark, toggleDark } from '@/boot/theme'
import { getFlagImgSrc } from '@/utils/tools'

const { locale, t } = useI18n()
function onCommand(cmd: string) {
  if (cmd === 'theme') return toggleDark()
  setLocale(cmd as AppLocale)
}
</script>

<template>
  <ElDropdown trigger="click" @command="onCommand">
    <span class="lang">
      <img :src="getFlagImgSrc(String(locale))" alt="" width="20" height="14" />
      <ElIcon><ArrowDown /></ElIcon>
    </span>
    <template #dropdown>
      <ElDropdownMenu>
        <ElDropdownItem v-for="l in SUPPORTED_LOCALES" :key="l.value" :command="l.value" :disabled="locale === l.value">
          <img :src="getFlagImgSrc(l.value)" alt="" width="18" height="12" style="margin-right: 8px" />{{ l.label }}
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
</style>
