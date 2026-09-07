<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import type { HomeContent } from '@/utils/models/catalog-models'
import { CatalogService } from '@/utils/services/catalog-service'
import { useCommonStore } from '@/stores/common-store'
import { useCategoryName } from '@/stores/category-store'
import { useSiteStore } from '@/stores/site-store'
import SavingsCounter from '@/components/common/SavingsCounter.vue'
import AppRow from '@/components/catalog/AppRow.vue'
import { formatCompact } from '@/utils/format'
import { assetUrl, notifyError } from '@/utils/tools'

const { t } = useI18n()
const router = useRouter()
const common = useCommonStore()
const site = useSiteStore()
const name = useCategoryName()
const home = ref<HomeContent | null>(null)
const loading = ref(true)
const q = ref('')

onMounted(async () => {
  common.setPageTitle('', t('seo_home_description'))
  try {
    home.value = await new CatalogService().getHome()
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
})

function search() {
  router.push({ path: '/search', query: q.value.trim() ? { q: q.value.trim() } : {} })
}
</script>

<template>
  <div class="gm-container home">
    <section class="home__hero">
      <div class="home__hero-text">
        <h1>{{ t('hero_title') }}</h1>
        <p>{{ t('hero_subtitle') }}</p>
        <form class="home__search" @submit.prevent="search">
          <ElInput v-model="q" size="large" :placeholder="t('hero_search_placeholder')">
            <template #prefix><ElIcon><Search /></ElIcon></template>
            <template #append><ElButton type="primary" native-type="submit">{{ t('search') }}</ElButton></template>
          </ElInput>
        </form>
        <div class="home__hero-links">
          <span v-if="site.semantic" class="home__hero-badge">✨ {{ t('semantic_search_on') }}</span>
          <RouterLink to="/for-agents" class="gm-link">{{ t('hero_for_agents') }} →</RouterLink>
          <RouterLink to="/upload" class="gm-link">{{ t('hero_publish') }} →</RouterLink>
        </div>
      </div>
      <SavingsCounter :savings="home?.savings ?? null" />
    </section>

    <section v-if="home" class="home__stats">
      <div class="home__stat"><strong>{{ formatCompact(home.stats.publishedApps) }}</strong><span>{{ t('stat_apps') }}</span></div>
      <div class="home__stat"><strong>{{ formatCompact(home.stats.downloads) }}</strong><span>{{ t('stat_downloads') }}</span></div>
      <div class="home__stat"><strong>{{ formatCompact(home.stats.searches) }}</strong><span>{{ t('stat_searches') }}</span></div>
      <div class="home__stat"><strong>{{ formatCompact(home.stats.agentSearches) }}</strong><span>{{ t('stat_agent_calls') }}</span></div>
      <div class="home__stat"><strong>{{ formatCompact(home.stats.members) }}</strong><span>{{ t('stat_members') }}</span></div>
      <RouterLink to="/wanted" class="home__stat home__stat--link"><strong>{{ formatCompact(home.openRequestCount) }}</strong><span>{{ t('stat_wanted') }}</span></RouterLink>
    </section>

    <section v-if="home?.banners.length" class="home__banners">
      <ElCarousel height="220px" :interval="6000" indicator-position="outside">
        <ElCarouselItem v-for="b in home.banners" :key="b.id">
          <component :is="b.link ? 'a' : 'div'" :href="b.link || undefined" class="home__banner" :style="b.imageUrl ? { backgroundImage: `url(${assetUrl(b.imageUrl)})` } : {}">
            <div class="home__banner-text"><h3>{{ b.title }}</h3><p v-if="b.subtitle">{{ b.subtitle }}</p></div>
          </component>
        </ElCarouselItem>
      </ElCarousel>
    </section>

    <div v-loading="loading" class="home__body">
      <AppRow v-if="home" :title="t('featured_apps')" :apps="home.featured" to="/search?sort=featured" />
      <AppRow v-if="home" :title="t('trending_apps')" :subtitle="t('trending_hint')" :apps="home.trending" to="/search?sort=downloads" />

      <section v-if="home" class="gm-section">
        <div class="gm-section__head"><h2>{{ t('browse_categories') }}</h2><RouterLink to="/search" class="gm-link">{{ t('see_all') }} →</RouterLink></div>
        <div class="home__cats">
          <RouterLink v-for="c in home.categories" :key="c.id" :to="`/category/${c.slug}`" class="home__cat">
            <ElIcon :size="22"><component :is="c.icon || 'Folder'" /></ElIcon>
            <span>{{ name(c) }}</span>
            <small>{{ c.appCount }}</small>
          </RouterLink>
        </div>
      </section>

      <AppRow v-if="home" :title="t('newest_apps')" :apps="home.newest" to="/search?sort=newest" />
      <AppRow v-if="home" :title="t('top_rated_apps')" :apps="home.topRated" to="/search?sort=rating" />
      <AppRow v-if="home" :title="t('recently_updated')" :apps="home.recentlyUpdated" to="/search?sort=updated" />

      <section class="home__mission gm-card">
        <div>
          <h2>{{ t('mission_title') }}</h2>
          <p>{{ t('mission_text') }}</p>
          <ol>
            <li>{{ t('rule_open_source') }}</li>
            <li>{{ t('rule_installable') }}</li>
            <li>{{ t('rule_free') }}</li>
          </ol>
        </div>
        <div class="home__mission-cta">
          <RouterLink to="/about"><ElButton size="large">{{ t('about') }}</ElButton></RouterLink>
          <RouterLink to="/for-agents"><ElButton size="large" type="primary">{{ t('connect_your_agent') }}</ElButton></RouterLink>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped lang="scss">
.home {
  padding-top: 24px;
  &__hero {
    display: grid;
    grid-template-columns: 1.1fr 1fr;
    gap: 28px;
    align-items: center;
    margin-bottom: 20px;
    h1 {
      font-size: 36px;
      line-height: 1.15;
      letter-spacing: -0.8px;
      margin: 0 0 10px;
    }
    p {
      color: var(--gm-text-muted);
      font-size: 15px;
      margin: 0 0 16px;
      max-width: 520px;
    }
    &-links {
      display: flex;
      gap: 16px;
      align-items: center;
      margin-top: 12px;
      font-size: 13px;
      flex-wrap: wrap;
    }
    &-badge {
      padding: 2px 8px;
      border-radius: 999px;
      background: var(--gm-green-light);
      color: var(--gm-green-dark);
      font-weight: 600;
    }
  }
  &__search :deep(.el-input-group__append) {
    background: var(--gm-primary);
    border-color: var(--gm-primary);
    .el-button {
      color: #fff;
    }
  }
  &__stats {
    display: grid;
    grid-template-columns: repeat(6, 1fr);
    gap: 10px;
    margin: 6px 0 10px;
  }
  &__stat {
    display: flex;
    flex-direction: column;
    padding: 10px 14px;
    border-radius: 10px;
    background: var(--gm-card-bg);
    border: 1px solid var(--gm-border);
    color: var(--gm-text);
    strong {
      font-size: 20px;
    }
    span {
      font-size: 12px;
      color: var(--gm-text-muted);
    }
    &--link:hover {
      border-color: var(--gm-primary);
    }
  }
  &__banners {
    margin: 16px 0;
  }
  &__banner {
    display: flex;
    align-items: flex-end;
    height: 100%;
    border-radius: 12px;
    background: var(--gm-navy) center/cover no-repeat;
    color: #fff;
    &-text {
      padding: 18px 22px;
      background: linear-gradient(transparent, rgba(0, 0, 0, 0.6));
      width: 100%;
      h3 { margin: 0; font-size: 22px; }
      p { margin: 4px 0 0; opacity: 0.9; }
    }
  }
  &__cats {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(170px, 1fr));
    gap: 10px;
  }
  &__cat {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 12px 14px;
    border-radius: 10px;
    background: var(--gm-card-bg);
    border: 1px solid var(--gm-border);
    color: var(--gm-text);
    font-weight: 600;
    font-size: 13px;
    span {
      flex: 1;
    }
    small {
      color: var(--gm-text-muted);
    }
    .el-icon {
      color: var(--gm-primary);
    }
    &:hover {
      border-color: var(--gm-primary);
      box-shadow: var(--gm-shadow-hover);
    }
  }
  &__mission {
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 24px;
    align-items: center;
    padding: 26px 28px;
    margin: 32px 0;
    h2 { margin: 0 0 8px; }
    p { color: var(--gm-text-muted); max-width: 640px; }
    ol { margin: 8px 0 0; padding-left: 20px; li { margin: 4px 0; } }
    &-cta {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }
  }
  @media (max-width: 900px) {
    &__hero, &__mission { grid-template-columns: 1fr; }
    &__stats { grid-template-columns: repeat(3, 1fr); }
    &__hero h1 { font-size: 28px; }
  }
}
</style>
