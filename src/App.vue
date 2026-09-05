<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import tr from 'element-plus/es/locale/lang/tr'
import en from 'element-plus/es/locale/lang/en'

const { locale } = useI18n()
const elLocale = computed(() => (locale.value === 'tr-TR' ? tr : en))
</script>

<template>
  <ElConfigProvider :locale="elLocale" :button="{ autoInsertSpace: true }">
    <RouterView />
  </ElConfigProvider>
</template>
