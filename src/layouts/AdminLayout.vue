<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import LeftMenu from '@/components/admin/LeftMenu.vue'
import { useUserStore } from '@/stores/user-store'
import { useCommonStore } from '@/stores/common-store'
import { isDark, toggleDark } from '@/boot/theme'
import { SUPPORTED_LOCALES, setLocale, type AppLocale } from '@/boot/i18n'
import { ADMIN_MENU } from '@/pages/admin/admin-menu'
import { AdminPanelService } from '@/utils/services/admin-service'
import type { MenuItem } from '@/utils/models/common-models'
import type { AdminMenuItemDto } from '@/utils/models/admin-models'

/** Prototype MainLayout: header + drawer with LeftMenu + router-view. Menu comes from the API (role → menus). */
const { t, locale } = useI18n()
const router = useRouter()
const route = useRoute()
const user = useUserStore()
const common = useCommonStore()
const collapsed = ref(false)
const menu = ref<MenuItem[]>(ADMIN_MENU)

function toMenu(items: AdminMenuItemDto[]): MenuItem[] {
  return items.map((i) => ({ label: i.label, icon: i.icon ?? undefined, route: i.route ?? undefined, children: i.children.length ? toMenu(i.children) : undefined }))
}

onMounted(async () => {
  try {
    const items = await new AdminPanelService().menu()
    if (items.length) menu.value = toMenu(items)
  } catch {
    /* fall back to the static menu */
  }
})

watch(
  () => route.fullPath,
  () => {
    const key = route.meta.titleKey
    if (key) {
      common.setActiveMenu({ label: t(key), route: route.path })
      common.setPageTitle(t(key))
    }
  },
  { immediate: true },
)

async function onCommand(cmd: string) {
  if (cmd === 'logout') {
    await user.signOut()
    return router.push('/login')
  }
  if (cmd === 'theme') return toggleDark()
  if (cmd === 'storefront') return router.push('/')
  if (cmd.startsWith('lang:')) return setLocale(cmd.slice(5) as AppLocale)
}
</script>

<template>
  <ElContainer class="admin">
    <ElAside :width="collapsed ? '64px' : '230px'" class="admin__aside">
      <div class="admin__brand" @click="router.push('/admin')">
        <ElIcon :size="22"><Setting /></ElIcon>
        <span v-show="!collapsed">{{ t('admin_panel') }}</span>
      </div>
      <LeftMenu :items="menu" :collapsed="collapsed" />
    </ElAside>
    <ElContainer>
      <ElHeader class="admin__header">
        <div class="admin__left">
          <ElButton text circle @click="collapsed = !collapsed">
            <ElIcon :size="18"><component :is="collapsed ? 'Expand' : 'Fold'" /></ElIcon>
          </ElButton>
          <span class="admin__title">{{ common.activeMenu?.label || t('admin_panel') }}</span>
        </div>
        <ElDropdown trigger="click" @command="onCommand">
          <span class="admin__user">
            <ElAvatar :size="28" :src="user.me?.avatarUrl || undefined">{{ user.displayName.slice(0, 1).toUpperCase() }}</ElAvatar>
            <span>{{ user.displayName }}</span>
            <ElIcon><ArrowDown /></ElIcon>
          </span>
          <template #dropdown>
            <ElDropdownMenu>
              <ElDropdownItem command="storefront"><ElIcon><Shop /></ElIcon>{{ t('home') }}</ElDropdownItem>
              <ElDropdownItem v-for="l in SUPPORTED_LOCALES" :key="l.value" :command="`lang:${l.value}`" :disabled="locale === l.value">
                <ElIcon><Flag /></ElIcon>{{ l.label }}
              </ElDropdownItem>
              <ElDropdownItem command="theme" divided>
                <ElIcon><component :is="isDark ? 'Sunny' : 'Moon'" /></ElIcon>{{ isDark ? t('light_theme') : t('dark_theme') }}
              </ElDropdownItem>
              <ElDropdownItem command="logout" divided><ElIcon><SwitchButton /></ElIcon>{{ t('logout') }}</ElDropdownItem>
            </ElDropdownMenu>
          </template>
        </ElDropdown>
      </ElHeader>
      <ElMain class="admin__main">
        <RouterView />
      </ElMain>
    </ElContainer>
  </ElContainer>
</template>

<style scoped lang="scss">
.admin {
  height: 100%;
  &__aside {
    background: var(--gm-card-bg);
    border-right: 1px solid var(--gm-border);
    transition: width 0.2s;
    overflow: hidden;
  }
  &__brand {
    display: flex;
    align-items: center;
    gap: 10px;
    height: 56px;
    padding: 0 20px;
    font-weight: 700;
    color: var(--gm-primary);
    cursor: pointer;
    white-space: nowrap;
  }
  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    height: 56px;
    background: var(--gm-card-bg);
    border-bottom: 1px solid var(--gm-border);
  }
  &__left {
    display: flex;
    align-items: center;
    gap: 10px;
  }
  &__title {
    font-weight: 600;
  }
  &__user {
    display: flex;
    align-items: center;
    gap: 8px;
    cursor: pointer;
    outline: none;
  }
  &__main {
    overflow: auto;
  }
}
</style>
