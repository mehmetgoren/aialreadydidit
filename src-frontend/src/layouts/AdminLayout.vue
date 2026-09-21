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
import { useMediaQuery } from '@/composables/use-media-query'

/** Prototype MainLayout: header + drawer with LeftMenu + router-view. Menu comes from the API (role → menus). */
const { t, locale } = useI18n()
const router = useRouter()
const route = useRoute()
const user = useUserStore()
const common = useCommonStore()
const collapsed = ref(false)
// phones / small tablets: the sidebar is an off-canvas drawer opened from the header button
const narrow = useMediaQuery('(max-width: 860px)')
const drawerOpen = ref(false)
function toggleMenu() {
  if (narrow.value) drawerOpen.value = !drawerOpen.value
  else collapsed.value = !collapsed.value
}
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
    drawerOpen.value = false
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
  <ElContainer class="admin" :class="{ 'admin--narrow': narrow, 'admin--drawer-open': narrow && drawerOpen }">
    <div v-if="narrow && drawerOpen" class="admin__backdrop" @click="drawerOpen = false" />
    <ElAside :width="narrow ? '264px' : collapsed ? '64px' : '230px'" class="admin__aside">
      <div class="admin__brand" @click="router.push('/admin')">
        <ElIcon :size="22"><Setting /></ElIcon>
        <span v-show="narrow || !collapsed">{{ t('admin_panel') }}</span>
      </div>
      <div class="admin__menu"><LeftMenu :items="menu" :collapsed="!narrow && collapsed" /></div>
    </ElAside>
    <ElContainer>
      <ElHeader class="admin__header">
        <div class="admin__left">
          <ElButton text circle @click="toggleMenu">
            <ElIcon :size="18"><component :is="(narrow ? !drawerOpen : collapsed) ? 'Expand' : 'Fold'" /></ElIcon>
          </ElButton>
          <span class="admin__title">{{ common.activeMenu?.label || t('admin_panel') }}</span>
        </div>
        <ElDropdown trigger="click" @command="onCommand">
          <span class="admin__user">
            <ElAvatar :size="28" :src="user.me?.avatarUrl || undefined">{{ user.displayName.slice(0, 1).toUpperCase() }}</ElAvatar>
            <span class="admin__user-name">{{ user.displayName }}</span>
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
    display: flex;
    flex-direction: column;
    background: var(--gm-card-bg);
    border-right: 1px solid var(--gm-border);
    transition: width 0.2s;
    overflow: hidden; // the brand row stays put; only the menu below scrolls
  }
  &__menu {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
    overflow-x: hidden;
    scrollbar-width: thin;
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
  &__backdrop {
    position: fixed;
    inset: 0;
    z-index: 29;
    background: rgba(0, 0, 0, 0.45);
  }
  &--narrow {
    .admin__aside {
      position: fixed;
      inset: 0 auto 0 0;
      z-index: 30;
      max-width: 84vw;
      transform: translateX(-100%);
      transition: transform 0.2s;
      box-shadow: var(--gm-shadow-hover);
    }
    .admin__header {
      padding: 0 10px;
    }
    .admin__left {
      min-width: 0;
    }
    .admin__title {
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .admin__user-name {
      display: none;
    }
    .admin__main {
      padding: 12px 10px;
    }
  }
  &--drawer-open .admin__aside {
    transform: none;
  }
}
// RTL: the drawer slides in from the right
[dir='rtl'] .admin--narrow .admin__aside {
  inset: 0 0 0 auto;
  transform: translateX(100%);
}
[dir='rtl'] .admin--drawer-open .admin__aside {
  transform: none;
}
</style>
