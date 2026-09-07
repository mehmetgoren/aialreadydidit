<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { WatchDto } from '@/utils/models/dashboard-models'
import { DashboardService } from '@/utils/services/dashboard-service'
import { useCommonStore } from '@/stores/common-store'
import EmptyState from '@/components/common/EmptyState.vue'
import { assetUrl, notifyError } from '@/utils/tools'

const { t } = useI18n()
const common = useCommonStore()
const service = new DashboardService()
const rows = ref<WatchDto[]>([])
const loading = ref(true)

async function load() {
  loading.value = true
  try {
    rows.value = await service.watches()
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(() => {
  common.setPageTitle(t('dash_watches'))
  load()
})

async function update(w: WatchDto) {
  try {
    await service.watch(w.app.id, w.notifyNewVersion, w.notifyReplies)
  } catch (err) {
    notifyError(err)
  }
}
async function remove(w: WatchDto) {
  try {
    await service.unwatch(w.app.id)
    rows.value = rows.value.filter((x) => x.id !== w.id)
  } catch (err) {
    notifyError(err)
  }
}
</script>

<template>
  <div>
    <h2 class="gm-title">{{ t('dash_watches') }}</h2>
    <p class="gm-muted">{{ t('watches_intro') }}</p>
    <div v-loading="loading" style="min-height: 80px">
      <EmptyState v-if="!rows.length && !loading" :title="t('no_watches')" icon="Bell" />
      <div v-for="w in rows" :key="w.id" class="watch">
        <img v-if="w.app.iconUrl" :src="assetUrl(w.app.iconUrl)" alt="" />
        <div class="watch__letter" v-else>{{ w.app.name.slice(0, 1) }}</div>
        <RouterLink :to="`/app/${w.app.slug}`" class="gm-link watch__name"><strong>{{ w.app.name }}</strong><div class="sub">v{{ w.app.latestVersion ?? '—' }}</div></RouterLink>
        <ElCheckbox v-model="w.notifyNewVersion" @change="update(w)">{{ t('notify_new_version') }}</ElCheckbox>
        <ElCheckbox v-model="w.notifyReplies" @change="update(w)">{{ t('notify_replies') }}</ElCheckbox>
        <ElButton text type="danger" size="small" @click="remove(w)">{{ t('remove') }}</ElButton>
      </div>
    </div>
  </div>
</template>

<style scoped>
.watch { display: flex; align-items: center; gap: 12px; padding: 10px 12px; border: 1px solid var(--gm-border); border-radius: 10px; margin-bottom: 8px; flex-wrap: wrap; }
.watch img, .watch__letter { width: 36px; height: 36px; border-radius: 9px; object-fit: cover; }
.watch__letter { display: grid; place-items: center; background: var(--gm-primary-light); color: var(--gm-primary); font-weight: 800; }
.watch__name { flex: 1; color: var(--gm-text); }
</style>
