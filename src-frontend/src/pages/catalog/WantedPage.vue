<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import type { AppRequestDto } from '@/utils/models/requests-models'
import { RequestsService } from '@/utils/services/requests-service'
import { useUserStore } from '@/stores/user-store'
import { useCommonStore } from '@/stores/common-store'
import AppCard from '@/components/catalog/AppCard.vue'
import PagePagination from '@/components/common/PagePagination.vue'
import StatusTag from '@/components/common/StatusTag.vue'
import { enableAfter, notifyError, notifyS } from '@/utils/tools'
import { fromNow } from '@/utils/format'

/** "Wanted" board: apps people and agents searched for and did not find. */
const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const user = useUserStore()
const common = useCommonStore()
const service = new RequestsService()

const rows = ref<AppRequestDto[]>([])
const total = ref(0)
const page = ref(1)
const status = ref('open')
const sort = ref('votes')
const q = ref('')
const loading = ref(false)
const saving = ref(false)
const showForm = ref(false)
const detail = ref<AppRequestDto | null>(null)
const form = reactive({ title: typeof route.query.title === 'string' ? route.query.title : '', description: '' })

async function load() {
  loading.value = true
  try {
    const p = await service.list(status.value, q.value || undefined, sort.value, page.value)
    rows.value = p.items
    total.value = p.totalCount
  } catch (err) {
    notifyError(err)
  } finally {
    loading.value = false
  }
}

async function openDetail(id: number) {
  try {
    detail.value = await service.getOne(id)
  } catch (err) {
    notifyError(err)
  }
}

onMounted(() => {
  common.setPageTitle(t('wanted'))
  load()
  if (route.params.id) openDetail(Number(route.params.id))
  if (route.query.title && user.isAuthenticated) showForm.value = true
})
watch([status, sort, page], load)

async function create() {
  await enableAfter(saving, async () => {
    try {
      const r = await service.create(form.title, form.description)
      notifyS(t('request_posted'))
      showForm.value = false
      form.title = ''
      form.description = ''
      await load()
      detail.value = r
    } catch (err) {
      notifyError(err)
    }
  })
}

async function vote(r: AppRequestDto) {
  if (!user.isAuthenticated) return router.push({ path: '/login', query: { redirect: '/wanted' } })
  try {
    const updated = await service.vote(r.id)
    Object.assign(r, updated)
    if (detail.value?.id === r.id) detail.value = updated
  } catch (err) {
    notifyError(err)
  }
}
</script>

<template>
  <div class="gm-container wanted">
    <header class="wanted__head">
      <div>
        <h1 class="gm-title">{{ t('wanted') }}</h1>
        <p class="gm-muted">{{ t('wanted_intro') }}</p>
      </div>
      <ElButton v-if="user.isAuthenticated" type="primary" @click="showForm = true"><ElIcon><Plus /></ElIcon>{{ t('post_wanted') }}</ElButton>
      <RouterLink v-else :to="{ path: '/login', query: { redirect: '/wanted' } }"><ElButton type="primary">{{ t('sign_in_to_post') }}</ElButton></RouterLink>
    </header>
    <div class="wanted__bar">
      <ElRadioGroup v-model="status" size="small">
        <ElRadioButton value="open">{{ t('status_open') }}</ElRadioButton>
        <ElRadioButton value="fulfilled">{{ t('status_fulfilled') }}</ElRadioButton>
        <ElRadioButton value="all">{{ t('all') }}</ElRadioButton>
      </ElRadioGroup>
      <ElRadioGroup v-model="sort" size="small">
        <ElRadioButton value="votes">{{ t('sort_votes') }}</ElRadioButton>
        <ElRadioButton value="newest">{{ t('sort_newest') }}</ElRadioButton>
      </ElRadioGroup>
      <ElInput v-model="q" :placeholder="t('search')" clearable size="small" style="width: 220px" @keyup.enter="load" @clear="load" />
    </div>
    <div v-loading="loading" class="wanted__list">
      <div v-if="!rows.length && !loading" class="gm-muted">{{ t('no_requests') }}</div>
      <article v-for="r in rows" :key="r.id" class="gm-card wanted__item">
        <button class="wanted__vote" :class="{ 'is-on': r.myVote }" type="button" @click="vote(r)">▲<span>{{ r.voteCount }}</span></button>
        <div class="wanted__body">
          <a class="wanted__title" @click="openDetail(r.id)">{{ r.title }}</a>
          <p class="wanted__desc">{{ r.description }}</p>
          <div class="sub">
            <StatusTag :value="r.status" /> · {{ fromNow(r.createdAt) }} · {{ r.requesterUsername ?? t('an_agent') }} <span v-if="r.source !== 'web'">({{ r.source }})</span>
            <RouterLink v-if="r.fulfilledBy" :to="`/app/${r.fulfilledBy.slug}`" class="gm-link"> · ✔ {{ r.fulfilledBy.name }}</RouterLink>
          </div>
        </div>
      </article>
    </div>
    <PagePagination v-model:page="page" :page-size="20" :total="total" />

    <ElDialog v-model="showForm" :title="t('post_wanted')" width="560px">
      <ElForm label-position="top">
        <ElFormItem :label="t('title')"><ElInput v-model="form.title" maxlength="160" show-word-limit /></ElFormItem>
        <ElFormItem :label="t('description')"><ElInput v-model="form.description" type="textarea" :rows="5" maxlength="4000" show-word-limit :placeholder="t('wanted_description_placeholder')" /></ElFormItem>
      </ElForm>
      <template #footer>
        <ElButton @click="showForm = false">{{ t('cancel') }}</ElButton>
        <ElButton type="primary" :loading="saving" :disabled="form.title.trim().length < 5 || form.description.trim().length < 20" @click="create">{{ t('post') }}</ElButton>
      </template>
    </ElDialog>

    <ElDialog :model-value="Boolean(detail)" :title="detail?.title" width="720px" @close="detail = null">
      <template v-if="detail">
        <p style="white-space: pre-wrap">{{ detail.description }}</p>
        <div class="sub"><StatusTag :value="detail.status" /> · {{ detail.voteCount }} {{ t('votes') }} · {{ fromNow(detail.createdAt) }}</div>
        <template v-if="detail.suggestions.length">
          <h4>{{ t('maybe_already_exists') }}</h4>
          <div class="gm-grid"><AppCard v-for="a in detail.suggestions" :key="a.id" :app="a" show-similarity /></div>
        </template>
      </template>
    </ElDialog>
  </div>
</template>

<style scoped lang="scss">
.wanted {
  padding-top: 8px;
  &__head {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 12px;
    flex-wrap: wrap;
  }
  &__bar {
    display: flex;
    gap: 12px;
    align-items: center;
    flex-wrap: wrap;
    margin: 10px 0 14px;
  }
  &__list {
    display: flex;
    flex-direction: column;
    gap: 10px;
    min-height: 80px;
  }
  &__item {
    display: flex;
    gap: 14px;
    padding: 14px 16px;
  }
  &__vote {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 6px 10px;
    border: 1px solid var(--gm-border);
    border-radius: 8px;
    background: var(--gm-page-bg);
    cursor: pointer;
    font-weight: 700;
    color: var(--gm-text);
    height: fit-content;
    &.is-on {
      border-color: var(--gm-primary);
      color: var(--gm-primary);
      background: var(--gm-primary-light);
    }
  }
  &__body { flex: 1; min-width: 0; }
  &__title { font-weight: 700; font-size: 15px; cursor: pointer; color: var(--gm-text); }
  &__desc {
    margin: 4px 0 6px;
    color: var(--gm-text-muted);
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
}
</style>
