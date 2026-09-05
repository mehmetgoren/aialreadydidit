<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminIdentityService } from '@/utils/services/admin-service'
import type { RoleDto } from '@/utils/models/admin-models'
import { confirmX, enableAfter, notifyError, notifyS } from '@/utils/tools'

const { t } = useI18n()
const service = new AdminIdentityService()
const rows = ref<RoleDto[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const form = reactive({ id: null as number | null, name: '', isAdmin: false })
async function load() {
  loading.value = true
  rows.value = await service.roles().catch((e) => { notifyError(e); return [] })
  loading.value = false
}
onMounted(load)
function open(r?: RoleDto) { Object.assign(form, r ? { id: r.id, name: r.name, isAdmin: r.isAdmin } : { id: null, name: '', isAdmin: false }); dialog.value = true }
async function save() {
  await enableAfter(saving, async () => {
    try { await service.saveRole(form.id, form.name, form.isAdmin); notifyS(t('saved')); dialog.value = false; load() } catch (err) { notifyError(err) }
  })
}
async function remove(r: RoleDto) {
  if (!(await confirmX(t('adm_confirm_delete')))) return
  await service.deleteRole(r.id).then(load).catch(notifyError)
}
</script>

<template>
  <AdminPage :title="t('adm_identity_roles')" :subtitle="t('adm_roles_subtitle')" :loading="loading">
    <template #actions><ElButton type="primary" @click="open()"><ElIcon><Plus /></ElIcon>{{ t('create') }}</ElButton></template>
    <ElTable :data="rows" class="gm-table" size="small" stripe>
      <ElTableColumn prop="id" label="#" width="70" />
      <ElTableColumn prop="name" :label="t('name')" min-width="200" />
      <ElTableColumn :label="t('adm_is_admin')" width="120" align="center"><template #default="{ row }">{{ row.isAdmin ? '✔' : '—' }}</template></ElTableColumn>
      <ElTableColumn prop="userCount" :label="t('app_users')" width="100" align="right" />
      <ElTableColumn width="280" align="right"><template #default="{ row }"><RouterLink :to="`/admin/role-menus?role=${row.id}`"><ElButton size="small">{{ t('adm_identity_role_menus') }}</ElButton></RouterLink><RouterLink :to="`/admin/role-actions?role=${row.id}`"><ElButton size="small">{{ t('adm_identity_role_actions') }}</ElButton></RouterLink><ElButton size="small" @click="open(row as RoleDto)">{{ t('edit') }}</ElButton><ElButton size="small" type="danger" text @click="remove(row as RoleDto)">{{ t('delete') }}</ElButton></template></ElTableColumn>
    </ElTable>
    <ElDialog v-model="dialog" :title="form.id ? t('edit') : t('create')" width="420px">
      <ElForm label-position="top"><ElFormItem :label="t('name')"><ElInput v-model="form.name" /></ElFormItem><ElFormItem :label="t('adm_is_admin')"><ElSwitch v-model="form.isAdmin" /></ElFormItem></ElForm>
      <template #footer><ElButton @click="dialog = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton></template>
    </ElDialog>
  </AdminPage>
</template>
