<script setup lang="ts">
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
</script>

<template>
  <div class="nf">
    <div class="nf__code">404</div>
    <p>{{ t('not_found_text') }}</p>
    <RouterLink to="/"><ElButton type="primary">{{ t('home') }}</ElButton></RouterLink>
  </div>
</template>

<style scoped>
.nf {
  min-height: 60vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
}
.nf__code {
  font-size: 72px;
  font-weight: 800;
  color: var(--gm-primary);
}
</style>
