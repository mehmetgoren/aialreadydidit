<script setup lang="ts">
import { computed } from 'vue'
import { useSiteStore } from '@/stores/site-store'

const site = useSiteStore()
const text = computed(() => site.config?.announcementText)
const link = computed(() => site.config?.announcementLink)
</script>

<template>
  <div v-if="text" class="announce">
    <div class="gm-container announce__inner">
      <span>{{ text }}</span>
      <a v-if="link" :href="link" class="announce__link">→</a>
    </div>
  </div>
</template>

<style scoped lang="scss">
.announce {
  background: var(--gm-navy);
  color: #fff;
  font-size: 13px;
  &__inner {
    display: flex;
    justify-content: center;
    gap: 10px;
    padding: 6px 12px;
  }
  &__link {
    color: #fff;
    font-weight: 700;
  }
}
</style>
