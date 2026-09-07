<script setup lang="ts">
import type { AppVersionDto } from '@/utils/models/catalog-models'
import MarkdownView from '@/components/common/MarkdownView.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { formatBytes, formatDate } from '@/utils/format'
import { platformIcon } from '@/utils/tools'

withDefaults(defineProps<{ versions: AppVersionDto[]; showStatus?: boolean }>(), { showStatus: false })
</script>

<template>
  <ElTimeline class="versions">
    <ElTimelineItem v-for="v in versions" :key="v.id" :timestamp="formatDate(v.releasedAt)" placement="top">
      <div class="versions__head">
        <strong>{{ $t('version') }} {{ v.version }}</strong>
        <StatusTag v-if="showStatus" :value="v.status" />
        <span v-if="v.sourceRef" class="gm-muted">· {{ v.sourceRef }}</span>
        <span class="gm-muted">· {{ v.downloadCount }} {{ $t('downloads').toLowerCase() }}</span>
      </div>
      <MarkdownView v-if="v.changelog" :source="v.changelog" class="versions__changelog" />
      <div class="versions__files">
        <span v-for="f in v.files" :key="f.id" class="versions__file">{{ platformIcon(f.platformCode) }} {{ f.fileName }} <small class="gm-muted">{{ formatBytes(f.sizeBytes) }}</small></span>
      </div>
    </ElTimelineItem>
  </ElTimeline>
</template>

<style scoped lang="scss">
.versions {
  padding-left: 4px;
  &__head {
    display: flex;
    gap: 8px;
    align-items: center;
    flex-wrap: wrap;
  }
  &__changelog {
    font-size: 13px;
    margin: 6px 0;
  }
  &__files {
    display: flex;
    flex-wrap: wrap;
    gap: 6px 14px;
    font-size: 12px;
  }
}
</style>
