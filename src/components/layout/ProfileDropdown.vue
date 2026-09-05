<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useUserStore } from '@/stores/user-store'

const { t } = useI18n()
const router = useRouter()
const user = useUserStore()

async function onCommand(cmd: string) {
  if (cmd === 'logout') {
    await user.signOut()
    return router.push('/')
  }
  return router.push(cmd)
}
</script>

<template>
  <ElDropdown trigger="click" @command="onCommand">
    <span class="profile">
      <ElAvatar :size="30" :src="user.me?.avatarUrl || undefined">{{ user.displayName.slice(0, 1).toUpperCase() }}</ElAvatar>
      <span class="profile__name">{{ user.displayName }}</span>
      <ElIcon><ArrowDown /></ElIcon>
    </span>
    <template #dropdown>
      <ElDropdownMenu>
        <ElDropdownItem command="/dashboard"><ElIcon><Odometer /></ElIcon>{{ t('dash_overview') }}</ElDropdownItem>
        <ElDropdownItem command="/dashboard/apps"><ElIcon><Box /></ElIcon>{{ t('dash_my_apps') }}</ElDropdownItem>
        <ElDropdownItem command="/dashboard/favorites"><ElIcon><Star /></ElIcon>{{ t('dash_favorites') }}</ElDropdownItem>
        <ElDropdownItem command="/dashboard/api-keys"><ElIcon><Key /></ElIcon>{{ t('dash_api_keys') }}</ElDropdownItem>
        <ElDropdownItem v-if="user.isAdmin || user.me?.role === 'Moderator'" command="/admin" divided><ElIcon><Setting /></ElIcon>{{ t('admin_panel') }}</ElDropdownItem>
        <ElDropdownItem command="/dashboard/settings" divided><ElIcon><User /></ElIcon>{{ t('dash_settings') }}</ElDropdownItem>
        <ElDropdownItem command="logout"><ElIcon><SwitchButton /></ElIcon>{{ t('logout') }}</ElDropdownItem>
      </ElDropdownMenu>
    </template>
  </ElDropdown>
</template>

<style scoped>
.profile {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  outline: none;
  color: var(--gm-text);
}
.profile__name {
  max-width: 140px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-weight: 600;
}
@media (max-width: 760px) {
  .profile__name {
    display: none;
  }
}
</style>
