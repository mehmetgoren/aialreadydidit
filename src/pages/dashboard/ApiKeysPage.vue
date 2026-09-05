<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { ApiKeyDto } from '@/utils/models/dashboard-models'
import { DashboardService } from '@/utils/services/dashboard-service'
import { useCommonStore } from '@/stores/common-store'
import { useSiteStore } from '@/stores/site-store'
import { confirmX, copyText, enableAfter, notifyError } from '@/utils/tools'
import { formatDateTime, formatNumber } from '@/utils/format'

const { t } = useI18n()
const common = useCommonStore()
const site = useSiteStore()
const service = new DashboardService()
const keys = ref<ApiKeyDto[]>([])
const loading = ref(true)
const saving = ref(false)
const dialog = ref(false)
const created = ref<ApiKeyDto | null>(null)
const form = reactive({ name: '', scopes: ['read', 'download'], expiresInDays: null as number | null })
const mcpUrl = computed(() => site.config?.mcpPublicUrl ?? '')

async function load() {
  loading.value = true
  try {
    keys.value = await service.apiKeys()
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(() => {
  common.setPageTitle(t('dash_api_keys'))
  site.ensureLoaded()
  load()
})

async function create() {
  await enableAfter(saving, async () => {
    try {
      created.value = await service.createApiKey({ name: form.name, scopes: form.scopes, expiresInDays: form.expiresInDays })
      dialog.value = false
      form.name = ''
      load()
    } catch (err) {
      notifyError(err)
    }
  })
}
async function revoke(k: ApiKeyDto) {
  if (!(await confirmX(t('confirm_revoke_key', { name: k.name })))) return
  try {
    await service.revokeApiKey(k.id)
    load()
  } catch (err) {
    notifyError(err)
  }
}
</script>

<template>
  <div>
    <div class="gm-section__head"><h2 class="gm-title">{{ t('dash_api_keys') }}</h2><ElButton type="primary" @click="dialog = true"><ElIcon><Plus /></ElIcon>{{ t('new_api_key') }}</ElButton></div>
    <p class="gm-muted">{{ t('api_keys_intro') }} <RouterLink to="/for-agents" class="gm-link">{{ t('for_agents') }} →</RouterLink></p>

    <ElAlert v-if="created" type="success" :closable="false" show-icon class="key-created">
      <template #title>{{ t('key_created_title') }}</template>
      <p>{{ t('key_created_text') }}</p>
      <div class="key-created__secret"><code>{{ created.secret }}</code><ElButton size="small" @click="copyText(created.secret!)">{{ t('copy') }}</ElButton></div>
      <pre class="key-created__snippet">claude mcp add --transport http ai-already-did-it {{ mcpUrl }} --header "X-Api-Key: {{ created.secret }}"</pre>
      <ElButton size="small" text @click="created = null">{{ t('dismiss') }}</ElButton>
    </ElAlert>

    <ElTable v-loading="loading" :data="keys" class="gm-table" stripe>
      <ElTableColumn :label="t('name')" prop="name" min-width="140" />
      <ElTableColumn :label="t('key')" width="150"><template #default="{ row }"><code>{{ row.prefix }}…</code></template></ElTableColumn>
      <ElTableColumn :label="t('scopes')" prop="scopes" width="170" />
      <ElTableColumn :label="t('requests')" width="100" align="right"><template #default="{ row }">{{ formatNumber(row.requestCount) }}</template></ElTableColumn>
      <ElTableColumn :label="t('downloads')" width="100" align="right" prop="downloadCount" />
      <ElTableColumn :label="t('last_used')" width="150"><template #default="{ row }">{{ row.lastUsedAt ? formatDateTime(row.lastUsedAt) : '—' }}</template></ElTableColumn>
      <ElTableColumn :label="t('status')" width="110"><template #default="{ row }"><ElTag :type="row.revokedAt ? 'info' : 'success'" size="small">{{ row.revokedAt ? t('revoked') : t('active') }}</ElTag></template></ElTableColumn>
      <ElTableColumn width="100" align="right"><template #default="{ row }"><ElButton v-if="!row.revokedAt" size="small" type="danger" text @click="revoke(row as ApiKeyDto)">{{ t('revoke') }}</ElButton></template></ElTableColumn>
    </ElTable>

    <ElDialog v-model="dialog" :title="t('new_api_key')" width="480px">
      <ElForm label-position="top">
        <ElFormItem :label="t('name')"><ElInput v-model="form.name" maxlength="80" placeholder="claude-code" /></ElFormItem>
        <ElFormItem :label="t('scopes')">
          <ElCheckboxGroup v-model="form.scopes">
            <ElCheckbox value="read" disabled>read</ElCheckbox>
            <ElCheckbox value="download">download</ElCheckbox>
            <ElCheckbox value="submit">submit</ElCheckbox>
          </ElCheckboxGroup>
        </ElFormItem>
        <ElFormItem :label="t('expires_in_days')"><ElInputNumber v-model="form.expiresInDays" :min="1" :max="3650" /></ElFormItem>
      </ElForm>
      <template #footer><ElButton @click="dialog = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="saving" :disabled="!form.name.trim()" @click="create">{{ t('create') }}</ElButton></template>
    </ElDialog>
  </div>
</template>

<style scoped>
.key-created { margin: 12px 0; }
.key-created__secret { display: flex; gap: 8px; align-items: center; margin: 8px 0; }
.key-created__secret code { font-size: 13px; word-break: break-all; }
.key-created__snippet { font-size: 12px; white-space: pre-wrap; word-break: break-all; background: var(--gm-page-bg); padding: 8px; border-radius: 6px; }
</style>
