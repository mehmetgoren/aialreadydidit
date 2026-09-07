<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useUserStore } from '@/stores/user-store'
import SearchBox from './SearchBox.vue'
import ProfileDropdown from './ProfileDropdown.vue'
import NotificationBell from './NotificationBell.vue'
import LanguageSwitch from './LanguageSwitch.vue'

const { t } = useI18n()
const user = useUserStore()
</script>

<template>
  <header class="header">
    <div class="gm-container header__inner">
      <RouterLink to="/" class="header__logo">
        <span class="header__logo-mark">AI</span>
        <span class="header__logo-text">{{ t('app_name') }}</span>
      </RouterLink>
      <SearchBox class="header__search" />
      <div class="header__spacer" />
      <RouterLink to="/for-agents" class="header__link">{{ t('for_agents') }}</RouterLink>
      <RouterLink to="/wanted" class="header__link">{{ t('wanted') }}</RouterLink>
      <RouterLink to="/upload" class="header__upload">
        <ElIcon><Upload /></ElIcon>
        {{ t('upload_app') }}
      </RouterLink>
      <LanguageSwitch />
      <template v-if="user.isAuthenticated">
        <NotificationBell />
        <ProfileDropdown />
      </template>
      <RouterLink v-else to="/login" class="header__login">{{ t('login') }}</RouterLink>
    </div>
  </header>
</template>

<style lang="scss">
.header {
  background: var(--gm-card-bg);
  border-bottom: 1px solid var(--gm-border);
  position: sticky;
  top: 0;
  z-index: 20;
  &__inner {
    display: flex;
    align-items: center;
    gap: 14px;
    height: var(--gm-header-height);
  }
  &__logo {
    display: flex;
    align-items: center;
    gap: 8px;
    color: var(--gm-text);
    text-decoration: none;
    white-space: nowrap;
    &-mark {
      display: grid;
      place-items: center;
      width: 34px;
      height: 34px;
      border-radius: 9px;
      background: var(--gm-primary);
      color: #fff;
      font-weight: 800;
      font-size: 14px;
    }
    &-text {
      font-weight: 800;
      font-size: 17px;
      letter-spacing: -0.3px;
    }
  }
  &__search {
    flex: 1;
    max-width: 520px;
  }
  &__spacer {
    flex: 1;
  }
  &__link {
    color: var(--gm-text);
    font-weight: 500;
    white-space: nowrap;
    &:hover {
      color: var(--gm-primary);
    }
  }
  &__upload {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 8px 14px;
    border-radius: 8px;
    background: var(--gm-primary);
    color: #fff;
    font-weight: 600;
    white-space: nowrap;
    &:hover {
      background: var(--gm-primary-dark);
    }
  }
  &__login {
    font-weight: 600;
    color: var(--gm-text);
    white-space: nowrap;
  }
  @media (max-width: 900px) {
    &__link,
    &__logo-text {
      display: none;
    }
  }
}
</style>
