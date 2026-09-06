<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { SessionDto } from '@/utils/models/user-models'
import { AccountService } from '@/utils/services/account-service'
import { useUserStore } from '@/stores/user-store'
import { useCommonStore } from '@/stores/common-store'
import { setLocale, SUPPORTED_LOCALES, type AppLocale } from '@/boot/i18n'
import { confirmX, enableAfter, notifyError, notifyS } from '@/utils/tools'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
const user = useUserStore()
const common = useCommonStore()
const accounts = new AccountService()
const profile = reactive({ displayName: '', bio: '', website: '', locale: 'en-US' })
const password = reactive({ current: '', next: '' })
const sessions = ref<SessionDto[]>([])
const saving = ref(false)
const savingPw = ref(false)

onMounted(async () => {
  common.setPageTitle(t('dash_settings'))
  const me = user.me ?? (await user.refreshMe())
  if (me) Object.assign(profile, { displayName: me.displayName, bio: me.bio ?? '', website: me.website ?? '', locale: me.locale })
  sessions.value = await accounts.sessions().catch(() => [])
})

async function saveProfile() {
  await enableAfter(saving, async () => {
    try {
      const me = await accounts.updateProfile({ displayName: profile.displayName, bio: profile.bio || null, website: profile.website || null, locale: profile.locale })
      user.patchMe(me)
      void setLocale(profile.locale as AppLocale)
      notifyS(t('saved'))
    } catch (err) {
      notifyError(err)
    }
  })
}
async function savePassword() {
  await enableAfter(savingPw, async () => {
    try {
      await accounts.changePassword(user.me?.hasPassword ? password.current : null, password.next)
      password.current = ''
      password.next = ''
      user.patchMe({ hasPassword: true })
      notifyS(t('saved'))
    } catch (err) {
      notifyError(err)
    }
  })
}
async function resend() {
  try {
    await accounts.resendVerification()
    notifyS(t('verification_sent'))
  } catch (err) {
    notifyError(err)
  }
}
async function revoke(s: SessionDto) {
  try {
    await accounts.revokeSession(s.id)
    sessions.value = sessions.value.filter((x) => x.id !== s.id)
  } catch (err) {
    notifyError(err)
  }
}
async function revokeAll() {
  if (!(await confirmX(t('confirm_revoke_all_sessions')))) return
  await accounts.revokeAllSessions().catch(notifyError)
  await user.signOut()
  location.href = '/login'
}
</script>

<template>
  <div class="settings">
    <h2 class="gm-title">{{ t('dash_settings') }}</h2>
    <section class="settings__box">
      <h3>{{ t('profile') }}</h3>
      <ElAlert v-if="user.me && !user.me.emailVerified" type="warning" show-icon :closable="false" style="margin-bottom: 12px"><template #title>{{ t('email_not_verified') }} <ElButton size="small" text type="primary" @click="resend">{{ t('resend_verification') }}</ElButton></template></ElAlert>
      <ElForm label-position="top" class="grid-form">
        <ElFormItem :label="t('email')"><ElInput :model-value="user.me?.email" disabled /></ElFormItem>
        <ElFormItem :label="t('username')"><ElInput :model-value="user.me?.username" disabled /></ElFormItem>
        <ElFormItem :label="t('display_name')"><ElInput v-model="profile.displayName" maxlength="80" /></ElFormItem>
        <ElFormItem :label="t('language')"><ElSelect v-model="profile.locale"><ElOption v-for="l in SUPPORTED_LOCALES" :key="l.value" :value="l.value" :label="l.label" /></ElSelect></ElFormItem>
        <ElFormItem :label="t('website')" class="full"><ElInput v-model="profile.website" placeholder="https://" /></ElFormItem>
        <ElFormItem :label="t('bio')" class="full"><ElInput v-model="profile.bio" type="textarea" :rows="3" maxlength="1000" show-word-limit /></ElFormItem>
      </ElForm>
      <ElButton type="primary" :loading="saving" @click="saveProfile">{{ t('save') }}</ElButton>
    </section>
    <section class="settings__box">
      <h3>{{ t('password') }}</h3>
      <ElForm label-position="top" class="grid-form">
        <ElFormItem v-if="user.me?.hasPassword" :label="t('current_password')"><ElInput v-model="password.current" type="password" show-password autocomplete="current-password" /></ElFormItem>
        <ElFormItem :label="t('new_password')"><ElInput v-model="password.next" type="password" show-password autocomplete="new-password" /></ElFormItem>
      </ElForm>
      <ElButton :loading="savingPw" :disabled="password.next.length < 8" @click="savePassword">{{ user.me?.hasPassword ? t('change_password') : t('set_password') }}</ElButton>
      <span v-if="user.me?.hasGoogle" class="sub" style="margin-left: 10px">{{ t('google_linked') }}</span>
    </section>
    <section class="settings__box">
      <div class="gm-section__head"><h3>{{ t('sessions') }}</h3><ElButton size="small" type="danger" plain @click="revokeAll">{{ t('sign_out_everywhere') }}</ElButton></div>
      <ElTable :data="sessions" size="small" class="gm-table">
        <ElTableColumn :label="t('created')" width="160"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></ElTableColumn>
        <ElTableColumn :label="t('last_used')" width="160"><template #default="{ row }">{{ row.lastUsedAt ? formatDateTime(row.lastUsedAt) : '—' }}</template></ElTableColumn>
        <ElTableColumn label="IP" prop="ip" width="140" />
        <ElTableColumn :label="t('device')" prop="userAgent" show-overflow-tooltip />
        <ElTableColumn width="120" align="right"><template #default="{ row }"><ElTag v-if="row.isCurrent" size="small" type="success">{{ t('current') }}</ElTag><ElButton v-else size="small" text type="danger" @click="revoke(row as SessionDto)">{{ t('revoke') }}</ElButton></template></ElTableColumn>
      </ElTable>
    </section>
  </div>
</template>

<style scoped>
.settings__box { padding: 16px 0 20px; border-bottom: 1px solid var(--gm-border); }
.settings__box h3 { margin: 0 0 12px; }
</style>
