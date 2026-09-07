<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminSystemService } from '@/utils/services/admin-service'
import type { SystemHealth } from '@/utils/models/admin-models'
import { notifyError, notifyS } from '@/utils/tools'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
const data = ref<SystemHealth | null>(null)
const loading = ref(true)
async function load() { loading.value = true; data.value = await new AdminSystemService().health().catch((e) => { notifyError(e); return null }); loading.value = false }
const sending = ref(false)
async function testEmail() {
  sending.value = true
  try { notifyS(await new AdminSystemService().testEmail()) } catch (e) { notifyError(e) } finally { sending.value = false }
}
onMounted(load)
</script>

<template>
  <AdminPage :title="t('adm_system_health')" :loading="loading">
    <template #actions>
      <ElButton :loading="sending" @click="testEmail"><ElIcon><Message /></ElIcon>{{ t('adm_send_test_email') }}</ElButton>
      <ElButton @click="load"><ElIcon><Refresh /></ElIcon>{{ t('refresh') }}</ElButton>
    </template>
    <template v-if="data">
      <div class="hl__checks">
        <div v-for="c in data.checks" :key="c.name" class="gm-card hl__check" :class="c.ok ? 'is-ok' : 'is-bad'">
          <div class="hl__dot" />
          <div class="hl__body"><strong>{{ c.name }}</strong><div class="sub">{{ c.detail }}</div></div>
          <span v-if="c.latencyMs != null" class="sub">{{ c.latencyMs }} ms</span>
        </div>
      </div>
      <p class="sub">{{ t('adm_version') }} {{ data.version }} · {{ data.environment }} · {{ t('adm_started') }} {{ formatDateTime(data.startedAt) }}</p>
      <h4>{{ t('adm_effective_config') }}</h4>
      <ElTable :data="Object.entries(data.config).map(([k, v]) => ({ k, v }))" size="small" class="gm-table"><ElTableColumn prop="k" :label="t('key')" width="280" /><ElTableColumn prop="v" :label="t('adm_value')" /></ElTable>
    </template>
  </AdminPage>
</template>

<style scoped>
.hl__checks { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 10px; margin-bottom: 12px; }
.hl__check { display: flex; align-items: center; gap: 10px; padding: 12px 14px; }
.hl__dot { width: 12px; height: 12px; border-radius: 50%; background: var(--gm-green); flex-shrink: 0; }
.is-bad .hl__dot { background: var(--el-color-danger); }
.hl__body { flex: 1; min-width: 0; }
</style>
