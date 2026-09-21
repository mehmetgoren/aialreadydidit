<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user-store'
import SearchBox from './SearchBox.vue'
import ProfileDropdown from './ProfileDropdown.vue'
import NotificationBell from './NotificationBell.vue'
import LanguageSwitch from './LanguageSwitch.vue'

const { t } = useI18n()
const router = useRouter()
const user = useUserStore()

// phones: the text links collapse into this menu
function onMenu(path: string) {
  void router.push(path)
}
</script>

<template>
  <header class="header" :class="{ 'header--auth': user.isAuthenticated }">
    <div class="gm-container header__inner">
      <RouterLink to="/" class="header__logo">
        <span class="header__logo-mark">AI</span>
        <span class="header__logo-text">{{ t('app_name') }}</span>
      </RouterLink>
      <SearchBox class="header__search" />
      <div class="header__spacer" />
      <RouterLink to="/for-agents" class="header__link">{{ t('for_agents') }}</RouterLink>
      <RouterLink to="/wanted" class="header__link">{{ t('wanted') }}</RouterLink>
      <RouterLink to="/upload" class="header__upload" :title="t('upload_app')" :aria-label="t('upload_app')">
        <ElIcon><Upload /></ElIcon>
        <span class="header__upload-text">{{ t('upload_app') }}</span>
      </RouterLink>
      <LanguageSwitch />
      <ElDropdown class="header__menu" trigger="click" @command="onMenu">
        <span class="header__menu-btn" :aria-label="t('menu')"><svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><path d="M4 7h16M4 12h16M4 17h16" /></svg></span>
        <template #dropdown>
          <ElDropdownMenu>
            <ElDropdownItem command="/for-agents"><ElIcon><Cpu /></ElIcon>{{ t('for_agents') }}</ElDropdownItem>
            <ElDropdownItem command="/wanted"><ElIcon><ChatLineSquare /></ElIcon>{{ t('wanted') }}</ElDropdownItem>
            <ElDropdownItem command="/upload"><ElIcon><Upload /></ElIcon>{{ t('upload_app') }}</ElDropdownItem>
            <ElDropdownItem command="/about" divided><ElIcon><InfoFilled /></ElIcon>{{ t('about') }}</ElDropdownItem>
          </ElDropdownMenu>
        </template>
      </ElDropdown>
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
    // takes the free space; the spacer only keeps a small gap so the box no longer shares half the room with it
    flex: 1 1 320px;
    min-width: 240px;
    max-width: 720px;
  }
  &__spacer {
    flex: 0 1 24px;
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
  &__menu {
    display: none;
  }
  &__menu-btn {
    display: grid;
    place-items: center;
    width: 36px;
    height: 36px;
    border-radius: 8px;
    color: var(--gm-text);
    cursor: pointer;
    outline: none;
  }
  @media (max-width: 900px) {
    &__link,
    &__logo-text {
      display: none;
    }
    &__menu {
      display: inline-flex;
    }
  }
  // phones: brand + actions on the first row, the search box gets a full row of its own
  @media (max-width: 640px) {
    &__inner {
      flex-wrap: wrap;
      height: auto;
      gap: 8px 10px;
      padding-top: 8px;
      padding-bottom: 10px;
    }
    // basis 0: the brand takes what the actions leave and ellipsizes, so nothing wraps except the search row
    &__logo {
      min-width: 0;
      flex: 1 1 0;
    }
    &__logo-text {
      display: block;
      font-size: 15px;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    &__spacer {
      display: none;
    }
    &__search {
      order: 10;
      flex: 1 1 100%;
      min-width: 0;
      max-width: none;
    }
    &__upload {
      padding: 9px;
      &-text {
        display: none;
      }
    }
    // signed in, the bell and the avatar join the row: "Publish an app" stays reachable from the menu
    &--auth &__upload {
      display: none;
    }
  }
  // small phones: the brand name matters more than a second way to reach "Publish an app"
  @media (max-width: 400px) {
    &__upload {
      display: none;
    }
    &__logo-text {
      font-size: 14px;
    }
  }
}
</style>
