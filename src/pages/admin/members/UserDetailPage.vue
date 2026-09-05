<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import AdminDetailList from '@/components/admin/AdminDetailList.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { AdminIdentityService } from '@/utils/services/admin-service'
import type { AdminUserDetail, RoleDto } from '@/utils/models/admin-models'
import { confirmX, enableAfter, notifyError, notifyS, promptX } from '@/utils/tools'
import { formatDateTime } from '@/utils/format'

const route = useRoute()
const { t } = useI18n()
const service = new AdminIdentityService()
const id = Number(route.params.id)
const user = ref<AdminUserDetail | null>(null)
const roles = ref<RoleDto[]>([])
const loading = ref(true)
const saving = ref(false)
const form = reactive({ displayName: '', roleId: 0, trustLevel: 0, isActive: true, adminNote: '', emailVerified: false })

async function load() {
  loading.value = true
  try {
    ;[user.value, roles.value] = await Promise.all([service.user(id), service.roles()])
    Object.assign(form, { displayName: user.value.displayName, roleId: user.value.roleId, trustLevel: user.value.trustLevel, isActive: user.value.isActive, adminNote: user.value.adminNote ?? '', emailVerified: user.value.emailVerified })
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(load)
async function save() {
  await enableAfter(saving, async () => {
    try {
      user.value = await service.updateUser(id, { ...form })
      notifyS(t('saved'))
    } catch (err) {
      notifyError(err)
    }
  })
}
async function toggleBan() {
  if (!user.value) return
  if (user.value.isBanned) {
    if (!(await confirmX(t('adm_confirm_unban', { name: user.value.username })))) return
    user.value = await service.ban(id, false).catch((e) => { notifyError(e); return user.value! })
  } else {
    const reason = await promptX(t('adm_ban_reason'))
    if (reason === null) return
    user.value = await service.ban(id, true, reason).catch((e) => { notifyError(e); return user.value! })
  }
}
async function revokeSessions() {
  await service.revokeUserSessions(id).then(() => { notifyS(t('saved')); load() }).catch(notifyError)
}
async function revokeKey(keyId: number) {
  await service.revokeApiKey(keyId).then(load).catch(notifyError)
}
</script>

<template>
  <AdminPage :title="user ? `@${user.username}` : t('app_users')" :loading="loading">
    <template #actions>
      <RouterLink to="/admin/users"><ElButton>← {{ t('app_users') }}</ElButton></RouterLink>
      <RouterLink v-if="user" :to="`/u/${user.username}`" target="_blank"><ElButton>{{ t('view_page') }}</ElButton></RouterLink>
      <ElButton v-if="user" :type="user.isBanned ? 'success' : 'danger'" @click="toggleBan">{{ user.isBanned ? t('unban') : t('ban') }}</ElButton>
      <ElButton @click="revokeSessions">{{ t('sign_out_everywhere') }}</ElButton>
    </template>
    <div v-if="user" class="ud">
      <div class="gm-card ud__box">
        <h4>{{ t('profile') }}</h4>
        <ElAlert v-if="user.isBanned" type="error" :closable="false" :title="`${t('banned')}: ${user.banReason ?? ''}`" style="margin-bottom: 10px" />
        <ElForm label-position="top" class="grid-form">
          <ElFormItem :label="t('display_name')"><ElInput v-model="form.displayName" /></ElFormItem>
          <ElFormItem :label="t('adm_role')"><ElSelect v-model="form.roleId" style="width: 100%"><ElOption v-for="r in roles" :key="r.id" :value="r.id" :label="r.name" /></ElSelect></ElFormItem>
          <ElFormItem :label="t('adm_trust_level')"><ElSelect v-model="form.trustLevel" style="width: 100%"><ElOption :value="0" :label="`0 — ${t('adm_trust_0')}`" /><ElOption :value="1" :label="`1 — ${t('adm_trust_1')}`" /><ElOption :value="2" :label="`2 — ${t('adm_trust_2')}`" /></ElSelect></ElFormItem>
          <ElFormItem :label="t('active')"><ElSwitch v-model="form.isActive" /> <span style="margin-left: 16px">{{ t('adm_email_verified') }}</span> <ElSwitch v-model="form.emailVerified" /></ElFormItem>
          <ElFormItem :label="t('adm_admin_note')" class="full"><ElInput v-model="form.adminNote" type="textarea" :rows="2" /></ElFormItem>
        </ElForm>
        <ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton>
        <AdminDetailList style="margin-top: 14px" :columns="2" :items="[
          { label: t('email'), value: user.email }, { label: t('username'), value: user.username }, { label: 'Google', value: user.hasGoogle, type: 'bool' }, { label: t('member_since'), value: user.createdAt, type: 'datetime' },
          { label: t('last_login'), value: user.lastLoginAt, type: 'datetime' }, { label: t('downloads'), value: user.downloadCount }, { label: t('dash_ratings'), value: user.ratingCount },
          { label: t('adm_reports_against'), value: user.reportCountAgainst }, { label: t('adm_reports_filed'), value: user.reportCountFiled }, { label: t('website'), value: user.website, href: user.website ?? undefined }, { label: t('bio'), value: user.bio },
        ]" />
      </div>
      <div class="gm-card ud__box">
        <h4>{{ t('stat_apps') }} ({{ user.apps.length }})</h4>
        <ElTable :data="user.apps" size="small" class="gm-table">
          <ElTableColumn :label="t('app')" min-width="200"><template #default="{ row }"><RouterLink :to="`/admin/apps/${row.id}`" class="gm-link">{{ row.name }}</RouterLink></template></ElTableColumn>
          <ElTableColumn :label="t('status')" width="120"><template #default="{ row }"><StatusTag :value="row.status" /></template></ElTableColumn>
          <ElTableColumn prop="downloadCount" :label="t('downloads')" width="100" align="right" />
          <ElTableColumn :label="t('rating')" width="90" align="right"><template #default="{ row }">{{ row.ratingCount ? Math.round(row.ratingAvg) : '—' }}</template></ElTableColumn>
        </ElTable>
        <h4 style="margin-top: 16px">{{ t('dash_api_keys') }} ({{ user.apiKeys.length }})</h4>
        <ElTable :data="user.apiKeys" size="small" class="gm-table">
          <ElTableColumn prop="name" :label="t('name')" min-width="140" />
          <ElTableColumn prop="prefix" :label="t('key')" width="130" />
          <ElTableColumn prop="scopes" :label="t('scopes')" width="150" />
          <ElTableColumn prop="requestCount" :label="t('requests')" width="90" align="right" />
          <ElTableColumn width="100" align="right"><template #default="{ row }"><ElButton v-if="!row.revokedAt" size="small" type="danger" text @click="revokeKey(row.id)">{{ t('revoke') }}</ElButton><ElTag v-else size="small" type="info">{{ t('revoked') }}</ElTag></template></ElTableColumn>
        </ElTable>
        <h4 style="margin-top: 16px">{{ t('sessions') }} ({{ user.sessions.length }})</h4>
        <ElTable :data="user.sessions" size="small" class="gm-table">
          <ElTableColumn :label="t('created')" width="150"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></ElTableColumn>
          <ElTableColumn prop="ip" label="IP" width="130" />
          <ElTableColumn prop="userAgent" :label="t('device')" show-overflow-tooltip />
        </ElTable>
      </div>
    </div>
  </AdminPage>
</template>

<style scoped>
.ud { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.ud__box { padding: 14px 16px; }
.ud__box h4 { margin: 0 0 10px; }
@media (max-width: 1100px) { .ud { grid-template-columns: 1fr; } }
</style>
