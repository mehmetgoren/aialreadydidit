<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { AccountService } from '@/utils/services/account-service'
import { enableAfter, notifyError, notifyS } from '@/utils/tools'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const password = ref('')
const busy = ref(false)

async function submit() {
  await enableAfter(busy, async () => {
    try {
      await new AccountService().resetPassword(String(route.query.token ?? ''), password.value)
      notifyS(t('password_reset_done'))
      await router.push('/login')
    } catch (err) {
      notifyError(err)
    }
  })
}
</script>

<template>
  <div class="gm-container" style="display: flex; justify-content: center; padding: 32px 16px">
    <div class="gm-card" style="width: 100%; max-width: 420px; padding: 24px 26px">
      <h2 class="gm-title">{{ t('reset_password') }}</h2>
      <ElForm label-position="top" @submit.prevent="submit">
        <ElFormItem :label="t('new_password')"><ElInput v-model="password" type="password" show-password size="large" autocomplete="new-password" /></ElFormItem>
        <ElButton type="primary" size="large" :loading="busy" :disabled="password.length < 8" native-type="submit" style="width: 100%">{{ t('save') }}</ElButton>
      </ElForm>
    </div>
  </div>
</template>
