<script setup lang="ts">
import { computed, ref } from 'vue'
import type { ScreenshotDto } from '@/utils/models/catalog-models'
import { assetUrl } from '@/utils/tools'

const props = defineProps<{ screenshots: ScreenshotDto[] }>()
const active = ref(0)
const preview = ref(false)
const urls = computed(() => props.screenshots.map((s) => assetUrl(s.url)))
const current = computed(() => props.screenshots[active.value])
</script>

<template>
  <div v-if="screenshots.length" class="gallery">
    <div class="gallery__main" @click="preview = true">
      <img :src="urls[active]" :alt="current?.caption || ''" />
      <div v-if="current?.caption" class="gallery__caption">{{ current.caption }}</div>
    </div>
    <div v-if="screenshots.length > 1" class="gallery__thumbs">
      <button v-for="(s, i) in screenshots" :key="s.id" class="gallery__thumb" :class="{ 'is-active': i === active }" type="button" @click="active = i">
        <img :src="assetUrl(s.thumbUrl)" :alt="s.caption || ''" loading="lazy" />
      </button>
    </div>
    <ElImageViewer v-if="preview" :url-list="urls" :initial-index="active" teleported @close="preview = false" />
  </div>
</template>

<style scoped lang="scss">
.gallery {
  &__main {
    position: relative;
    border-radius: 12px;
    overflow: hidden;
    background: var(--gm-page-bg);
    border: 1px solid var(--gm-border);
    cursor: zoom-in;
    img {
      width: 100%;
      max-height: 520px;
      object-fit: contain;
      display: block;
    }
  }
  &__caption {
    position: absolute;
    left: 0;
    right: 0;
    bottom: 0;
    padding: 6px 12px;
    background: rgba(0, 0, 0, 0.55);
    color: #fff;
    font-size: 12px;
  }
  &__thumbs {
    display: flex;
    gap: 8px;
    margin-top: 10px;
    overflow-x: auto;
  }
  &__thumb {
    flex-shrink: 0;
    width: 96px;
    height: 60px;
    padding: 0;
    border: 2px solid transparent;
    border-radius: 8px;
    overflow: hidden;
    background: var(--gm-page-bg);
    cursor: pointer;
    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
    &.is-active {
      border-color: var(--gm-primary);
    }
  }
}
</style>
