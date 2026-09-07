<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminPage from '@/components/admin/AdminPage.vue'
import { AdminContentService } from '@/utils/services/admin-service'
import { CatalogService } from '@/utils/services/catalog-service'
import type { FeaturedItem } from '@/utils/models/admin-models'
import { assetUrl, notifyError, notifyS } from '@/utils/tools'

const { t } = useI18n()
const service = new AdminContentService()
const rows = ref<FeaturedItem[]>([])
const loading = ref(true)
const options = ref<{ id: number; name: string }[]>([])
const pick = ref<number | null>(null)
const note = ref('')

async function load() {
  loading.value = true
  rows.value = await service.featured().catch((e) => { notifyError(e); return [] })
  loading.value = false
}
onMounted(load)
async function search(q: string) {
  if (!q) return
  const r = await new CatalogService().search({ q, pageSize: 10 }).catch(() => null)
  options.value = r?.page.items.map((a) => ({ id: a.id, name: a.name })) ?? []
}
async function add() {
  if (!pick.value) return
  rows.value = await service.feature(pick.value, note.value || undefined).catch((e) => { notifyError(e); return rows.value })
  pick.value = null
  note.value = ''
  notifyS(t('saved'))
}
async function move(i: number, dir: -1 | 1) {
  const ids = rows.value.map((r) => r.id)
  const j = i + dir
  if (j < 0 || j >= ids.length) return
  ;[ids[i], ids[j]] = [ids[j]!, ids[i]!]
  rows.value = await service.reorderFeatured(ids).catch((e) => { notifyError(e); return rows.value })
}
async function remove(r: FeaturedItem) {
  rows.value = await service.unfeature(r.id).catch((e) => { notifyError(e); return rows.value })
}
</script>

<template>
  <AdminPage :title="t('adm_content_featured')" :subtitle="t('adm_featured_subtitle')" :loading="loading">
    <div class="feat__add">
      <ElSelect v-model="pick" filterable remote :remote-method="search" :placeholder="t('adm_search_app')" style="width: 320px"><ElOption v-for="o in options" :key="o.id" :value="o.id" :label="o.name" /></ElSelect>
      <ElInput v-model="note" :placeholder="t('adm_featured_note')" style="width: 260px" maxlength="200" />
      <ElButton type="primary" :disabled="!pick" @click="add"><ElIcon><Plus /></ElIcon>{{ t('adm_feature') }}</ElButton>
    </div>
    <ElTable :data="rows" class="gm-table" size="small">
      <ElTableColumn label="#" width="60"><template #default="{ $index }">{{ $index + 1 }}</template></ElTableColumn>
      <ElTableColumn :label="t('app')" min-width="240"><template #default="{ row }"><div style="display: flex; gap: 8px; align-items: center"><img v-if="row.iconUrl" :src="assetUrl(row.iconUrl)" style="width: 28px; height: 28px; border-radius: 6px" alt="" /><RouterLink :to="`/app/${row.slug}`" target="_blank" class="gm-link">{{ row.name }}</RouterLink></div></template></ElTableColumn>
      <ElTableColumn prop="featuredNote" :label="t('adm_featured_note')" min-width="200" />
      <ElTableColumn prop="downloadCount" :label="t('downloads')" width="100" align="right" />
      <ElTableColumn width="200" align="right"><template #default="{ row, $index }"><ElButton size="small" text @click="move($index, -1)">↑</ElButton><ElButton size="small" text @click="move($index, 1)">↓</ElButton><ElButton size="small" type="danger" text @click="remove(row as FeaturedItem)">{{ t('remove') }}</ElButton></template></ElTableColumn>
    </ElTable>
  </AdminPage>
</template>

<style scoped>
.feat__add { display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap; }
</style>
