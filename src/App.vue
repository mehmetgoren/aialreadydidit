<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Language } from 'element-plus/es/locale'
import en from 'element-plus/es/locale/lang/en'
import tr from 'element-plus/es/locale/lang/tr'
import es from 'element-plus/es/locale/lang/es'
import ptBr from 'element-plus/es/locale/lang/pt-br'
import de from 'element-plus/es/locale/lang/de'
import fr from 'element-plus/es/locale/lang/fr'
import ar from 'element-plus/es/locale/lang/ar'
import ru from 'element-plus/es/locale/lang/ru'
import ja from 'element-plus/es/locale/lang/ja'
import ko from 'element-plus/es/locale/lang/ko'
import zhCn from 'element-plus/es/locale/lang/zh-cn'
import type { AppLocale } from '@/i18n/locales'

/** Element Plus built-in translations (date pickers, pagination, empty states…) per UI language. */
const elementLocales: Record<AppLocale, Language> = {
  'en-US': en,
  'tr-TR': tr,
  'es-ES': es,
  'pt-BR': ptBr,
  'de-DE': de,
  'fr-FR': fr,
  'ar-SA': ar,
  'ru-RU': ru,
  'ja-JP': ja,
  'ko-KR': ko,
  'zh-CN': zhCn,
}

const { locale } = useI18n()
const elLocale = computed(() => elementLocales[locale.value as AppLocale] ?? en)
</script>

<template>
  <ElConfigProvider :locale="elLocale" :button="{ autoInsertSpace: true }">
    <RouterView />
  </ElConfigProvider>
</template>
