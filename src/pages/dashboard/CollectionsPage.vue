<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { CollectionDto } from '@/utils/models/catalog-models'
import { DashboardService } from '@/utils/services/dashboard-service'
import { useUserStore } from '@/stores/user-store'
import { useCommonStore } from '@/stores/common-store'
import AppGrid from '@/components/catalog/AppGrid.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import { confirmX, copyText, enableAfter, notifyError, notifyS } from '@/utils/tools'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const user = useUserStore()
const common = useCommonStore()
const service = new DashboardService()
const list = ref<CollectionDto[]>([])
const current = ref<CollectionDto | null>(null)
const loading = ref(true)
const saving = ref(false)
const dialog = ref(false)
const form = reactive({ id: null as number | null, name: '', description: '', isPublic: false })
const id = computed(() => (route.params.id ? Number(route.params.id) : null))

async function load() {
  loading.value = true
  try {
    if (id.value) current.value = await service.collection(id.value)
    else {
      current.value = null
      list.value = await service.collections()
    }
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(() => {
  common.setPageTitle(t('dash_collections'))
  load()
})
watch(id, load)

function openCreate() {
  Object.assign(form, { id: null, name: '', description: '', isPublic: false })
  dialog.value = true
}
function openEdit(c: CollectionDto) {
  Object.assign(form, { id: c.id, name: c.name, description: c.description ?? '', isPublic: c.isPublic })
  dialog.value = true
}
async function save() {
  await enableAfter(saving, async () => {
    try {
      const req = { name: form.name, description: form.description || null, isPublic: form.isPublic }
      if (form.id) current.value = await service.updateCollection(form.id, req)
      else await service.createCollection(req)
      dialog.value = false
      notifyS(t('saved'))
      load()
    } catch (err) {
      notifyError(err)
    }
  })
}
async function remove(c: CollectionDto) {
  if (!(await confirmX(t('confirm_delete_collection', { name: c.name })))) return
  try {
    await service.deleteCollection(c.id)
    router.push('/dashboard/collections')
    load()
  } catch (err) {
    notifyError(err)
  }
}
async function removeItem(appId: number) {
  if (!current.value) return
  try {
    current.value = await service.removeFromCollection(current.value.id, appId)
  } catch (err) {
    notifyError(err)
  }
}
function share(c: CollectionDto) {
  copyText(`${location.origin}/u/${user.me?.username}/collections/${c.slug}`)
}
</script>

<template>
  <div v-loading="loading">
    <template v-if="current">
      <div class="gm-section__head">
        <div><RouterLink to="/dashboard/collections" class="gm-link">← {{ t('dash_collections') }}</RouterLink><h2 class="gm-title" style="margin-top: 6px">{{ current.name }} <ElTag v-if="current.isPublic" size="small" type="success">{{ t('public') }}</ElTag></h2><p class="gm-muted">{{ current.description }}</p></div>
        <div>
          <ElButton @click="openEdit(current)">{{ t('edit') }}</ElButton>
          <ElButton v-if="current.isPublic" @click="share(current)">{{ t('share') }}</ElButton>
          <ElButton type="danger" text @click="remove(current)">{{ t('delete') }}</ElButton>
        </div>
      </div>
      <div class="gm-grid">
        <div v-for="a in current.items" :key="a.id" class="col-item">
          <AppGrid :apps="[a]" />
          <ElButton size="small" text type="danger" class="col-item__remove" @click="removeItem(a.id)">{{ t('remove') }}</ElButton>
        </div>
      </div>
      <EmptyState v-if="!current.items.length" :title="t('empty_collection')" :text="t('empty_collection_text')" icon="Collection" />
    </template>
    <template v-else>
      <div class="gm-section__head"><h2 class="gm-title">{{ t('dash_collections') }}</h2><ElButton type="primary" @click="openCreate"><ElIcon><Plus /></ElIcon>{{ t('new_collection') }}</ElButton></div>
      <EmptyState v-if="!list.length && !loading" :title="t('no_collections')" :text="t('no_collections_text')" icon="Collection" />
      <div class="cols">
        <RouterLink v-for="c in list" :key="c.id" :to="`/dashboard/collections/${c.id}`" class="gm-card cols__item">
          <strong>{{ c.name }}</strong>
          <span class="sub">{{ c.itemCount }} {{ t('stat_apps').toLowerCase() }} · {{ c.isPublic ? t('public') : t('private') }}</span>
          <p v-if="c.description" class="sub">{{ c.description }}</p>
        </RouterLink>
      </div>
    </template>

    <ElDialog v-model="dialog" :title="form.id ? t('edit') : t('new_collection')" width="480px">
      <ElForm label-position="top">
        <ElFormItem :label="t('name')"><ElInput v-model="form.name" maxlength="80" /></ElFormItem>
        <ElFormItem :label="t('description')"><ElInput v-model="form.description" type="textarea" :rows="3" maxlength="1000" /></ElFormItem>
        <ElFormItem><ElSwitch v-model="form.isPublic" :active-text="t('public')" :inactive-text="t('private')" /></ElFormItem>
      </ElForm>
      <template #footer><ElButton @click="dialog = false">{{ t('cancel') }}</ElButton><ElButton type="primary" :loading="saving" :disabled="!form.name.trim()" @click="save">{{ t('save') }}</ElButton></template>
    </ElDialog>
  </div>
</template>

<style scoped>
.cols { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 12px; }
.cols__item { padding: 14px 16px; color: var(--gm-text); display: flex; flex-direction: column; gap: 4px; }
.col-item { position: relative; }
.col-item__remove { position: absolute; top: 6px; right: 6px; background: var(--gm-card-bg); }
</style>
