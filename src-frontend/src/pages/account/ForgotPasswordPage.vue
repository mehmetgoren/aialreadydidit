<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { AccountService } from '@/utils/services/account-service'
import { enableAfter, notifyError } from '@/utils/tools'

const { t } = useI18n()
const email = ref('')
const busy = ref(false)
const sent = ref(false)

async function submit() {
  await enableAfter(busy, async () => {
    try {
      await new AccountService().forgotPassword(email.value.trim())
      sent.value = true
    } catch (err) {
      notifyError(err)
    }
  })
}
</script>

<template>
  <div class="gm-container" style="display: flex; justify-content: center; padding: 32px 16px">
    <div class="gm-card" style="width: 100%; max-width: 420px; padding: 24px 26px">
      <h2 class="gm-title">{{ t('forgot_password') }}</h2>
      <ElResult v-if="sent" icon="success" :title="t('reset_mail_sent')" :sub-title="t('reset_mail_sent_hint')" />
      <ElForm v-else label-position="top" @submit.prevent="submit">
        <ElFormItem :label="t('email')"><ElInput v-model.trim="email" type="email" size="large" /></ElFormItem>
        <ElButton type="primary" size="large" :loading="busy" native-type="submit" style="width: 100%">{{ t('send_reset_link') }}</ElButton>
      </ElForm>
    </div>
  </div>
</template>
