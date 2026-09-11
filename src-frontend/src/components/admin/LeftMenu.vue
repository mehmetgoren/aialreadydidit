<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useCommonStore } from '@/stores/common-store'
import type { MenuItem } from '@/utils/models/common-models'

/**
 * Admin drawer menu (prototype: LeftMenu.vue). Items come from props so the admin feature can feed
 * backend-driven menus (getmenus + role access) or a static list.
 */
const props = defineProps<{ items: MenuItem[]; collapsed?: boolean }>()
const route = useRoute()
const router = useRouter()
const common = useCommonStore()
const { t } = useI18n()

const active = computed(() => route.path)
const openeds = computed(() => props.items.filter((i) => i.children?.length).map((i) => i.label))

function onClick(item: MenuItem) {
  if (!item.route) return
  common.setActiveMenu({ label: t(item.label), icon: item.icon, route: item.route })
  router.push(item.route)
}
</script>

<template>
  <ElMenu
    :default-active="active"
    :collapse="collapsed"
    :default-openeds="openeds"
    class="left-menu"
  >
    <template v-for="item in props.items" :key="item.label">
      <ElSubMenu v-if="item.children?.length" :index="item.label">
        <template #title>
          <ElIcon><component :is="item.icon || 'Menu'" /></ElIcon>
          <span>{{ t(item.label) }}</span>
        </template>
        <ElMenuItem
          v-for="child in item.children"
          :key="child.label"
          :index="child.route || child.label"
          @click="onClick(child)"
        >
          <ElIcon><component :is="child.icon || 'Document'" /></ElIcon>
          <span>{{ t(child.label) }}</span>
        </ElMenuItem>
      </ElSubMenu>
      <ElMenuItem v-else :index="item.route || item.label" @click="onClick(item)">
        <ElIcon><component :is="item.icon || 'Document'" /></ElIcon>
        <template #title>{{ t(item.label) }}</template>
      </ElMenuItem>
    </template>
  </ElMenu>
</template>

<style scoped>
.left-menu {
  border-right: none;
  min-height: 100%;
}
.left-menu:not(.el-menu--collapse) {
  width: 220px;
}
</style>
