<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AppCardDto } from '@/utils/models/catalog-models'
import { DashboardService } from '@/utils/services/dashboard-service'
import { useCommonStore } from '@/stores/common-store'
import AppGrid from '@/components/catalog/AppGrid.vue'
import { notifyError } from '@/utils/tools'

const { t } = useI18n()
const common = useCommonStore()
const apps = ref<AppCardDto[]>([])
const loading = ref(true)

onMounted(async () => {
  common.setPageTitle(t('dash_favorites'))
  try {
    apps.value = await new DashboardService().favorites()
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <h2 class="gm-title">{{ t('dash_favorites') }}</h2>
    <AppGrid :apps="apps" :loading="loading" :empty-title="t('no_favorites')" :empty-text="t('no_favorites_text')" />
  </div>
</template>
