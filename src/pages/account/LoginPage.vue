<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { FormInstance } from 'element-plus'
import { AccountService } from '@/utils/services/account-service'
import { useUserStore } from '@/stores/user-store'
import { useSiteStore } from '@/stores/site-store'
import { useCommonStore } from '@/stores/common-store'
import { createRules, email, minLength, required } from '@/utils/validation/validation'
import { enableAfter, notifyError, notifyS } from '@/utils/tools'

declare global {
  interface Window {
    google?: { accounts: { id: { initialize: (o: object) => void; renderButton: (el: HTMLElement, o: object) => void } } }
  }
}

/** Sign-in / sign-up with e-mail + password, plus Google Identity Services when a client id is configured. */
const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()
const user = useUserStore()
const site = useSiteStore()
const common = useCommonStore()
const accounts = new AccountService()

const mode = ref<'login' | 'signup'>(route.path === '/signup' ? 'signup' : 'login')
watch(() => route.path, (p) => (mode.value = p === '/signup' ? 'signup' : 'login'))
const redirect = computed(() => (typeof route.query.redirect === 'string' ? route.query.redirect : '/'))

const loginFormRef = ref<FormInstance>()
const signupFormRef = ref<FormInstance>()
const busy = ref(false)
const loginForm = reactive({ login: '', password: '', rememberMe: true })
const signupForm = reactive({ email: '', username: '', displayName: '', password: '' })
const loginRules = createRules({ login: [required()], password: [required()] })
const signupRules = createRules({
  email: [required(), email()],
  username: [required(), minLength(3)],
  password: [required(), minLength(8)],
})
const googleEl = ref<HTMLElement | null>(null)

onMounted(async () => {
  common.setPageTitle(t(mode.value))
  await site.ensureLoaded()
  if (site.googleClientId) loadGoogle()
})

function loadGoogle() {
  const render = () => {
    if (!window.google || !googleEl.value) return
    window.google.accounts.id.initialize({ client_id: site.googleClientId, callback: onGoogle, ux_mode: 'popup' })
    window.google.accounts.id.renderButton(googleEl.value, { theme: 'outline', size: 'large', width: 320, text: 'continue_with', locale: String(locale.value).slice(0, 2) })
  }
  if (window.google) return render()
  const script = document.createElement('script')
  script.src = 'https://accounts.google.com/gsi/client'
  script.async = true
  script.onload = render
  document.head.appendChild(script)
}

async function onGoogle(response: { credential: string }) {
  try {
    user.applyAuth(await accounts.google(response.credential, String(locale.value)))
    await router.push(redirect.value)
  } catch (err) {
    notifyError(err)
  }
}

async function submitLogin() {
  if (!(await loginFormRef.value?.validate().catch(() => false))) return
  await enableAfter(busy, async () => {
    try {
      user.applyAuth(await accounts.signIn({ ...loginForm }))
      notifyS(t('welcome_back'))
      await router.push(redirect.value)
    } catch (err) {
      notifyError(err)
    }
  })
}

async function submitSignup() {
  if (!(await signupFormRef.value?.validate().catch(() => false))) return
  await enableAfter(busy, async () => {
    try {
      user.applyAuth(await accounts.signUp({ ...signupForm, locale: String(locale.value) }))
      notifyS(t('signup_done'))
      await router.push(redirect.value)
    } catch (err) {
      notifyError(err)
    }
  })
}
</script>

<template>
  <div class="gm-container auth">
    <div class="gm-card auth__card">
      <div class="auth__tabs">
        <RouterLink :to="{ path: '/login', query: route.query }" :class="{ 'is-active': mode === 'login' }">{{ t('login') }}</RouterLink>
        <RouterLink :to="{ path: '/signup', query: route.query }" :class="{ 'is-active': mode === 'signup' }">{{ t('signup') }}</RouterLink>
      </div>

      <ElForm v-if="mode === 'login'" ref="loginFormRef" :model="loginForm" :rules="loginRules" label-position="top" @submit.prevent="submitLogin">
        <ElFormItem :label="t('email_or_username')" prop="login"><ElInput v-model.trim="loginForm.login" autocomplete="username" size="large" /></ElFormItem>
        <ElFormItem :label="t('password')" prop="password"><ElInput v-model="loginForm.password" type="password" show-password autocomplete="current-password" size="large" @keyup.enter="submitLogin" /></ElFormItem>
        <div class="auth__row">
          <ElCheckbox v-model="loginForm.rememberMe">{{ t('remember_me') }}</ElCheckbox>
          <RouterLink to="/forgot-password" class="gm-link">{{ t('forgot_password') }}</RouterLink>
        </div>
        <ElButton type="primary" size="large" :loading="busy" native-type="submit" class="auth__submit">{{ t('login') }}</ElButton>
      </ElForm>

      <ElForm v-else ref="signupFormRef" :model="signupForm" :rules="signupRules" label-position="top" @submit.prevent="submitSignup">
        <ElFormItem :label="t('email')" prop="email"><ElInput v-model.trim="signupForm.email" type="email" autocomplete="email" size="large" /></ElFormItem>
        <ElFormItem :label="t('username')" prop="username"><ElInput v-model.trim="signupForm.username" autocomplete="username" size="large" /></ElFormItem>
        <ElFormItem :label="t('display_name')"><ElInput v-model.trim="signupForm.displayName" size="large" /></ElFormItem>
        <ElFormItem :label="t('password')" prop="password"><ElInput v-model="signupForm.password" type="password" show-password autocomplete="new-password" size="large" /></ElFormItem>
        <ElButton type="primary" size="large" :loading="busy" native-type="submit" class="auth__submit">{{ t('signup') }}</ElButton>
        <p class="sub auth__terms">{{ t('signup_terms') }}</p>
      </ElForm>

      <template v-if="site.googleClientId">
        <div class="auth__or"><span>{{ t('or') }}</span></div>
        <div ref="googleEl" class="auth__google" />
      </template>
      <p class="sub auth__anon">{{ t('anonymous_ok') }}</p>
    </div>
  </div>
</template>

<style scoped lang="scss">
.auth {
  display: flex;
  justify-content: center;
  padding: 32px 16px;
  &__card {
    width: 100%;
    max-width: 420px;
    padding: 24px 26px 20px;
  }
  &__tabs {
    display: flex;
    margin-bottom: 20px;
    border-bottom: 1px solid var(--gm-border);
    a {
      flex: 1;
      text-align: center;
      padding: 10px;
      color: var(--gm-text-muted);
      font-weight: 600;
      border-bottom: 3px solid transparent;
      margin-bottom: -1px;
      &.is-active {
        color: var(--gm-text);
        border-bottom-color: var(--gm-primary);
      }
    }
  }
  &__row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 14px;
  }
  &__submit {
    width: 100%;
  }
  &__terms, &__anon {
    text-align: center;
    margin: 12px 0 0;
  }
  &__or {
    display: flex;
    align-items: center;
    gap: 10px;
    margin: 18px 0 12px;
    color: var(--gm-text-muted);
    font-size: 12px;
    &::before, &::after { content: ''; flex: 1; border-top: 1px solid var(--gm-border); }
  }
  &__google {
    display: flex;
    justify-content: center;
    min-height: 44px;
  }
}
</style>
