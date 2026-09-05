<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { CollectionDto } from '@/utils/models/catalog-models'
import { DashboardService } from '@/utils/services/dashboard-service'
import { notifyError, notifyS } from '@/utils/tools'

/** "Add to collection" popover with inline "new collection". */
const props = defineProps<{ appId: number; selectedIds: number[] }>()
const emit = defineEmits<{ changed: [ids: number[]] }>()
const { t } = useI18n()
const service = new DashboardService()
const collections = ref<CollectionDto[]>([])
const selected = ref<number[]>([...props.selectedIds])
const newName = ref('')
const busy = ref(false)

watch(() => props.selectedIds, (v) => (selected.value = [...v]))

async function load() {
  try {
    collections.value = await service.collections()
  } catch {
    collections.value = []
  }
}
onMounted(load)

async function toggle(c: CollectionDto, on: boolean) {
  busy.value = true
  try {
    if (on) await service.addToCollection(c.id, props.appId)
    else await service.removeFromCollection(c.id, props.appId)
    selected.value = on ? [...selected.value, c.id] : selected.value.filter((x) => x !== c.id)
    emit('changed', selected.value)
  } catch (err) {
    notifyError(err)
  } finally {
    busy.value = false
  }
}

async function create() {
  if (!newName.value.trim()) return
  busy.value = true
  try {
    const c = await service.createCollection({ name: newName.value.trim(), isPublic: false })
    await service.addToCollection(c.id, props.appId)
    collections.value.unshift(c)
    selected.value = [...selected.value, c.id]
    emit('changed', selected.value)
    newName.value = ''
    notifyS(t('collection_created'))
  } catch (err) {
    notifyError(err)
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <ElPopover trigger="click" placement="bottom" :width="300">
    <template #reference>
      <ElButton><ElIcon><Collection /></ElIcon>{{ t('add_to_collection') }}</ElButton>
    </template>
    <div v-loading="busy" class="picker">
      <div v-if="!collections.length" class="gm-muted">{{ t('no_collections') }}</div>
      <label v-for="c in collections" :key="c.id" class="picker__row">
        <ElCheckbox :model-value="selected.includes(c.id)" @update:model-value="toggle(c, Boolean($event))">{{ c.name }}</ElCheckbox>
        <ElIcon v-if="c.isPublic" class="gm-muted"><View /></ElIcon>
      </label>
      <div class="picker__new">
        <ElInput v-model="newName" :placeholder="t('new_collection')" size="small" @keyup.enter="create" />
        <ElButton size="small" type="primary" @click="create">{{ t('create') }}</ElButton>
      </div>
    </div>
  </ElPopover>
</template>

<style scoped>
.picker__row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 2px 0;
}
.picker__new {
  display: flex;
  gap: 6px;
  margin-top: 8px;
}
</style>
