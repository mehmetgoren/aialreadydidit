<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCategoryStore, useCategoryName } from '@/stores/category-store'
import type { CategoryNode } from '@/utils/models/catalog-models'

/**
 * Manual category choice for when LLM suggestions are off: one select per tree level
 * (category → sub-category → optional finer type). The value is the id of the deepest chosen node.
 */
const props = defineProps<{ modelValue: number | null; error?: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: number | null] }>()
const { t } = useI18n()
const categories = useCategoryStore()
const name = useCategoryName()

/** Root → … → selected node; empty when nothing is chosen. */
const path = computed(() => (props.modelValue ? categories.pathToId(props.modelValue) : []))

/** Levels to render: one per selected node plus the next (when the last chosen node has children). */
const levels = computed(() => {
  const out: { options: CategoryNode[]; selected: number | null }[] = []
  let options = categories.roots
  for (const node of path.value) {
    out.push({ options, selected: node.id })
    options = node.children
  }
  if (options.length && (out.length === 0 || path.value.at(-1)!.children.length)) out.push({ options, selected: null })
  return out
})

const labels = ['category', 'sub_category', 'category_type']

function choose(level: number, id: number | null) {
  // Choosing at a level replaces everything below it; clearing goes back to the parent.
  emit('update:modelValue', id ?? (level > 0 ? path.value[level - 1]!.id : null))
}
</script>

<template>
  <div class="picker" :class="{ 'is-error': error }">
    <div v-for="(level, i) in levels" :key="i" class="picker__level">
      <span class="picker__label">{{ t(labels[i] ?? 'category_type') }}<span v-if="i < 2" class="picker__required">*</span></span>
      <ElSelect :model-value="level.selected" filterable default-first-option :clearable="i > 0" :placeholder="t('select_placeholder')" style="width: 100%" @update:model-value="choose(i, $event as number | null)">
        <ElOption v-for="n in level.options" :key="n.id" :value="n.id" :label="name(n)" />
      </ElSelect>
    </div>
    <div class="picker__hint" :class="{ 'gm-muted': !error }">{{ error || t('category_manual_hint') }}</div>
  </div>
</template>

<style scoped>
.picker {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 8px 12px;
  width: 100%;
}
.picker__level {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.picker__label {
  font-size: 12px;
  line-height: 1.2;
  color: var(--gm-text-muted);
}
.picker__required {
  color: var(--el-color-danger);
  margin-left: 2px;
}
.picker__hint {
  grid-column: 1 / -1;
  font-size: 12px;
  line-height: 1.4;
}
.is-error .picker__hint {
  color: var(--el-color-danger);
}
</style>
