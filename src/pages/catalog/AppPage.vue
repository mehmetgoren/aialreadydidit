<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { AppCardDto, AppDetail, LineageNode } from '@/utils/models/catalog-models'
import { CatalogService } from '@/utils/services/catalog-service'
import { DashboardService } from '@/utils/services/dashboard-service'
import { useCommonStore } from '@/stores/common-store'
import { useUserStore } from '@/stores/user-store'
import { useCategoryName } from '@/stores/category-store'
import ScreenshotGallery from '@/components/catalog/ScreenshotGallery.vue'
import DownloadButtons from '@/components/catalog/DownloadButtons.vue'
import PromptBlock from '@/components/catalog/PromptBlock.vue'
import LineageTree from '@/components/catalog/LineageTree.vue'
import VersionsList from '@/components/catalog/VersionsList.vue'
import RatingsSection from '@/components/catalog/RatingsSection.vue'
import ReportDialog from '@/components/catalog/ReportDialog.vue'
import CollectionPicker from '@/components/catalog/CollectionPicker.vue'
import AppGrid from '@/components/catalog/AppGrid.vue'
import MarkdownView from '@/components/common/MarkdownView.vue'
import ScoreBadge from '@/components/common/ScoreBadge.vue'
import PlatformBadges from '@/components/common/PlatformBadges.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import { formatCompact, formatDate, formatMoney, formatNumber } from '@/utils/format'
import { assetUrl, notifyError, notifyS } from '@/utils/tools'

const route = useRoute()
const { t } = useI18n()
const common = useCommonStore()
const user = useUserStore()
const name = useCategoryName()
const catalog = new CatalogService()
const dashboard = new DashboardService()

const app = ref<AppDetail | null>(null)
/** Raw-content root of the linked repository so README images with relative paths resolve (archives have no such base). */
const readmeBaseUrl = computed(() => {
  const url = app.value?.repoUrl
  if (!url) return null
  const gh = url.match(/^https?:\/\/github\.com\/([^/]+)\/([^/#?]+)/i)
  if (gh) return `https://raw.githubusercontent.com/${gh[1]}/${gh[2].replace(/\.git$/, '')}/HEAD/`
  const gl = url.match(/^https?:\/\/gitlab\.com\/(.+?)(?:\.git)?\/?$/i)
  if (gl) return `https://gitlab.com/${gl[1]}/-/raw/HEAD/`
  return null
})
const similar = ref<AppCardDto[]>([])
const lineage = ref<LineageNode | null>(null)
const loading = ref(true)
const notFound = ref(false)
const tab = ref('description')
const reportOpen = ref(false)
const reportRating = ref<number | null>(null)

const slug = computed(() => String(route.params.slug))
const files = computed(() => app.value?.latestVersion?.files ?? [])

async function load() {
  loading.value = true
  notFound.value = false
  try {
    app.value = await catalog.getApp(slug.value)
    common.setPageTitle(app.value.name, app.value.shortDescription)
    common.setBreadcrumb([...app.value.categoryPath.map((c) => ({ label: name(c), to: `/category/${c.slug}` })), { label: app.value.name }])
    catalog.getSimilar(slug.value).then((s) => (similar.value = s)).catch(() => {})
    if (app.value.derivedFrom || app.value.derivatives.length) catalog.getLineage(slug.value).then((l) => (lineage.value = l)).catch(() => {})
  } catch {
    notFound.value = true
  } finally {
    loading.value = false
  }
}
onMounted(load)
watch(slug, load)

async function toggleFavorite() {
  if (!user.isAuthenticated || !app.value) return
  try {
    const on = await dashboard.toggleFavorite(app.value.id)
    app.value.viewer.isFavorite = on
    app.value.favoriteCount += on ? 1 : -1
  } catch (err) {
    notifyError(err)
  }
}

async function toggleWatch() {
  if (!user.isAuthenticated || !app.value) return
  try {
    const on = app.value.viewer.isWatching ? await dashboard.unwatch(app.value.id) : await dashboard.watch(app.value.id)
    app.value.viewer.isWatching = on
    notifyS(on ? t('watching_on') : t('watching_off'))
  } catch (err) {
    notifyError(err)
  }
}

function onDownloaded() {
  if (app.value) {
    app.value.downloadCount++
    if (user.isAuthenticated && !app.value.viewer.isOwner) {
      app.value.viewer.hasDownloaded = true
      app.value.viewer.canRate = true
    }
  }
}

function openReport(ratingId: number | null = null) {
  reportRating.value = ratingId
  reportOpen.value = true
}
</script>

<template>
  <div class="gm-container app-page">
    <EmptyState v-if="notFound" :title="t('app_not_found')" icon="Warning" />
    <div v-else v-loading="loading" class="app-page__body">
      <template v-if="app">
        <ElAlert v-if="app.status !== 'published'" type="warning" show-icon :closable="false" class="app-page__status">
          <template #title>{{ t('app_status_notice') }} <StatusTag :value="app.status" /> <span v-if="app.rejectionReason">— {{ app.rejectionReason }}</span></template>
        </ElAlert>

        <header class="app-page__head gm-card">
          <img v-if="app.iconUrl" :src="assetUrl(app.iconUrl)" class="app-page__icon" alt="" />
          <div v-else class="app-page__icon app-page__icon--letter">{{ app.name.slice(0, 1).toUpperCase() }}</div>
          <div class="app-page__titles">
            <h1>{{ app.name }} <ElTag v-if="app.isFeatured" type="warning" size="small" effect="dark">★ {{ t('featured') }}</ElTag></h1>
            <p class="app-page__short">{{ app.shortDescription }}</p>
            <div class="app-page__facts">
              <RouterLink :to="`/u/${app.uploader.username}`" class="gm-link">{{ app.uploader.displayName }}</RouterLink>
              <span>·</span>
              <RouterLink :to="`/category/${app.category.slug}`" class="gm-link">{{ name(app.category) }}</RouterLink>
              <span>·</span>
              <span :title="app.license.name">{{ app.license.spdxId }}</span>
              <span v-if="app.llmModel">·</span>
              <span v-if="app.llmModel" class="app-page__model">🤖 {{ app.llmModel.displayName }}</span>
            </div>
            <div class="app-page__badges">
              <ScoreBadge :score="app.ratingAvg" :count="app.ratingCount" />
              <span class="gm-muted"><ElIcon><Download /></ElIcon> {{ formatCompact(app.downloadCount) }}</span>
              <PlatformBadges :platforms="app.platforms" size="default" />
              <span v-if="app.latestVersion" class="gm-muted">v{{ app.latestVersion.version }} · {{ formatDate(app.latestVersion.releasedAt) }}</span>
            </div>
          </div>
          <div class="app-page__actions">
            <template v-if="user.isAuthenticated">
              <ElButton :type="app.viewer.isFavorite ? 'warning' : ''" @click="toggleFavorite"><ElIcon><component :is="app.viewer.isFavorite ? 'StarFilled' : 'Star'" /></ElIcon>{{ app.favoriteCount }}</ElButton>
              <ElButton :type="app.viewer.isWatching ? 'primary' : ''" @click="toggleWatch"><ElIcon><Bell /></ElIcon>{{ t('notify_new_version') }}</ElButton>
              <CollectionPicker :app-id="app.id" :selected-ids="app.viewer.collectionIds" @changed="app.viewer.collectionIds = $event" />
              <RouterLink v-if="app.viewer.isOwner" :to="`/upload/${app.id}`"><ElButton type="primary" plain><ElIcon><Edit /></ElIcon>{{ t('edit') }}</ElButton></RouterLink>
            </template>
            <ElButton text type="danger" @click="openReport()"><ElIcon><WarningFilled /></ElIcon>{{ t('report') }}</ElButton>
          </div>
        </header>

        <div class="app-page__grid">
          <main class="app-page__main">
            <ScreenshotGallery :screenshots="app.screenshots" />
            <ElTabs v-model="tab" class="app-page__tabs">
              <ElTabPane :label="t('tab_description')" name="description">
                <MarkdownView :source="app.longDescription" />
                <div v-if="app.tags.length" class="app-page__tags">
                  <RouterLink v-for="tag in app.tags" :key="tag" :to="{ path: '/search', query: { tags: tag } }"><ElTag size="small" effect="plain">{{ tag }}</ElTag></RouterLink>
                </div>
              </ElTabPane>
              <ElTabPane v-if="app.readmeMarkdown && app.readmeMarkdown !== app.longDescription" label="README" name="readme">
                <MarkdownView :source="app.readmeMarkdown" :base-url="readmeBaseUrl" />
              </ElTabPane>
              <ElTabPane :label="`${t('tab_prompts')} (${app.prompts.length})`" name="prompts">
                <p class="gm-muted">{{ t('prompts_intro') }}</p>
                <PromptBlock :prompts="app.prompts" :app-name="app.name" :repo-url="app.repoUrl" />
              </ElTabPane>
              <ElTabPane :label="`${t('tab_versions')} (${app.versions.length})`" name="versions">
                <VersionsList :versions="app.versions" />
              </ElTabPane>
              <ElTabPane :label="`${t('tab_reviews')} (${app.ratingCount})`" name="reviews">
                <RatingsSection :app="app" @report="openReport" @changed="load" />
              </ElTabPane>
            </ElTabs>
          </main>

          <aside class="app-page__side">
            <section class="gm-card app-page__box">
              <h3>{{ t('download') }}</h3>
              <DownloadButtons v-if="files.length" :slug="app.slug" :files="files" :version="app.latestVersion?.version ?? ''" @downloaded="onDownloaded" />
              <div v-else class="gm-muted">{{ t('no_files_yet') }}</div>
            </section>

            <section class="gm-card app-page__box">
              <h3>{{ t('source_code') }}</h3>
              <a v-if="app.repoUrl" :href="app.repoUrl" target="_blank" rel="noopener" class="app-page__repo">
                <ElIcon><Link /></ElIcon>{{ app.repoUrl.replace(/^https?:\/\//, '') }}
              </a>
              <div class="app-page__kv">
                <span>{{ t('license') }}</span><a :href="app.license.url || '#'" target="_blank" rel="noopener">{{ app.license.spdxId }}</a>
                <template v-if="app.repoPrimaryLanguage"><span>{{ t('language') }}</span><b>{{ app.repoPrimaryLanguage }}</b></template>
                <template v-if="app.repoStars != null"><span>GitHub ★</span><b>{{ app.repoStars }}</b></template>
                <span>{{ t('source_size') }}</span><b>{{ formatNumber(app.sourceLineCount) }} {{ t('lines') }} · {{ app.sourceFileCount }} {{ t('files') }}</b>
                <span>{{ t('generated_by') }}</span><b>{{ app.llmModel?.displayName ?? '—' }}<small v-if="app.llmModelNote" class="gm-muted"> · {{ app.llmModelNote }}</small></b>
                <template v-if="app.homepageUrl"><span>{{ t('homepage') }}</span><a :href="app.homepageUrl" target="_blank" rel="noopener">{{ app.homepageUrl.replace(/^https?:\/\//, '') }}</a></template>
              </div>
            </section>

            <section class="gm-card app-page__box app-page__savings">
              <h3>{{ t('savings_title') }}</h3>
              <div class="app-page__savings-big">{{ formatNumber(app.estSavedTokens) }} <small>{{ t('tokens') }}</small></div>
              <div class="gm-muted">{{ t('savings_per_download', { tokens: formatNumber(app.estGenerationTokens), cost: formatMoney(app.estGenerationCostUsd) }) }}</div>
              <div class="gm-muted">≈ {{ formatMoney(app.estSavedCostUsd) }} {{ t('saved_so_far') }}</div>
            </section>

            <section v-if="lineage" class="gm-card app-page__box">
              <h3>{{ t('lineage') }}</h3>
              <LineageTree :node="lineage" :current-id="app.id" />
            </section>

            <section class="gm-card app-page__box">
              <h3>{{ t('uploader') }}</h3>
              <RouterLink :to="`/u/${app.uploader.username}`" class="app-page__uploader">
                <ElAvatar :size="40" :src="app.uploader.avatarUrl || undefined">{{ app.uploader.displayName.slice(0, 1).toUpperCase() }}</ElAvatar>
                <div><b>{{ app.uploader.displayName }}</b><div class="sub">{{ t('uploader_stats', { apps: app.uploader.appCount, downloads: formatCompact(app.uploader.totalDownloads) }) }}</div></div>
              </RouterLink>
            </section>
          </aside>
        </div>

        <section v-if="similar.length" class="gm-section">
          <div class="gm-section__head"><h2>{{ t('similar_apps') }}</h2></div>
          <AppGrid :apps="similar" show-similarity />
        </section>

        <ReportDialog v-model="reportOpen" :slug="app.slug" :rating-id="reportRating" />
      </template>
    </div>
  </div>
</template>

<style scoped lang="scss">
.app-page {
  padding-top: 8px;
  &__body {
    min-height: 300px;
  }
  &__status {
    margin-bottom: 12px;
  }
  &__head {
    display: flex;
    gap: 18px;
    padding: 20px;
    align-items: flex-start;
    flex-wrap: wrap;
    h1 {
      margin: 0 0 4px;
      font-size: 26px;
      letter-spacing: -0.4px;
      display: flex;
      align-items: center;
      gap: 10px;
      flex-wrap: wrap;
    }
  }
  &__icon {
    width: 84px;
    height: 84px;
    border-radius: 18px;
    object-fit: cover;
    flex-shrink: 0;
    &--letter {
      display: grid;
      place-items: center;
      background: var(--gm-primary-light);
      color: var(--gm-primary);
      font-weight: 800;
      font-size: 34px;
    }
  }
  &__titles {
    flex: 1;
    min-width: 260px;
  }
  &__short {
    margin: 0 0 8px;
    color: var(--gm-text-muted);
    font-size: 15px;
  }
  &__facts {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
    font-size: 13px;
    color: var(--gm-text-muted);
  }
  &__badges {
    display: flex;
    gap: 14px;
    align-items: center;
    margin-top: 10px;
    flex-wrap: wrap;
    font-size: 13px;
  }
  &__actions {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
    align-items: flex-start;
    .el-button + .el-button { margin-left: 0; }
  }
  &__grid {
    display: grid;
    grid-template-columns: 1fr 340px;
    gap: 20px;
    margin-top: 18px;
    align-items: start;
  }
  &__main {
    min-width: 0;
  }
  &__tabs {
    margin-top: 16px;
  }
  &__tags {
    display: flex;
    gap: 6px;
    flex-wrap: wrap;
    margin-top: 12px;
  }
  &__box {
    padding: 16px;
    margin-bottom: 14px;
    h3 {
      margin: 0 0 10px;
      font-size: 14px;
      text-transform: uppercase;
      letter-spacing: 0.3px;
      color: var(--gm-text-muted);
    }
  }
  &__repo {
    display: flex;
    align-items: center;
    gap: 6px;
    word-break: break-all;
    margin-bottom: 10px;
    font-weight: 600;
  }
  &__kv {
    display: grid;
    grid-template-columns: auto 1fr;
    gap: 6px 12px;
    font-size: 13px;
    span { color: var(--gm-text-muted); }
    b { font-weight: 600; word-break: break-word; }
  }
  &__savings {
    background: linear-gradient(135deg, var(--gm-green-light), var(--gm-card-bg));
    &-big {
      font-size: 24px;
      font-weight: 800;
      small { font-size: 13px; font-weight: 500; color: var(--gm-text-muted); }
    }
  }
  &__uploader {
    display: flex;
    gap: 10px;
    align-items: center;
    color: var(--gm-text);
  }
  @media (max-width: 900px) {
    &__grid { grid-template-columns: 1fr; }
  }
}
</style>
