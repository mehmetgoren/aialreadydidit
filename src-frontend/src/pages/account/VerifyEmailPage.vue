<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { AccountService } from '@/utils/services/account-service'
import { useUserStore } from '@/stores/user-store'
import { errorMessage } from '@/utils/tools'

const route = useRoute()
const { t } = useI18n()
const user = useUserStore()
const state = ref<'loading' | 'ok' | 'error'>('loading')
const message = ref('')

onMounted(async () => {
  try {
    await new AccountService().verifyEmail(String(route.query.token ?? ''))
    state.value = 'ok'
    user.refreshMe().catch(() => {})
  } catch (err) {
    state.value = 'error'
    message.value = errorMessage(err)
  }
})
</script>

<template>
  <div class="gm-container" style="display: flex; justify-content: center; padding: 32px 16px">
    <div class="gm-card" style="width: 100%; max-width: 480px; padding: 24px">
      <ElResult v-if="state === 'ok'" icon="success" :title="t('email_verified')"><template #extra><RouterLink to="/"><ElButton type="primary">{{ t('home') }}</ElButton></RouterLink></template></ElResult>
      <ElResult v-else-if="state === 'error'" icon="error" :title="t('email_verify_failed')" :sub-title="message" />
      <div v-else v-loading="true" style="height: 120px" />
    </div>
  </div>
</template>
