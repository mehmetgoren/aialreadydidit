<script setup lang="ts">
import type { LineageNode } from '@/utils/models/catalog-models'
import { assetUrl } from '@/utils/tools'

/** Recursive "derived from / forks" tree. */
defineProps<{ node: LineageNode; currentId: number }>()
</script>

<template>
  <ul class="lineage">
    <li>
      <RouterLink :to="`/app/${node.slug}`" class="lineage__node" :class="{ 'is-current': node.id === currentId }">
        <img v-if="node.iconUrl" :src="assetUrl(node.iconUrl)" alt="" />
        <span>{{ node.name }}</span>
        <ElTag v-if="node.derivationKind" size="small" effect="plain">{{ $t(`derivation_${node.derivationKind}`) }}</ElTag>
      </RouterLink>
      <LineageTree v-for="child in node.derivatives" :key="child.id" :node="child" :current-id="currentId" />
    </li>
  </ul>
</template>

<style scoped lang="scss">
.lineage {
  list-style: none;
  margin: 0;
  padding-left: 18px;
  border-left: 1px dashed var(--gm-border);
  &__node {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 4px 8px;
    margin: 3px 0;
    border-radius: 6px;
    color: var(--gm-text);
    img {
      width: 20px;
      height: 20px;
      border-radius: 5px;
    }
    &.is-current {
      background: var(--gm-primary-light);
      font-weight: 700;
    }
    &:hover {
      background: var(--gm-page-bg);
    }
  }
}
</style>
