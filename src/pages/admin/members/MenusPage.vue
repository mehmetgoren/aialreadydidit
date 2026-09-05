<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminIdentityService } from '@/utils/services/admin-service'
import type { MenuDto } from '@/utils/models/admin-models'
import { confirmX, enableAfter, notifyError, notifyS } from '@/utils/tools'

const { t } = useI18n()
const service = new AdminIdentityService()
const tree = ref<MenuDto[]>([])
const flat = ref<MenuDto[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const form = reactive<Partial<MenuDto>>({})
async function load() {
  loading.value = true
  try {
    tree.value = await service.menus()
    const out: MenuDto[] = []
    const walk = (n: MenuDto[]) => n.forEach((m) => { out.push(m); walk(m.children) })
    walk(tree.value)
    flat.value = out
  } catch (err) { notifyError(err) } finally { loading.value = false }
}
onMounted(load)
function open(m?: MenuDto, parent?: MenuDto) { Object.assign(form, m ? { ...m } : { id: undefined, name: '', route: '', description: '', orderNum: flat.value.length + 1, parentId: parent?.id ?? null, visible: true, icon: '' }); dialog.value = true }
async function save() {
  await enableAfter(saving, async () => { try { await service.saveMenu(form.id ?? null, form); notifyS(t('saved')); dialog.value = false; load() } catch (err) { notifyError(err) } })
}
async function remove(m: MenuDto) { if (!(await confirmX(t('adm_confirm_delete')))) return; await service.deleteMenu(m.id).then(load).catch(notifyError) }
</script>

<template>
  <AdminPage :title="t('adm_identity_menus')" :subtitle="t('adm_menus_subtitle')" :loading="loading">
    <template #actions><ElButton type="primary" @click="open()"><ElIcon><Plus /></ElIcon>{{ t('create') }}</ElButton></template>
    <ElTree :data="tree" node-key="id" :props="{ children: 'children', label: 'name' }" default-expand-all>
      <template #default="{ data }">
        <div class="menu-node">
          <ElIcon v-if="data.icon"><component :is="data.icon" /></ElIcon>
          <span>{{ t(data.name) }} <code class="sub">{{ data.name }}</code></span>
          <span class="sub">{{ data.route }}</span>
          <ElTag v-if="!data.visible" size="small" type="info">{{ t('hidden') }}</ElTag>
          <span class="menu-node__actions"><ElButton size="small" text @click.stop="open(undefined, data)">+ {{ t('adm_sub') }}</ElButton><ElButton size="small" text @click.stop="open(data)">{{ t('edit') }}</ElButton><ElButton size="small" text type="danger" @click.stop="remove(data)">{{ t('delete') }}</ElButton></span>
        </div>
      </template>
    </ElTree>
    <ElDialog v-model="dialog" :title="form.id ? t('edit') : t('create')" width="520px">
      <ElForm label-position="top" class="grid-form">
        <ElFormItem :label="t('adm_i18n_key')"><ElInput v-model="form.name" /></ElFormItem>
        <ElFormItem :label="t('adm_route')"><ElInput v-model="form.route" /></ElFormItem>
        <ElFormItem :label="t('adm_icon')"><ElInput v-model="form.icon" /></ElFormItem>
        <ElFormItem :label="t('adm_sort')"><ElInputNumber v-model="form.orderNum" /></ElFormItem>
        <ElFormItem :label="t('adm_parent')"><ElSelect v-model="form.parentId" clearable style="width: 100%"><ElOption v-for="m in flat.filter((x) => x.id !== form.id)" :key="m.id" :value="m.id" :label="t(m.name)" /></ElSelect></ElFormItem>
        <ElFormItem :label="t('visible')"><ElSwitch v-model="form.visible" /></ElFormItem>
      </ElForm>
      <template #footer><ElButton @click="dialog = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton></template>
    </ElDialog>
  </AdminPage>
</template>

<style scoped>
.menu-node { display: flex; align-items: center; gap: 8px; width: 100%; }
.menu-node__actions { margin-left: auto; opacity: 0; }
.menu-node:hover .menu-node__actions { opacity: 1; }
</style>
