<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { ReportReason } from '@/utils/models/catalog-models'
import { AppsService } from '@/utils/services/apps-service'
import { useUserStore } from '@/stores/user-store'
import { enableAfter, notifyError, notifyS } from '@/utils/tools'

const props = defineProps<{ slug: string; ratingId?: number | null }>()
const visible = defineModel<boolean>({ default: false })
const { t } = useI18n()
const user = useUserStore()
const saving = ref(false)
const form = reactive<{ reason: ReportReason; details: string; email: string }>({ reason: 'broken', details: '', email: '' })
const reasons: ReportReason[] = ['malware', 'notOpenSource', 'notFree', 'copyright', 'broken', 'spam', 'inappropriate', 'other']

async function send() {
  await enableAfter(saving, async () => {
    try {
      await new AppsService().report(props.slug, { reason: form.reason, details: form.details || undefined, email: user.isAuthenticated ? undefined : form.email || undefined, ratingId: props.ratingId ?? null })
      notifyS(t('report_sent'))
      visible.value = false
      form.details = ''
    } catch (err) {
      notifyError(err)
    }
  })
}
</script>

<template>
  <ElDialog v-model="visible" :title="t('report_abuse')" width="480px">
    <ElForm label-position="top">
      <ElFormItem :label="t('report_reason')">
        <ElSelect v-model="form.reason" style="width: 100%">
          <ElOption v-for="r in reasons" :key="r" :value="r" :label="t(`report_reason_${r}`)" />
        </ElSelect>
      </ElFormItem>
      <ElFormItem :label="t('report_details')">
        <ElInput v-model="form.details" type="textarea" :rows="4" maxlength="4000" show-word-limit />
      </ElFormItem>
      <ElFormItem v-if="!user.isAuthenticated" :label="t('email_optional')">
        <ElInput v-model="form.email" type="email" />
      </ElFormItem>
    </ElForm>
    <template #footer>
      <ElButton @click="visible = false">{{ t('cancel') }}</ElButton>
      <ElButton type="danger" :loading="saving" @click="send">{{ t('send_report') }}</ElButton>
    </template>
  </ElDialog>
</template>
