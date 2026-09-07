<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminIdentityService } from '@/utils/services/admin-service'
import type { ControllerActionsDto, RoleDto } from '@/utils/models/admin-models'
import { enableAfter, notifyError, notifyS } from '@/utils/tools'

const route = useRoute()
const { t } = useI18n()
const service = new AdminIdentityService()
const roles = ref<RoleDto[]>([])
const roleId = ref<number | null>(route.query.role ? Number(route.query.role) : null)
const controllers = ref<ControllerActionsDto[]>([])
const selected = ref<Set<string>>(new Set())
const loading = ref(false)
const saving = ref(false)
const role = computed(() => roles.value.find((r) => r.id === roleId.value))

onMounted(async () => { [roles.value, controllers.value] = await Promise.all([service.roles().catch(() => []), service.controllerActions().catch(() => [])]); roleId.value ??= roles.value[0]?.id ?? null })
watch(roleId, async (id) => { if (!id) return; loading.value = true; const a = await service.roleActions(id).catch((e) => { notifyError(e); return [] }); selected.value = new Set(a.map((x) => `${x.controller}|${x.action}`)); loading.value = false }, { immediate: true })
function toggle(controller: string, action: string) { const k = `${controller}|${action}`; if (selected.value.has(k)) selected.value.delete(k); else selected.value.add(k); selected.value = new Set(selected.value) }
async function save() {
  if (!roleId.value) return
  await enableAfter(saving, async () => { try { await service.saveRoleActions(roleId.value!, [...selected.value].map((k) => { const [controller, action] = k.split('|'); return { controller: controller!, action: action! } })); notifyS(t('saved')) } catch (err) { notifyError(err) } })
}
</script>

<template>
  <AdminPage :title="t('adm_identity_role_actions')" :subtitle="t('adm_role_actions_subtitle')" :loading="loading">
    <template #actions><ElSelect v-model="roleId" style="width: 200px"><ElOption v-for="r in roles" :key="r.id" :value="r.id" :label="r.name" /></ElSelect><ElButton type="primary" :loading="saving" :disabled="role?.isAdmin" @click="save">{{ t('save') }}</ElButton></template>
    <ElAlert v-if="role?.isAdmin" type="info" :closable="false" :title="t('adm_admin_has_all')" style="margin-bottom: 12px" />
    <div class="ra">
      <div v-for="c in controllers" :key="c.controller" class="gm-card ra__box">
        <strong>{{ c.controller }}</strong>
        <div class="ra__actions">
          <ElCheckbox v-for="a in c.actions" :key="a" :model-value="selected.has(`${c.controller}|${a}`)" :disabled="role?.isAdmin" @change="toggle(c.controller, a)">{{ a }}</ElCheckbox>
        </div>
      </div>
    </div>
  </AdminPage>
</template>

<style scoped>
.ra { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 10px; }
.ra__box { padding: 10px 12px; }
.ra__actions { display: flex; flex-wrap: wrap; gap: 0 12px; }
</style>
