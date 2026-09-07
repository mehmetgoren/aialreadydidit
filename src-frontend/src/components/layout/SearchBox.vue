<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const q = ref(typeof route.query.q === 'string' ? route.query.q : '')

watch(
  () => route.query.q,
  (v) => (q.value = typeof v === 'string' ? v : ''),
)

function submit() {
  const term = q.value.trim()
  router.push({ path: '/search', query: term ? { q: term } : {} })
}
</script>

<template>
  <form class="searchbox" @submit.prevent="submit">
    <ElInput v-model="q" :placeholder="t('search_placeholder')" clearable size="large" @keyup.enter="submit">
      <template #prefix><ElIcon><Search /></ElIcon></template>
      <template #append>
        <ElButton type="primary" native-type="submit">{{ t('search') }}</ElButton>
      </template>
    </ElInput>
  </form>
</template>

<style scoped>
.searchbox :deep(.el-input-group__append) {
  background: var(--gm-primary);
  border-color: var(--gm-primary);
}
.searchbox :deep(.el-input-group__append .el-button) {
  color: #fff;
  border: none;
}
</style>
