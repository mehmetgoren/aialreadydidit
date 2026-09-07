<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminCatalogService } from '@/utils/services/admin-service'
import type { AdminCategoryNode } from '@/utils/models/admin-models'
import type Node from 'element-plus/es/components/tree/src/model/node'
import { confirmX, enableAfter, notifyError, notifyS, promptX } from '@/utils/tools'

/** Category tree (3 levels) with create / edit / move / merge / approve LLM-proposed. */
const { t } = useI18n()
const service = new AdminCatalogService()
const tree = ref<AdminCategoryNode[]>([])
const flat = ref<AdminCategoryNode[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const form = reactive({ id: null as number | null, nameEn: '', nameTr: '', slug: '', description: '', icon: '', parentId: null as number | null, isActive: true })

async function load() {
  loading.value = true
  try {
    tree.value = await service.categoryTree()
    const out: AdminCategoryNode[] = []
    const walk = (nodes: AdminCategoryNode[]) => nodes.forEach((n) => { out.push(n); walk(n.children) })
    walk(tree.value)
    flat.value = out
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(load)

function openCreate(parent: AdminCategoryNode | null) {
  Object.assign(form, { id: null, nameEn: '', nameTr: '', slug: '', description: '', icon: '', parentId: parent?.id ?? null, isActive: true })
  dialog.value = true
}
function openEdit(n: AdminCategoryNode) {
  Object.assign(form, { id: n.id, nameEn: n.nameEn, nameTr: n.nameTr, slug: n.slug, description: n.description ?? '', icon: n.icon ?? '', parentId: n.parentId, isActive: n.isActive })
  dialog.value = true
}
async function save() {
  await enableAfter(saving, async () => {
    try {
      const req = { nameEn: form.nameEn, nameTr: form.nameTr || null, slug: form.slug || null, description: form.description || null, icon: form.icon || null, parentId: form.parentId, isActive: form.isActive }
      if (form.id) await service.updateCategory(form.id, req)
      else await service.createCategory(req)
      notifyS(t('saved'))
      dialog.value = false
      load()
    } catch (err) {
      notifyError(err)
    }
  })
}
async function approve(n: AdminCategoryNode) {
  await service.approveCategory(n.id).then(load).catch(notifyError)
}
async function remove(n: AdminCategoryNode) {
  if (!(await confirmX(t('adm_confirm_delete')))) return
  await service.deleteCategory(n.id).then(load).catch(notifyError)
}
async function merge(n: AdminCategoryNode) {
  const target = await promptX(t('adm_merge_into_id'))
  if (!target) return
  await service.mergeCategory(n.id, Number(target)).then(load).catch(notifyError)
}
async function move(n: AdminCategoryNode) {
  const target = await promptX(t('adm_move_under_id'), undefined, n.parentId ? String(n.parentId) : '')
  if (target === null) return
  await service.moveCategory(n.id, target ? Number(target) : null).then(load).catch(notifyError)
}
function allowDrop(_d: Node, drop: Node, type: 'prev' | 'inner' | 'next') {
  return !(type === 'inner' && (drop.data as AdminCategoryNode).level >= 3)
}
async function onDrop(draggingNode: Node, dropNode: Node, type: 'before' | 'after' | 'inner') {
  const dragging = { data: draggingNode.data as AdminCategoryNode }
  const drop = { data: dropNode.data as AdminCategoryNode }
  try {
    if (type === 'inner') await service.moveCategory(dragging.data.id, drop.data.id)
    else {
      if (dragging.data.parentId !== drop.data.parentId) await service.moveCategory(dragging.data.id, drop.data.parentId)
      const siblings = flat.value.filter((c) => c.parentId === drop.data.parentId && c.id !== dragging.data.id).map((c) => c.id)
      const idx = siblings.indexOf(drop.data.id)
      siblings.splice(type === 'before' ? idx : idx + 1, 0, dragging.data.id)
      await service.reorderCategories(siblings)
    }
  } catch (err) {
    notifyError(err)
  }
  load()
}
</script>

<template>
  <AdminPage :title="t('admin_categories')" :subtitle="t('adm_categories_subtitle')" :loading="loading">
    <template #actions><ElButton type="primary" @click="openCreate(null)"><ElIcon><Plus /></ElIcon>{{ t('adm_new_root') }}</ElButton></template>
    <ElTree :data="tree" node-key="id" :props="{ children: 'children', label: 'nameEn' }" default-expand-all draggable :allow-drop="allowDrop" @node-drop="onDrop">
      <template #default="{ data }">
        <div class="cat-node">
          <ElIcon v-if="data.icon"><component :is="data.icon" /></ElIcon>
          <span :class="{ 'is-inactive': !data.isActive }">{{ data.nameEn }} <small class="gm-muted">/ {{ data.nameTr }}</small></span>
          <code class="sub">{{ data.slug }}</code>
          <span class="sub">#{{ data.id }} · {{ data.totalAppCount }}</span>
          <ElTag v-if="data.isLlmProposed" type="warning" size="small">🤖 {{ t('adm_proposed_new') }}</ElTag>
          <ElTag v-if="!data.isActive && !data.isLlmProposed" type="info" size="small">{{ t('inactive') }}</ElTag>
          <span class="cat-node__actions">
            <ElButton v-if="data.isLlmProposed" size="small" type="success" text @click.stop="approve(data)">{{ t('adm_approve') }}</ElButton>
            <ElButton v-if="data.level < 3" size="small" text @click.stop="openCreate(data)">+ {{ t('adm_sub') }}</ElButton>
            <ElButton size="small" text @click.stop="openEdit(data)">{{ t('edit') }}</ElButton>
            <ElButton size="small" text @click.stop="move(data)">{{ t('adm_move') }}</ElButton>
            <ElButton size="small" text @click.stop="merge(data)">{{ t('adm_merge') }}</ElButton>
            <ElButton size="small" text type="danger" @click.stop="remove(data)">{{ t('delete') }}</ElButton>
          </span>
        </div>
      </template>
    </ElTree>
    <ElDialog v-model="dialog" :title="form.id ? t('edit') : t('create')" width="560px">
      <ElForm label-position="top" class="grid-form">
        <ElFormItem :label="t('adm_name_en')"><ElInput v-model="form.nameEn" /></ElFormItem>
        <ElFormItem :label="t('adm_name_tr')"><ElInput v-model="form.nameTr" /></ElFormItem>
        <ElFormItem label="Slug"><ElInput v-model="form.slug" /></ElFormItem>
        <ElFormItem :label="t('adm_icon')"><ElInput v-model="form.icon" placeholder="Element Plus icon name" /></ElFormItem>
        <ElFormItem :label="t('adm_parent')"><ElSelect v-model="form.parentId" clearable filterable style="width: 100%"><ElOption v-for="c in flat.filter((x) => x.level < 3 && x.id !== form.id)" :key="c.id" :value="c.id" :label="`${'— '.repeat(c.level - 1)}${c.nameEn}`" /></ElSelect></ElFormItem>
        <ElFormItem :label="t('active')"><ElSwitch v-model="form.isActive" /></ElFormItem>
        <ElFormItem :label="t('description')" class="full"><ElInput v-model="form.description" type="textarea" :rows="2" /></ElFormItem>
      </ElForm>
      <template #footer><ElButton @click="dialog = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="saving" :disabled="!form.nameEn" @click="save">{{ t('save') }}</ElButton></template>
    </ElDialog>
  </AdminPage>
</template>

<style scoped>
.cat-node { display: flex; align-items: center; gap: 8px; width: 100%; padding-right: 8px; }
.cat-node .is-inactive { opacity: 0.55; }
.cat-node__actions { margin-left: auto; opacity: 0; }
.cat-node:hover .cat-node__actions { opacity: 1; }
</style>
