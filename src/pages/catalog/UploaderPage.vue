<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { AppCardDto, CollectionDto, UploaderDto } from '@/utils/models/catalog-models'
import { CatalogService } from '@/utils/services/catalog-service'
import { useCommonStore } from '@/stores/common-store'
import AppGrid from '@/components/catalog/AppGrid.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import { formatCompact, formatDate } from '@/utils/format'

const route = useRoute()
const { t } = useI18n()
const common = useCommonStore()
const service = new CatalogService()
const uploader = ref<UploaderDto | null>(null)
const apps = ref<AppCardDto[]>([])
const collections = ref<CollectionDto[]>([])
const loading = ref(true)
const notFound = ref(false)
const username = computed(() => String(route.params.username))

async function load() {
  loading.value = true
  notFound.value = false
  try {
    uploader.value = await service.getUploader(username.value)
    common.setPageTitle(uploader.value.displayName)
    common.setBreadcrumb([{ label: uploader.value.displayName }])
    const [r, c] = await Promise.all([service.search({ uploader: username.value, sort: 'downloads', pageSize: 48 }), service.getPublicCollections(username.value)])
    apps.value = r.page.items
    collections.value = c
  } catch {
    notFound.value = true
  } finally {
    loading.value = false
  }
}
onMounted(load)
watch(username, load)
</script>

<template>
  <div class="gm-container uploader">
    <EmptyState v-if="notFound" :title="t('member_not_found')" icon="Warning" />
    <div v-else v-loading="loading">
      <header v-if="uploader" class="gm-card uploader__head">
        <ElAvatar :size="72" :src="uploader.avatarUrl || undefined">{{ uploader.displayName.slice(0, 1).toUpperCase() }}</ElAvatar>
        <div class="uploader__info">
          <h1>{{ uploader.displayName }} <ElTag v-if="uploader.trustLevel >= 2" type="success" size="small">{{ t('verified_publisher') }}</ElTag><ElTag v-else-if="uploader.trustLevel === 1" size="small">{{ t('trusted') }}</ElTag></h1>
          <div class="gm-muted">@{{ uploader.username }} · {{ t('member_since') }} {{ formatDate(uploader.memberSince) }}</div>
          <p v-if="uploader.bio">{{ uploader.bio }}</p>
          <a v-if="uploader.website" :href="uploader.website" target="_blank" rel="noopener" class="gm-link">{{ uploader.website }}</a>
        </div>
        <div class="uploader__stats">
          <div><strong>{{ uploader.appCount }}</strong><span>{{ t('stat_apps') }}</span></div>
          <div><strong>{{ formatCompact(uploader.totalDownloads) }}</strong><span>{{ t('stat_downloads') }}</span></div>
        </div>
      </header>
      <section class="gm-section">
        <div class="gm-section__head"><h2>{{ t('published_apps') }}</h2></div>
        <AppGrid :apps="apps" :loading="loading" :empty-title="t('no_apps_yet')" />
      </section>
      <section v-if="collections.length" class="gm-section">
        <div class="gm-section__head"><h2>{{ t('public_collections') }}</h2></div>
        <div class="uploader__collections">
          <RouterLink v-for="c in collections" :key="c.id" :to="`/u/${uploader?.username}/collections/${c.slug}`" class="gm-card uploader__collection">
            <strong>{{ c.name }}</strong>
            <span class="gm-muted">{{ c.itemCount }} {{ t('stat_apps').toLowerCase() }}</span>
            <p v-if="c.description" class="sub">{{ c.description }}</p>
          </RouterLink>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped lang="scss">
.uploader {
  padding-top: 8px;
  &__head {
    display: flex;
    gap: 18px;
    padding: 20px;
    align-items: flex-start;
    h1 { margin: 0 0 4px; font-size: 24px; display: flex; gap: 8px; align-items: center; }
    p { margin: 8px 0 4px; }
  }
  &__info { flex: 1; }
  &__stats {
    display: flex;
    gap: 20px;
    > div { display: flex; flex-direction: column; align-items: center; }
    strong { font-size: 22px; }
    span { font-size: 12px; color: var(--gm-text-muted); }
  }
  &__collections {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
    gap: 12px;
  }
  &__collection {
    padding: 14px 16px;
    color: var(--gm-text);
    display: flex;
    flex-direction: column;
    gap: 4px;
  }
}
</style>
