<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { RatingDto } from '@/utils/models/catalog-models'
import { DashboardService } from '@/utils/services/dashboard-service'
import { useCommonStore } from '@/stores/common-store'
import ScoreBadge from '@/components/common/ScoreBadge.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import { notifyError } from '@/utils/tools'
import { formatDate } from '@/utils/format'

const { t } = useI18n()
const common = useCommonStore()
const rows = ref<RatingDto[]>([])
const loading = ref(true)

onMounted(async () => {
  common.setPageTitle(t('dash_ratings'))
  try {
    rows.value = await new DashboardService().ratings()
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <h2 class="gm-title">{{ t('dash_ratings') }}</h2>
    <div v-loading="loading" style="min-height: 80px">
      <EmptyState v-if="!rows.length && !loading" :title="t('no_ratings')" icon="ChatLineSquare" />
      <article v-for="r in rows" :key="r.id" class="mr">
        <div class="mr__head">
          <RouterLink :to="`/app/${r.appSlug}#reviews`" class="gm-link"><strong>{{ r.appName }}</strong></RouterLink>
          <ScoreBadge :score="r.score" :count="1" />
          <ElTag v-if="r.worked === true" type="success" size="small" effect="plain">✔ {{ t('worked') }}</ElTag>
          <ElTag v-else-if="r.worked === false" type="danger" size="small" effect="plain">✘ {{ t('did_not_work') }}</ElTag>
          <span class="sub">{{ formatDate(r.updatedAt) }}<span v-if="r.version"> · v{{ r.version }}</span></span>
        </div>
        <p v-if="r.review" class="mr__text">{{ r.review }}</p>
        <div v-for="rep in r.replies" :key="rep.id" class="mr__reply"><strong>{{ rep.displayName }}</strong>: {{ rep.body }}</div>
      </article>
    </div>
  </div>
</template>

<style scoped>
.mr { padding: 12px 14px; border: 1px solid var(--gm-border); border-radius: 10px; margin-bottom: 10px; }
.mr__head { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.mr__text { margin: 8px 0 0; white-space: pre-wrap; }
.mr__reply { margin-top: 8px; padding: 6px 10px; background: var(--gm-page-bg); border-radius: 6px; font-size: 13px; }
</style>
