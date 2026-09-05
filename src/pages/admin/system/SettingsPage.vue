<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminSystemService } from '@/utils/services/admin-service'
import type { SiteSettingDto } from '@/utils/models/admin-models'
import { enableAfter, notifyError, notifyS } from '@/utils/tools'

const { t } = useI18n()
const service = new AdminSystemService()
const rows = ref<SiteSettingDto[]>([])
const values = ref<Record<string, string | null>>({})
const loading = ref(true)
const saving = ref(false)
const groups = computed(() => [...new Set(rows.value.map((r) => r.group))])

onMounted(async () => {
  try {
    rows.value = await service.settings()
    values.value = Object.fromEntries(rows.value.map((r) => [r.key, r.value]))
  } catch (err) { notifyError(err) } finally { loading.value = false }
})
async function save() {
  await enableAfter(saving, async () => {
    try { rows.value = await service.saveSettings(values.value); notifyS(t('saved')) } catch (err) { notifyError(err) }
  })
}
</script>

<template>
  <AdminPage :title="t('adm_system_settings')" :subtitle="t('adm_settings_subtitle')" :loading="loading">
    <template #actions><ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton></template>
    <ElTabs>
      <ElTabPane v-for="g in groups" :key="g" :label="t(`adm_settings_group_${g}`)">
        <ElForm label-position="top" class="settings-form">
          <ElFormItem v-for="s in rows.filter((r) => r.group === g)" :key="s.key" :label="s.key">
            <ElSwitch v-if="s.valueType === 'bool'" :model-value="values[s.key] === 'true'" @update:model-value="values[s.key] = String($event)" />
            <ElInputNumber v-else-if="s.valueType === 'int'" :model-value="Number(values[s.key] ?? 0)" @update:model-value="values[s.key] = String($event ?? 0)" />
            <ElInputNumber v-else-if="s.valueType === 'decimal'" :model-value="Number(values[s.key] ?? 0)" :precision="4" :step="0.1" @update:model-value="values[s.key] = String($event ?? 0)" />
            <ElInput v-else-if="s.valueType === 'text'" v-model="values[s.key]" type="textarea" :rows="3" />
            <ElInput v-else v-model="values[s.key]" />
            <div class="sub">{{ s.description }}</div>
          </ElFormItem>
        </ElForm>
      </ElTabPane>
    </ElTabs>
  </AdminPage>
</template>

<style scoped>
.settings-form { max-width: 720px; }
</style>
