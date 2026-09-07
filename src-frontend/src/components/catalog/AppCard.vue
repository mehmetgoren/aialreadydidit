<script setup lang="ts">
import { useCategoryName } from '@/stores/category-store'
import type { AppCardDto } from '@/utils/models/catalog-models'
import { assetUrl } from '@/utils/tools'
import { formatCompact } from '@/utils/format'
import PlatformBadges from '@/components/common/PlatformBadges.vue'
import ScoreBadge from '@/components/common/ScoreBadge.vue'

/** Card shown in grids, carousels and search results (Play Store style). */
withDefaults(defineProps<{ app: AppCardDto; horizontal?: boolean; showSimilarity?: boolean }>(), { horizontal: false, showSimilarity: false })
const name = useCategoryName()
</script>

<template>
  <RouterLink :to="`/app/${app.slug}`" class="app-card" :class="{ 'is-horizontal': horizontal }">
    <div class="app-card__cover">
      <img v-if="app.coverUrl" :src="assetUrl(app.coverUrl)" :alt="app.name" loading="lazy" />
      <div v-else class="app-card__cover-empty"><ElIcon :size="34"><Picture /></ElIcon></div>
      <span v-if="app.isFeatured" class="app-card__featured">★</span>
      <span v-if="showSimilarity && app.similarity != null" class="app-card__sim">{{ Math.round(app.similarity * 100) }}%</span>
    </div>
    <div class="app-card__body">
      <div class="app-card__head">
        <img v-if="app.iconUrl" :src="assetUrl(app.iconUrl)" class="app-card__icon" alt="" />
        <div class="app-card__icon app-card__icon--letter" v-else>{{ app.name.slice(0, 1).toUpperCase() }}</div>
        <div class="app-card__titles">
          <div class="app-card__name" :title="app.name">{{ app.name }}</div>
          <div class="app-card__cat">{{ name({ nameEn: app.categoryNameEn, nameTr: app.categoryNameTr }) }}</div>
        </div>
      </div>
      <p class="app-card__desc">{{ app.shortDescription }}</p>
      <div class="app-card__meta">
        <ScoreBadge :score="app.ratingAvg" :count="app.ratingCount" />
        <span class="app-card__downloads"><ElIcon><Download /></ElIcon>{{ formatCompact(app.downloadCount) }}</span>
        <PlatformBadges :platforms="app.platforms" :max="4" />
      </div>
      <div class="app-card__foot">
        <span class="app-card__license">{{ app.licenseSpdxId }}</span>
        <span v-if="app.llmModelName" class="app-card__model" :title="app.llmModelName">{{ app.llmModelName }}</span>
      </div>
    </div>
  </RouterLink>
</template>

<style scoped lang="scss">
.app-card {
  display: flex;
  flex-direction: column;
  background: var(--gm-card-bg);
  border: 1px solid var(--gm-border);
  border-radius: 12px;
  overflow: hidden;
  color: var(--gm-text);
  transition: box-shadow 0.15s, transform 0.15s;
  height: 100%;
  &:hover {
    box-shadow: var(--gm-shadow-hover);
    transform: translateY(-2px);
  }
  &__cover {
    position: relative;
    aspect-ratio: 16 / 9;
    background: var(--gm-page-bg);
    overflow: hidden;
    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      display: block;
    }
    &-empty {
      display: grid;
      place-items: center;
      height: 100%;
      color: var(--gm-text-muted);
    }
  }
  &__featured {
    position: absolute;
    top: 8px;
    left: 8px;
    background: var(--gm-yellow);
    color: #fff;
    border-radius: 6px;
    padding: 1px 6px;
    font-size: 12px;
    font-weight: 700;
  }
  &__sim {
    position: absolute;
    top: 8px;
    right: 8px;
    background: rgba(31, 111, 235, 0.92);
    color: #fff;
    border-radius: 6px;
    padding: 1px 7px;
    font-size: 12px;
    font-weight: 700;
  }
  &__body {
    padding: 12px 14px 12px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    flex: 1;
  }
  &__head {
    display: flex;
    gap: 10px;
    align-items: center;
  }
  &__icon {
    width: 40px;
    height: 40px;
    border-radius: 10px;
    object-fit: cover;
    flex-shrink: 0;
    &--letter {
      display: grid;
      place-items: center;
      background: var(--gm-primary-light);
      color: var(--gm-primary);
      font-weight: 800;
      font-size: 18px;
    }
  }
  &__titles {
    min-width: 0;
  }
  &__name {
    font-weight: 700;
    font-size: 15px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  &__cat {
    font-size: 12px;
    color: var(--gm-text-muted);
  }
  &__desc {
    margin: 0;
    font-size: 13px;
    color: var(--gm-text-muted);
    line-height: 1.45;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
    flex: 1;
  }
  &__meta {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 12px;
  }
  &__downloads {
    display: inline-flex;
    align-items: center;
    gap: 3px;
    color: var(--gm-text-muted);
  }
  &__foot {
    display: flex;
    justify-content: space-between;
    gap: 8px;
    font-size: 11px;
    color: var(--gm-text-muted);
  }
  &__license {
    padding: 1px 6px;
    border-radius: 4px;
    border: 1px solid var(--gm-border);
  }
  &__model {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  &.is-horizontal {
    flex-direction: row;
    .app-card__cover {
      width: 220px;
      aspect-ratio: auto;
      flex-shrink: 0;
    }
  }
}
</style>
