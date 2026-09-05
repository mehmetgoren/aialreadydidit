<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminIdentityService } from '@/utils/services/admin-service'
import type { RoleDto, RoleMenuDto } from '@/utils/models/admin-models'
import { enableAfter, notifyError, notifyS } from '@/utils/tools'

const route = useRoute()
const { t } = useI18n()
const service = new AdminIdentityService()
const roles = ref<RoleDto[]>([])
const roleId = ref<number | null>(route.query.role ? Number(route.query.role) : null)
const rows = ref<RoleMenuDto[]>([])
const loading = ref(false)
const saving = ref(false)
onMounted(async () => { roles.value = await service.roles().catch(() => []); roleId.value ??= roles.value[0]?.id ?? null })
watch(roleId, async (id) => { if (!id) return; loading.value = true; rows.value = await service.roleMenus(id).catch((e) => { notifyError(e); return [] }); loading.value = false }, { immediate: true })
async function save() {
  if (!roleId.value) return
  await enableAfter(saving, async () => { try { await service.saveRoleMenus(roleId.value!, rows.value.filter((r) => r.hasAccess).map((r) => r.menuId)); notifyS(t('saved')) } catch (err) { notifyError(err) } })
}
</script>

<template>
  <AdminPage :title="t('adm_identity_role_menus')" :subtitle="t('adm_role_menus_subtitle')" :loading="loading">
    <template #actions><ElSelect v-model="roleId" style="width: 200px"><ElOption v-for="r in roles" :key="r.id" :value="r.id" :label="r.name" /></ElSelect><ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton></template>
    <ElTable :data="rows" size="small" class="gm-table">
      <ElTableColumn :label="t('adm_identity_menus')" min-width="240"><template #default="{ row }"><span :style="{ paddingLeft: row.parentId ? '24px' : '0' }">{{ t(row.menuName) }}</span> <code class="sub">{{ row.route }}</code></template></ElTableColumn>
      <ElTableColumn :label="t('adm_access')" width="100" align="center"><template #default="{ row }"><ElSwitch v-model="row.hasAccess" size="small" /></template></ElTableColumn>
    </ElTable>
  </AdminPage>
</template>
