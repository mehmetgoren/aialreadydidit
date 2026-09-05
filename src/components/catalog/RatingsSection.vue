<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AppDetail, RatingDto, RatingSummary } from '@/utils/models/catalog-models'
import { AppsService } from '@/utils/services/apps-service'
import { useUserStore } from '@/stores/user-store'
import ScoreBadge from '@/components/common/ScoreBadge.vue'
import PagePagination from '@/components/common/PagePagination.vue'
import { confirmX, enableAfter, notifyError, notifyS } from '@/utils/tools'
import { fromNow } from '@/utils/format'

/** Ratings out of 100 + reviews + "worked / didn't work"; only members who downloaded may rate. */
const props = defineProps<{ app: AppDetail }>()
const emit = defineEmits<{ report: [ratingId: number]; changed: [] }>()
const { t } = useI18n()
const user = useUserStore()
const service = new AppsService()

const summary = ref<RatingSummary | null>(null)
const rows = ref<RatingDto[]>([])
const total = ref(0)
const page = ref(1)
const sort = ref('helpful')
const loading = ref(false)
const saving = ref(false)
const showForm = ref(false)
const replyFor = ref<number | null>(null)
const replyText = ref('')
const form = reactive({ score: 80, review: '', versionId: null as number | null, worked: 'unknown' as 'yes' | 'no' | 'unknown' })

const canRate = computed(() => user.isAuthenticated && props.app.viewer.canRate)
const mine = computed(() => rows.value.find((r) => r.isMine))
const bars = computed(() => {
  const h = summary.value?.histogram ?? [0, 0, 0, 0, 0]
  const max = Math.max(1, ...h)
  return [4, 3, 2, 1, 0].map((i) => ({ label: `${i * 20}–${i * 20 + 19 + (i === 4 ? 1 : 0)}`, value: h[i] ?? 0, pct: ((h[i] ?? 0) / max) * 100 }))
})

async function load() {
  loading.value = true
  try {
    const [s, p] = await Promise.all([service.ratingSummary(props.app.slug), service.ratings(props.app.slug, sort.value, page.value)])
    summary.value = s
    rows.value = p.items
    total.value = p.totalCount
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}
onMounted(load)
watch([page, sort], load)

function openForm() {
  if (mine.value) Object.assign(form, { score: mine.value.score, review: mine.value.review ?? '', worked: mine.value.worked === true ? 'yes' : mine.value.worked === false ? 'no' : 'unknown' })
  form.versionId = props.app.latestVersion?.id ?? null
  showForm.value = true
}

async function save() {
  await enableAfter(saving, async () => {
    try {
      await service.rate(props.app.slug, { score: form.score, review: form.review || null, versionId: form.versionId, worked: form.worked === 'yes' ? true : form.worked === 'no' ? false : null })
      notifyS(t('rating_saved'))
      showForm.value = false
      page.value = 1
      await load()
      emit('changed')
    } catch (err) {
      notifyError(err)
    }
  })
}

async function removeMine() {
  if (!(await confirmX(t('confirm_delete_rating')))) return
  try {
    await service.deleteRating(props.app.slug)
    await load()
    emit('changed')
  } catch (err) {
    notifyError(err)
  }
}

async function vote(r: RatingDto, helpful: boolean) {
  if (!user.isAuthenticated) return notifyError(new Error(t('sign_in_to_vote')))
  try {
    const updated = await service.vote(r.id, r.myVote === helpful ? null : helpful)
    Object.assign(r, updated)
  } catch (err) {
    notifyError(err)
  }
}

async function sendReply(r: RatingDto) {
  if (!replyText.value.trim()) return
  try {
    const updated = await service.reply(r.id, replyText.value.trim())
    Object.assign(r, updated)
    replyFor.value = null
    replyText.value = ''
  } catch (err) {
    notifyError(err)
  }
}
</script>

<template>
  <div id="reviews" class="ratings">
    <div class="ratings__summary">
      <div class="ratings__big">
        <ScoreBadge :score="summary?.average ?? 0" :count="summary?.count ?? 0" large />
        <div class="gm-muted">{{ t('ratings_count', { n: summary?.count ?? 0 }) }}</div>
        <div v-if="summary && summary.count" class="ratings__worked">
          <span class="is-ok">✔ {{ summary.workedCount }} {{ t('worked') }}</span>
          <span class="is-bad">✘ {{ summary.notWorkedCount }} {{ t('did_not_work') }}</span>
        </div>
      </div>
      <div class="ratings__bars">
        <div v-for="b in bars" :key="b.label" class="ratings__bar">
          <span class="ratings__bar-label">{{ b.label }}</span>
          <div class="ratings__bar-track"><div class="ratings__bar-fill" :style="{ width: b.pct + '%' }" /></div>
          <span class="ratings__bar-count">{{ b.value }}</span>
        </div>
      </div>
      <div class="ratings__cta">
        <template v-if="canRate">
          <ElButton type="primary" @click="openForm">{{ mine ? t('edit_my_rating') : t('rate_this_app') }}</ElButton>
          <ElButton v-if="mine" text type="danger" @click="removeMine">{{ t('delete') }}</ElButton>
        </template>
        <div v-else-if="app.viewer.isOwner" class="gm-muted">{{ t('cannot_rate_own') }}</div>
        <div v-else-if="user.isAuthenticated" class="gm-muted">{{ t('download_to_rate') }}</div>
        <RouterLink v-else :to="{ path: '/login', query: { redirect: `/app/${app.slug}` } }" class="gm-link">{{ t('sign_in_to_rate') }}</RouterLink>
      </div>
    </div>

    <ElDialog v-model="showForm" :title="t('rate_this_app')" width="520px">
      <ElForm label-position="top">
        <ElFormItem :label="`${t('score')}: ${form.score}/100`">
          <ElSlider v-model="form.score" :min="0" :max="100" :step="1" show-input />
        </ElFormItem>
        <ElFormItem :label="t('worked_on_version')">
          <ElRadioGroup v-model="form.worked">
            <ElRadio value="yes">✔ {{ t('worked') }}</ElRadio>
            <ElRadio value="no">✘ {{ t('did_not_work') }}</ElRadio>
            <ElRadio value="unknown">{{ t('not_sure') }}</ElRadio>
          </ElRadioGroup>
        </ElFormItem>
        <ElFormItem :label="t('version')">
          <ElSelect v-model="form.versionId" style="width: 100%">
            <ElOption v-for="v in app.versions" :key="v.id" :value="v.id" :label="v.version" />
          </ElSelect>
        </ElFormItem>
        <ElFormItem :label="t('review')">
          <ElInput v-model="form.review" type="textarea" :rows="4" maxlength="2000" show-word-limit :placeholder="t('review_placeholder')" />
        </ElFormItem>
      </ElForm>
      <template #footer>
        <ElButton @click="showForm = false">{{ t('cancel') }}</ElButton>
        <ElButton type="primary" :loading="saving" @click="save">{{ t('save') }}</ElButton>
      </template>
    </ElDialog>

    <div class="ratings__list-head">
      <strong>{{ t('reviews') }}</strong>
      <ElRadioGroup v-model="sort" size="small">
        <ElRadioButton value="helpful">{{ t('sort_helpful') }}</ElRadioButton>
        <ElRadioButton value="newest">{{ t('sort_newest') }}</ElRadioButton>
        <ElRadioButton value="highest">{{ t('sort_highest') }}</ElRadioButton>
        <ElRadioButton value="lowest">{{ t('sort_lowest') }}</ElRadioButton>
      </ElRadioGroup>
    </div>
    <div v-loading="loading" class="ratings__list">
      <div v-if="!rows.length && !loading" class="gm-muted">{{ t('no_reviews_yet') }}</div>
      <article v-for="r in rows" :key="r.id" class="review">
        <div class="review__head">
          <ElAvatar :size="32" :src="r.avatarUrl || undefined">{{ r.displayName.slice(0, 1).toUpperCase() }}</ElAvatar>
          <div class="review__who">
            <RouterLink :to="`/u/${r.username}`" class="review__name">{{ r.displayName }}</RouterLink>
            <div class="sub">{{ fromNow(r.createdAt) }}<span v-if="r.version"> · v{{ r.version }}</span></div>
          </div>
          <ScoreBadge :score="r.score" :count="1" />
          <ElTag v-if="r.worked === true" type="success" size="small" effect="plain">✔ {{ t('worked') }}</ElTag>
          <ElTag v-else-if="r.worked === false" type="danger" size="small" effect="plain">✘ {{ t('did_not_work') }}</ElTag>
        </div>
        <p v-if="r.review" class="review__text">{{ r.review }}</p>
        <div v-for="rep in r.replies" :key="rep.id" class="review__reply">
          <strong>{{ rep.displayName }}</strong> <ElTag v-if="rep.isUploader" size="small" type="primary" effect="plain">{{ t('uploader') }}</ElTag>
          <span class="sub">· {{ fromNow(rep.createdAt) }}</span>
          <p>{{ rep.body }}</p>
        </div>
        <div class="review__actions">
          <ElButton size="small" text :type="r.myVote === true ? 'primary' : ''" @click="vote(r, true)">👍 {{ t('helpful') }} ({{ r.helpfulCount }})</ElButton>
          <ElButton size="small" text :type="r.myVote === false ? 'primary' : ''" @click="vote(r, false)">👎</ElButton>
          <ElButton v-if="(app.viewer.isOwner || user.isAdmin) && !r.replies.length" size="small" text @click="replyFor = replyFor === r.id ? null : r.id">{{ t('reply') }}</ElButton>
          <ElButton size="small" text type="danger" @click="emit('report', r.id)">{{ t('report') }}</ElButton>
        </div>
        <div v-if="replyFor === r.id" class="review__reply-form">
          <ElInput v-model="replyText" type="textarea" :rows="2" maxlength="2000" />
          <ElButton size="small" type="primary" @click="sendReply(r)">{{ t('send') }}</ElButton>
        </div>
      </article>
    </div>
    <PagePagination v-model:page="page" :page-size="10" :total="total" />
  </div>
</template>

<style scoped lang="scss">
.ratings {
  &__summary {
    display: grid;
    grid-template-columns: auto 1fr auto;
    gap: 24px;
    align-items: center;
    padding: 16px;
    border: 1px solid var(--gm-border);
    border-radius: 10px;
    margin-bottom: 16px;
  }
  &__big {
    text-align: center;
  }
  &__worked {
    display: flex;
    gap: 10px;
    justify-content: center;
    font-size: 12px;
    margin-top: 6px;
    .is-ok { color: var(--gm-green-dark); }
    .is-bad { color: #b42318; }
  }
  &__bar {
    display: grid;
    grid-template-columns: 60px 1fr 30px;
    gap: 8px;
    align-items: center;
    font-size: 12px;
    &-track {
      height: 8px;
      background: var(--gm-page-bg);
      border-radius: 4px;
      overflow: hidden;
    }
    &-fill {
      height: 100%;
      background: var(--gm-primary);
      border-radius: 4px;
    }
    &-count {
      color: var(--gm-text-muted);
    }
  }
  &__list-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
    flex-wrap: wrap;
    gap: 8px;
  }
  &__list {
    display: flex;
    flex-direction: column;
    gap: 12px;
    min-height: 60px;
  }
  @media (max-width: 760px) {
    &__summary {
      grid-template-columns: 1fr;
    }
  }
}
.review {
  padding: 12px 14px;
  border: 1px solid var(--gm-border);
  border-radius: 10px;
  &__head {
    display: flex;
    align-items: center;
    gap: 10px;
    flex-wrap: wrap;
  }
  &__who {
    flex: 1;
  }
  &__name {
    font-weight: 700;
    color: var(--gm-text);
  }
  &__text {
    margin: 10px 0 6px;
    line-height: 1.5;
    white-space: pre-wrap;
  }
  &__reply {
    margin: 8px 0 0 20px;
    padding: 8px 12px;
    border-left: 3px solid var(--gm-primary);
    background: var(--gm-page-bg);
    border-radius: 0 8px 8px 0;
    font-size: 13px;
    p {
      margin: 4px 0 0;
    }
  }
  &__actions {
    margin-top: 6px;
  }
  &__reply-form {
    display: flex;
    gap: 8px;
    margin-top: 8px;
    align-items: flex-start;
  }
}
</style>
