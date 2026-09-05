<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDataTable from '@/components/admin/AdminDataTable.vue'
import { AdminIdentityService } from '@/utils/services/admin-service'
import type { AdminUserRow, RoleDto } from '@/utils/models/admin-models'
import type { Paged } from '@/utils/models/common-models'
import { confirmX, notifyError, notifyS } from '@/utils/tools'
import { formatDate, formatDateTime } from '@/utils/format'

const { t } = useI18n()
const service = new AdminIdentityService()
const tableRef = ref<InstanceType<typeof AdminDataTable>>()
const roles = ref<RoleDto[]>([])
const filters = reactive({ role: '', status: '' })
const params = computed(() => ({ role: filters.role || null, status: filters.status || null }))
function fetchPage(query: Record<string, unknown>) {
  return service.users(query) as unknown as Promise<Paged<Record<string, unknown>>>
}
onMounted(async () => { roles.value = await service.roles().catch(() => []) })
async function toggleBan(row: AdminUserRow) {
  if (!(await confirmX(row.isBanned ? t('adm_confirm_unban', { name: row.username }) : t('adm_confirm_ban', { name: row.username })))) return
  await service.ban(row.id, !row.isBanned).then(() => { notifyS(t('saved')); tableRef.value?.reload() }).catch(notifyError)
}
</script>

<template>
  <AdminPage :title="t('app_users')" :subtitle="t('adm_users_subtitle')">
    <AdminDataTable ref="tableRef" :fetch="fetchPage" :params="params" :default-sort="{ prop: 'created', order: 'descending' }">
      <template #filters>
        <ElSelect v-model="filters.role" :placeholder="t('adm_role')" clearable class="f-sm"><ElOption v-for="r in roles" :key="r.id" :value="r.name" :label="r.name" /></ElSelect>
        <ElSelect v-model="filters.status" :placeholder="t('status')" clearable class="f-sm"><ElOption value="banned" :label="t('banned')" /><ElOption value="inactive" :label="t('inactive')" /><ElOption value="unverified" :label="t('adm_unverified')" /></ElSelect>
      </template>
      <ElTableColumn prop="username" :label="t('username')" min-width="180" sortable="custom"><template #default="{ row }"><RouterLink :to="`/admin/users/${row.id}`" class="gm-link"><strong>{{ row.username }}</strong></RouterLink><div class="sub">{{ row.displayName }} · {{ row.email }}{{ row.emailVerified ? ' ✔' : '' }}</div></template></ElTableColumn>
      <ElTableColumn :label="t('adm_role')" width="110"><template #default="{ row }"><ElTag :type="row.role === 'Admin' ? 'danger' : row.role === 'Moderator' ? 'warning' : 'info'" size="small">{{ row.role }}</ElTag></template></ElTableColumn>
      <ElTableColumn :label="t('adm_trust_level')" prop="trustLevel" width="80" align="center" />
      <ElTableColumn :label="t('stat_apps')" prop="appCount" width="70" align="center" />
      <ElTableColumn :label="t('dash_ratings')" prop="ratingCount" width="80" align="center" />
      <ElTableColumn :label="t('downloads')" prop="downloadCount" width="90" align="center" />
      <ElTableColumn label="Google" width="80" align="center"><template #default="{ row }">{{ row.hasGoogle ? '✔' : '—' }}</template></ElTableColumn>
      <ElTableColumn prop="lastLogin" :label="t('last_login')" width="150" sortable="custom"><template #default="{ row }">{{ row.lastLoginAt ? formatDateTime(row.lastLoginAt) : '—' }}</template></ElTableColumn>
      <ElTableColumn prop="created" :label="t('created')" width="110" sortable="custom"><template #default="{ row }">{{ formatDate(row.createdAt) }}</template></ElTableColumn>
      <ElTableColumn :label="t('status')" width="100"><template #default="{ row }"><ElTag v-if="row.isBanned" type="danger" size="small">{{ t('banned') }}</ElTag><ElTag v-else-if="!row.isActive" type="info" size="small">{{ t('inactive') }}</ElTag><ElTag v-else type="success" size="small">{{ t('active') }}</ElTag></template></ElTableColumn>
      <ElTableColumn width="170" align="right" fixed="right"><template #default="{ row }"><ElButton size="small" type="primary" @click="$router.push(`/admin/users/${row.id}`)">{{ t('detail') }}</ElButton><ElButton size="small" :type="row.isBanned ? 'success' : 'danger'" plain @click="toggleBan(row as AdminUserRow)">{{ row.isBanned ? t('unban') : t('ban') }}</ElButton></template></ElTableColumn>
    </AdminDataTable>
  </AdminPage>
</template>
