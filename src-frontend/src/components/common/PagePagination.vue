<script setup lang="ts">
defineProps<{ page: number; pageSize: number; total: number }>()
const emit = defineEmits<{ 'update:page': [value: number] }>()
</script>

<template>
  <div v-if="total > pageSize" class="page-pagination">
    <ElPagination :current-page="page" :page-size="pageSize" :total="total" layout="prev, pager, next" background @update:current-page="emit('update:page', $event)" />
  </div>
</template>

<style scoped>
.page-pagination {
  display: flex;
  justify-content: center;
  margin-top: 20px;
}
</style>
