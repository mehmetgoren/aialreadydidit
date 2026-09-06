<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { CascaderOption } from 'element-plus'
import type { AppDraft, DuplicateCheck, MetadataSuggestion, PromptInput } from '@/utils/models/apps-models'
import type { CategoryNode, DerivationKind } from '@/utils/models/catalog-models'
import { MyAppsService } from '@/utils/services/my-apps-service'
import { CatalogService } from '@/utils/services/catalog-service'
import { useSiteStore } from '@/stores/site-store'
import { useCategoryStore, useCategoryName } from '@/stores/category-store'
import AppCard from '@/components/catalog/AppCard.vue'
import CategoryPicker from '@/components/upload/CategoryPicker.vue'
import { debounce, enableAfter, fieldErrors, notifyError, notifyS } from '@/utils/tools'

/** Step 2 — name, descriptions (with live duplicate check), category (+ LLM suggestion), license, model, prompts, tags, lineage. */
const props = defineProps<{ draft: AppDraft }>()
const emit = defineEmits<{ updated: [d: AppDraft]; next: []; back: [] }>()
const { t } = useI18n()
const site = useSiteStore()
const categories = useCategoryStore()
const name = useCategoryName()
const service = new MyAppsService()

const form = reactive({
  name: props.draft.name === 'Untitled app' ? '' : props.draft.name,
  shortDescription: props.draft.shortDescription,
  longDescription: props.draft.longDescription,
  categoryId: props.draft.categoryId,
  licenseId: props.draft.licenseId,
  llmModelId: props.draft.llmModelId,
  llmModelNote: props.draft.llmModelNote ?? '',
  homepageUrl: props.draft.homepageUrl ?? '',
  tags: [...props.draft.tags],
  derivedFromAppId: props.draft.derivedFromAppId,
  derivedFromName: props.draft.derivedFromName ?? '',
  derivationKind: (props.draft.derivationKind ?? 'fork') as DerivationKind,
  prompts: props.draft.prompts.length ? props.draft.prompts.map((p) => ({ title: p.title, promptText: p.promptText })) : ([{ title: 'Prompt 1', promptText: '' }] as PromptInput[]),
  estOverride: props.draft.estIsOverride,
  estTokens: props.draft.estIsOverride ? props.draft.estGenerationTokens : null as number | null,
  estCost: props.draft.estIsOverride ? props.draft.estGenerationCostUsd : null as number | null,
})
const errors = ref<Record<string, string>>({})
const saving = ref(false)
const suggesting = ref(false)
const suggestion = ref<MetadataSuggestion | null>(null)
const dupes = ref<DuplicateCheck | null>(null)
const dupeDismissed = ref(false)
const tagOptions = ref<string[]>([])
const derivedOptions = ref<{ id: number; name: string }[]>([])

const cascader = computed(() => {
  const map = (nodes: CategoryNode[]): CascaderOption[] =>
    nodes.map((n) => ({ value: n.id, label: name(n), children: n.children.length ? map(n.children) : undefined }))
  return map(categories.roots)
})
const categoryPath = computed(() => (form.categoryId ? categories.pathToId(form.categoryId).map((c) => c.id) : []))
function onCategory(v: unknown) {
  const arr = v as number[] | null
  form.categoryId = arr?.length ? arr[arr.length - 1]! : null
}

const runDupes = debounce(async () => {
  if (form.shortDescription.length < 15 && form.longDescription.length < 30) return
  try {
    dupes.value = await service.checkDuplicates(form.name, form.shortDescription, form.longDescription, props.draft.id)
  } catch {
    /* ignore */
  }
}, 700)
watch(() => [form.name, form.shortDescription, form.longDescription], () => { dupeDismissed.value = false; runDupes() })

onMounted(async () => {
  await categories.fetchTree()
  tagOptions.value = (await new CatalogService().getTags(undefined, 60).catch(() => [])).map((x) => x.name)
  runDupes()
})

async function searchApps(q: string) {
  if (!q) return
  const r = await new CatalogService().search({ q, pageSize: 8 }).catch(() => null)
  derivedOptions.value = r?.page.items.map((a) => ({ id: a.id, name: a.name })) ?? []
}

function declareFork(appId: number, appName: string) {
  form.derivedFromAppId = appId
  form.derivedFromName = appName
  form.derivationKind = 'fork'
  derivedOptions.value = [{ id: appId, name: appName }]
  dupeDismissed.value = true
}

async function suggest() {
  await enableAfter(suggesting, async () => {
    try {
      suggestion.value = await service.suggestMetadata(form.name, form.shortDescription, form.longDescription, props.draft.readmeMarkdown)
    } catch (err) {
      notifyError(err)
    }
  })
}
function applySuggestion() {
  const s = suggestion.value
  if (!s) return
  if (s.categoryId) form.categoryId = s.categoryId
  if (s.tags.length) form.tags = Array.from(new Set([...form.tags, ...s.tags])).slice(0, site.limits?.maxTags ?? 10)
  if (s.shortDescription && !form.shortDescription) form.shortDescription = s.shortDescription
  suggestion.value = null
}

async function save(next: boolean) {
  await enableAfter(saving, async () => {
    errors.value = {}
    try {
      const d = await service.update(props.draft.id, {
        name: form.name,
        shortDescription: form.shortDescription,
        longDescription: form.longDescription,
        categoryId: form.categoryId,
        licenseId: form.licenseId,
        llmModelId: form.llmModelId,
        llmModelNote: form.llmModelNote,
        homepageUrl: form.homepageUrl,
        tags: form.tags,
        derivedFromAppId: form.derivedFromAppId ?? 0,
        derivationKind: form.derivedFromAppId ? form.derivationKind : null,
        prompts: form.prompts.filter((p) => p.promptText.trim()),
        estGenerationTokens: form.estOverride ? form.estTokens : 0,
        estGenerationCostUsd: form.estOverride ? form.estCost : null,
      })
      emit('updated', d)
      notifyS(t('saved'))
      if (next) emit('next')
    } catch (err) {
      errors.value = fieldErrors(err)
      notifyError(err)
    }
  })
}
</script>

<template>
  <div class="step">
    <div v-if="dupes?.bestMatch && !dupeDismissed" class="dupe">
      <div class="dupe__text">
        <strong>{{ t('dupe_title', { pct: Math.round((dupes.bestMatch.similarity ?? 0) * 100), name: dupes.bestMatch.name }) }}</strong>
        <p>{{ t('dupe_text') }}</p>
        <div class="dupe__actions">
          <RouterLink :to="`/app/${dupes.bestMatch.slug}`" target="_blank"><ElButton size="small">{{ t('dupe_view') }}</ElButton></RouterLink>
          <ElButton size="small" type="primary" @click="declareFork(dupes.bestMatch.id, dupes.bestMatch.name)">{{ t('dupe_declare_fork') }}</ElButton>
          <ElButton size="small" text @click="dupeDismissed = true">{{ t('dupe_continue') }}</ElButton>
        </div>
      </div>
      <div class="dupe__card"><AppCard :app="dupes.bestMatch" show-similarity /></div>
    </div>

    <ElForm label-position="top" class="grid-form">
      <ElFormItem :label="t('app_name_label')" :error="errors.name" class="full"><ElInput v-model="form.name" maxlength="120" show-word-limit size="large" /></ElFormItem>
      <ElFormItem :label="t('short_description')" :error="errors.shortDescription" class="full"><ElInput v-model="form.shortDescription" maxlength="200" show-word-limit :placeholder="t('short_description_hint')" /></ElFormItem>
      <ElFormItem :label="t('long_description')" :error="errors.longDescription" class="full">
        <ElInput v-model="form.longDescription" type="textarea" :rows="10" :placeholder="t('long_description_hint')" />
        <div class="sub">{{ t('markdown_supported') }}</div>
      </ElFormItem>

      <!-- LLM categorisation on: free cascader + "Suggest". Off (Ai:EnableCategorySuggestions=false): explicit category / sub-category selects. -->
      <ElFormItem v-if="!site.config?.categorySuggestionsAvailable" :label="t('category')" class="full">
        <CategoryPicker v-model="form.categoryId" :error="errors.categoryId" />
      </ElFormItem>
      <ElFormItem v-else :label="t('category')" :error="errors.categoryId">
        <div class="cat-row">
          <ElCascader :model-value="categoryPath" :options="cascader" :props="{ checkStrictly: true, expandTrigger: 'hover' }" clearable filterable style="flex: 1" @update:model-value="onCategory" />
          <ElButton :loading="suggesting" @click="suggest"><ElIcon><MagicStick /></ElIcon>{{ t('suggest') }}</ElButton>
        </div>
        <ElAlert v-if="suggestion" type="success" :closable="true" show-icon class="suggestion" @close="suggestion = null">
          <template #title>{{ t('suggestion_title', { model: suggestion.model }) }}</template>
          <div v-if="suggestion.categoryPath">📂 {{ suggestion.categoryPath }}</div>
          <div v-else-if="suggestion.proposedCategoryName">📂 {{ t('proposed_category', { name: suggestion.proposedCategoryName }) }}</div>
          <div v-if="suggestion.tags.length">🏷 {{ suggestion.tags.join(', ') }}</div>
          <div v-if="suggestion.shortDescription">✎ {{ suggestion.shortDescription }}</div>
          <div v-if="suggestion.reasoning" class="sub">{{ suggestion.reasoning }}</div>
          <ElButton size="small" type="primary" style="margin-top: 6px" @click="applySuggestion">{{ t('apply_suggestion') }}</ElButton>
        </ElAlert>
      </ElFormItem>
      <ElFormItem :label="t('license')" :error="errors.licenseId">
        <ElSelect v-model="form.licenseId" filterable default-first-option style="width: 100%">
          <ElOption v-for="l in site.licenses.filter((x) => x.isAllowed)" :key="l.id" :value="l.id" :label="`${l.spdxId} — ${l.name}`" />
        </ElSelect>
        <div class="sub">{{ t('license_hint') }}</div>
      </ElFormItem>
      <ElFormItem :label="t('generated_by')" :error="errors.llmModelId">
        <ElSelect v-model="form.llmModelId" filterable default-first-option style="width: 100%">
          <ElOption v-for="m in site.llmModels" :key="m.id" :value="m.id" :label="m.displayName" />
        </ElSelect>
      </ElFormItem>
      <ElFormItem :label="t('model_note')"><ElInput v-model="form.llmModelNote" maxlength="200" :placeholder="t('model_note_hint')" /></ElFormItem>
      <ElFormItem :label="t('tags')" class="full">
        <ElSelect v-model="form.tags" multiple filterable allow-create default-first-option :multiple-limit="site.limits?.maxTags ?? 10" style="width: 100%" :placeholder="t('tags_hint')">
          <ElOption v-for="tg in tagOptions" :key="tg" :value="tg" :label="tg" />
        </ElSelect>
      </ElFormItem>
      <ElFormItem :label="t('homepage')" :error="errors.homepageUrl"><ElInput v-model="form.homepageUrl" placeholder="https://" /></ElFormItem>
      <ElFormItem :label="t('derived_from')">
        <div class="cat-row">
          <ElSelect v-model="form.derivedFromAppId" filterable remote clearable :remote-method="searchApps" :placeholder="t('derived_from_hint')" style="flex: 1" @clear="form.derivedFromName = ''">
            <ElOption v-if="form.derivedFromAppId && !derivedOptions.some((o) => o.id === form.derivedFromAppId)" :value="form.derivedFromAppId" :label="form.derivedFromName" />
            <ElOption v-for="o in derivedOptions" :key="o.id" :value="o.id" :label="o.name" />
          </ElSelect>
          <ElSelect v-if="form.derivedFromAppId" v-model="form.derivationKind" style="width: 130px">
            <ElOption value="fork" :label="t('derivation_fork')" /><ElOption value="inspired" :label="t('derivation_inspired')" /><ElOption value="port" :label="t('derivation_port')" />
          </ElSelect>
        </div>
      </ElFormItem>
    </ElForm>

    <h3 class="step__h3">{{ t('prompts_title') }}</h3>
    <p class="gm-muted">{{ t('prompts_hint') }}</p>
    <div v-for="(p, i) in form.prompts" :key="i" class="prompt">
      <div class="prompt__head">
        <ElInput v-model="p.title" maxlength="160" style="width: 320px" :placeholder="t('prompt_title')" />
        <ElButton v-if="form.prompts.length > 1" text type="danger" size="small" @click="form.prompts.splice(i, 1)">{{ t('remove') }}</ElButton>
      </div>
      <ElInput v-model="p.promptText" type="textarea" :rows="5" :placeholder="t('prompt_text_hint')" />
    </div>
    <ElButton size="small" @click="form.prompts.push({ title: `Prompt ${form.prompts.length + 1}`, promptText: '' })"><ElIcon><Plus /></ElIcon>{{ t('add_prompt') }}</ElButton>

    <h3 class="step__h3">{{ t('cost_title') }}</h3>
    <p class="gm-muted">{{ t('cost_hint') }}</p>
    <ElCheckbox v-model="form.estOverride">{{ t('cost_override') }}</ElCheckbox>
    <div v-if="form.estOverride" class="cat-row" style="margin-top: 8px; max-width: 480px">
      <ElInputNumber v-model="form.estTokens" :min="0" :step="1000" :placeholder="t('tokens')" style="flex: 1" />
      <ElInputNumber v-model="form.estCost" :min="0" :step="0.5" :precision="2" placeholder="USD" style="flex: 1" />
    </div>

    <div class="step__nav">
      <ElButton @click="emit('back')">← {{ t('back') }}</ElButton>
      <div><ElButton :loading="saving" @click="save(false)">{{ t('save') }}</ElButton><ElButton type="primary" :loading="saving" @click="save(true)">{{ t('save_and_next') }} →</ElButton></div>
    </div>
  </div>
</template>

<style scoped lang="scss">
.dupe {
  display: grid;
  grid-template-columns: 1fr 260px;
  gap: 16px;
  padding: 14px 16px;
  margin-bottom: 18px;
  border: 1px solid var(--gm-yellow);
  background: #fff8e6;
  border-radius: 10px;
  html.dark & { background: #3a2e10; }
  p { margin: 6px 0 10px; }
  &__actions { display: flex; gap: 8px; flex-wrap: wrap; .el-button + .el-button { margin-left: 0; } }
  @media (max-width: 760px) { grid-template-columns: 1fr; }
}
.cat-row { display: flex; gap: 8px; width: 100%; }
.suggestion { margin-top: 8px; }
.step__h3 { margin: 22px 0 4px; }
.prompt { margin: 10px 0; &__head { display: flex; justify-content: space-between; margin-bottom: 6px; } }
.step__nav { display: flex; justify-content: space-between; margin-top: 24px; padding-top: 16px; border-top: 1px solid var(--gm-border); }
</style>
