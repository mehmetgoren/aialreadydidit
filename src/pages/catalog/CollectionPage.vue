<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { CollectionDto } from '@/utils/models/catalog-models'
import { CatalogService } from '@/utils/services/catalog-service'
import { useCommonStore } from '@/stores/common-store'
import AppGrid from '@/components/catalog/AppGrid.vue'
import EmptyState from '@/components/common/EmptyState.vue'

const route = useRoute()
const { t } = useI18n()
const common = useCommonStore()
const collection = ref<CollectionDto | null>(null)
const loading = ref(true)
const notFound = ref(false)

onMounted(async () => {
  try {
    collection.value = await new CatalogService().getPublicCollection(String(route.params.username), String(route.params.slug))
    common.setPageTitle(collection.value.name)
    common.setBreadcrumb([{ label: collection.value.ownerDisplayName, to: `/u/${collection.value.ownerUsername}` }, { label: collection.value.name }])
  } catch {
    notFound.value = true
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="gm-container" style="padding-top: 8px">
    <EmptyState v-if="notFound" :title="t('collection_not_found')" icon="Warning" />
    <div v-else v-loading="loading">
      <template v-if="collection">
        <h1 class="gm-title">{{ collection.name }}</h1>
        <p class="gm-muted">{{ collection.description }} · {{ t('by') }} <RouterLink :to="`/u/${collection.ownerUsername}`" class="gm-link">{{ collection.ownerDisplayName }}</RouterLink></p>
        <AppGrid :apps="collection.items" :empty-title="t('empty_collection')" />
      </template>
    </div>
  </div>
</template>
