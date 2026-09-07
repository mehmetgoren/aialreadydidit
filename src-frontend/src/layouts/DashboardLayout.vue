<script setup lang="ts">
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { DASHBOARD_MENU } from '@/utils/dashboard-menu'
import { useUserStore } from '@/stores/user-store'

/** Member area: left menu + content (prototype: ProfileLayout). */
const route = useRoute()
const { t } = useI18n()
const user = useUserStore()

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
        <nav class="gm-side-menu">
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
      grid-template-columns: 1fr;
    }
    &__side {
      border-right: none;
      border-bottom: 1px solid var(--gm-border);
    }
  }
}
</style>
