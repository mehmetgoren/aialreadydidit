<script setup lang="ts">
import { nextTick, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { DASHBOARD_MENU } from '@/utils/dashboard-menu'
import { useUserStore } from '@/stores/user-store'

/** Member area: left menu + content (prototype: ProfileLayout). */
const route = useRoute()
const { t } = useI18n()
const user = useUserStore()

// phones show the menu as a horizontal strip: keep the active entry in view without touching the page scroll
const nav = ref<HTMLElement | null>(null)
async function revealActive() {
  await nextTick()
  const el = nav.value
  const active = el?.querySelector<HTMLElement>('.is-active')
  if (!el || !active || el.scrollWidth <= el.clientWidth) return
  el.scrollLeft = active.offsetLeft - (el.clientWidth - active.offsetWidth) / 2
}
onMounted(revealActive)
watch(() => route.path, revealActive)

function isActive(routePath: string): boolean {
  if (routePath === '/dashboard') return route.path === '/dashboard'
  return route.path.startsWith(routePath)
}
</script>

<template>
  <div class="gm-container dash">
    <div class="gm-card dash__card">
      <aside class="dash__side">
        <div class="dash__user">
          <ElAvatar :size="44" :src="user.me?.avatarUrl || undefined">{{ user.displayName.slice(0, 1).toUpperCase() }}</ElAvatar>
          <div>
            <div class="dash__name">{{ user.displayName }}</div>
            <div class="sub">@{{ user.me?.username }}</div>
          </div>
        </div>
        <nav ref="nav" class="gm-side-menu">
          <RouterLink v-for="item in DASHBOARD_MENU" :key="item.label" :to="item.route!" :class="{ 'is-active': isActive(item.route!) }">
            <ElIcon><component :is="item.icon || 'Document'" /></ElIcon>
            <span>{{ t(item.label) }}</span>
          </RouterLink>
        </nav>
      </aside>
      <section class="dash__content">
        <RouterView v-slot="{ Component }">
          <Transition name="fade" mode="out-in">
            <component :is="Component" />
          </Transition>
        </RouterView>
      </section>
    </div>
  </div>
</template>

<style scoped lang="scss">
.dash {
  padding: 20px 16px 32px;
  &__card {
    display: grid;
    grid-template-columns: 250px 1fr;
    min-height: 520px;
    overflow: hidden;
  }
  &__side {
    border-right: 1px solid var(--gm-border);
  }
  &__user {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 18px 20px 8px;
  }
  &__name {
    font-weight: 700;
  }
  &__content {
    padding: 22px 24px 28px;
    min-width: 0;
  }
  @media (max-width: 860px) {
    &__card {
      grid-template-columns: minmax(0, 1fr);
      grid-template-rows: auto 1fr; // the menu strip keeps its height, the page takes the rest
    }
    &__side {
      border-right: none;
      border-bottom: 1px solid var(--gm-border);
      min-width: 0;
    }
    // the menu becomes one swipeable strip of pills — a stacked list would push every page a screen down
    &__user {
      display: none;
    }
    .gm-side-menu {
      position: relative;
      display: flex;
      gap: 6px;
      padding: 10px 12px;
      overflow-x: auto;
      scrollbar-width: none;
      -webkit-overflow-scrolling: touch;
      &::-webkit-scrollbar {
        display: none;
      }
      a {
        flex: 0 0 auto;
        gap: 6px;
        padding: 7px 12px;
        border: 1px solid var(--gm-border);
        border-radius: 999px;
        font-size: 13px;
        white-space: nowrap;
        &.is-active {
          border-color: var(--gm-primary);
          color: var(--gm-primary);
        }
      }
    }
    &__content {
      padding: 16px 14px 22px;
    }
  }
  @media (max-width: 640px) {
    padding: 12px 10px 24px;
  }
}
</style>
