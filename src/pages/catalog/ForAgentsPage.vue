<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { AgentCheck, AgentDocs } from '@/utils/models/requests-models'
import { AgentService } from '@/utils/services/requests-service'
import { useCommonStore } from '@/stores/common-store'
import { useSiteStore } from '@/stores/site-store'
import { useUserStore } from '@/stores/user-store'
import { copyText, notifyError } from '@/utils/tools'

/** Developer / agent page: the flow, endpoints, MCP configuration snippets and a live "check" playground. */
const { t } = useI18n()
const common = useCommonStore()
const site = useSiteStore()
const user = useUserStore()
const docs = ref<AgentDocs | null>(null)
const q = ref('')
const checking = ref(false)
const result = ref<AgentCheck | null>(null)

const apiUrl = computed(() => site.config?.apiPublicUrl ?? '')
const mcpUrl = computed(() => site.config?.mcpPublicUrl ?? '')
const claudeCodeSnippet = computed(() => `claude mcp add --transport http ai-already-did-it ${mcpUrl.value}\n# with an API key (dashboard → API keys):\nclaude mcp add --transport http ai-already-did-it ${mcpUrl.value} --header "X-Api-Key: aad_..."`)
const jsonSnippet = computed(() => JSON.stringify({ mcpServers: { 'ai-already-did-it': { type: 'http', url: mcpUrl.value, headers: { 'X-Api-Key': 'aad_...' } } } }, null, 2))
const curlSnippet = computed(() => `curl "${apiUrl.value}/api/v1/agent/check?q=cpu+temperature+monitor+for+linux" -H "X-Api-Key: aad_..."`)
const systemPrompt = computed(() => `Before generating any application, call GET ${apiUrl.value}/api/v1/agent/check?q=<description> (or the MCP tool check_before_building). If the verdict is "download", use the existing app. If it is "fork", start from its source and prompts. Only build from scratch when the verdict is "build", then publish the result at ${site.config?.publicUrl ?? ''}.`)

onMounted(async () => {
  common.setPageTitle(t('for_agents'))
  await site.ensureLoaded()
  docs.value = await new AgentService().docs().catch(() => null)
})

async function check() {
  if (q.value.trim().length < 3) return
  checking.value = true
  try {
    result.value = await new AgentService().check(q.value.trim())
  } catch (err) {
    notifyError(err)
  } finally {
    checking.value = false
  }
}
</script>

<template>
  <div class="gm-container agents">
    <h1 class="gm-title">{{ t('agents_title') }}</h1>
    <p class="agents__lead">{{ t('agents_lead') }}</p>

    <section class="gm-card agents__box">
      <h2>{{ t('agents_try') }}</h2>
      <div class="agents__try">
        <ElInput v-model="q" size="large" :placeholder="t('agents_try_placeholder')" @keyup.enter="check">
          <template #append><ElButton type="primary" :loading="checking" @click="check">{{ t('check') }}</ElButton></template>
        </ElInput>
      </div>
      <div v-if="result" class="agents__result">
        <ElTag :type="result.verdict === 'download' ? 'success' : result.verdict === 'fork' ? 'warning' : 'info'" size="large" effect="dark">{{ t(`verdict_${result.verdict}`) }}</ElTag>
        <p>{{ result.advice }}</p>
        <ul>
          <li v-for="m in result.matches" :key="m.slug">
            <RouterLink :to="`/app/${m.slug}`" class="gm-link">{{ m.name }}</RouterLink>
            <span v-if="m.similarity != null" class="gm-muted"> · {{ Math.round(m.similarity * 100) }}%</span>
            <span class="gm-muted"> · {{ m.license }} · {{ m.platforms.join(', ') }}</span>
          </li>
        </ul>
      </div>
    </section>

    <section class="gm-card agents__box">
      <h2>{{ t('agents_flow_title') }}</h2>
      <ol class="agents__flow">
        <li><code>check_before_building</code> — {{ t('agents_flow_1') }}</li>
        <li><strong>download</strong> — {{ t('agents_flow_2') }}</li>
        <li><strong>fork</strong> — {{ t('agents_flow_3') }}</li>
        <li><strong>build</strong> — {{ t('agents_flow_4') }}</li>
      </ol>
    </section>

    <div class="agents__cols">
      <section class="gm-card agents__box">
        <h2>MCP</h2>
        <p class="gm-muted">{{ t('agents_mcp_text') }}</p>
        <div class="agents__snippet"><div class="agents__snippet-head">Claude Code <ElButton text size="small" @click="copyText(claudeCodeSnippet)">{{ t('copy') }}</ElButton></div><pre>{{ claudeCodeSnippet }}</pre></div>
        <div class="agents__snippet"><div class="agents__snippet-head">Claude Desktop / Cursor / others <ElButton text size="small" @click="copyText(jsonSnippet)">{{ t('copy') }}</ElButton></div><pre>{{ jsonSnippet }}</pre></div>
        <p class="sub">{{ t('agents_mcp_tools') }}: check_before_building, search_apps, get_app, list_categories, download_app, submit_app_request, get_savings</p>
      </section>
      <section class="gm-card agents__box">
        <h2>REST API</h2>
        <p class="gm-muted">{{ t('agents_rest_text') }}</p>
        <div class="agents__snippet"><div class="agents__snippet-head">curl <ElButton text size="small" @click="copyText(curlSnippet)">{{ t('copy') }}</ElButton></div><pre>{{ curlSnippet }}</pre></div>
        <ul v-if="docs" class="agents__endpoints">
          <li v-for="(desc, ep) in docs.endpoints" :key="ep"><code>{{ ep }}</code><span class="gm-muted">{{ desc }}</span></li>
        </ul>
        <p class="sub">
          <a :href="`${apiUrl}/scalar/v1`" target="_blank" rel="noopener">OpenAPI / Scalar</a> ·
          <a :href="`${apiUrl}/llms.txt`" target="_blank" rel="noopener">llms.txt</a>
          <span v-if="docs"> · {{ docs.rateLimits }}</span>
        </p>
      </section>
    </div>

    <section class="gm-card agents__box">
      <h2>{{ t('agents_system_prompt') }}</h2>
      <div class="agents__snippet"><div class="agents__snippet-head">{{ t('agents_system_prompt_hint') }} <ElButton text size="small" @click="copyText(systemPrompt)">{{ t('copy') }}</ElButton></div><pre>{{ systemPrompt }}</pre></div>
    </section>

    <section class="gm-card agents__box agents__keys">
      <div>
        <h2>{{ t('agents_keys_title') }}</h2>
        <p class="gm-muted">{{ t('agents_keys_text') }}</p>
      </div>
      <RouterLink :to="user.isAuthenticated ? '/dashboard/api-keys' : { path: '/login', query: { redirect: '/dashboard/api-keys' } }"><ElButton type="primary">{{ t('agents_get_key') }}</ElButton></RouterLink>
    </section>
  </div>
</template>

<style scoped lang="scss">
.agents {
  padding-top: 8px;
  max-width: 1000px;
  &__lead { font-size: 16px; color: var(--gm-text-muted); }
  &__box { padding: 20px 22px; margin-top: 16px; h2 { margin: 0 0 8px; font-size: 18px; } }
  &__cols { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
  &__result { margin-top: 14px; p { margin: 10px 0; } ul { margin: 0; padding-left: 18px; } }
  &__flow { padding-left: 20px; line-height: 1.8; margin: 0; }
  &__snippet {
    margin: 10px 0;
    border: 1px solid var(--gm-border);
    border-radius: 8px;
    overflow: hidden;
    &-head { display: flex; justify-content: space-between; align-items: center; padding: 4px 10px; background: var(--gm-page-bg); font-size: 12px; font-weight: 600; }
    pre { margin: 0; padding: 10px 12px; font-size: 12px; white-space: pre-wrap; word-break: break-all; }
  }
  &__endpoints { list-style: none; padding: 0; margin: 10px 0; li { display: flex; flex-direction: column; gap: 2px; padding: 6px 0; border-top: 1px solid var(--gm-border); font-size: 12px; } code { font-size: 12px; } }
  &__keys { display: flex; justify-content: space-between; align-items: center; gap: 16px; }
  @media (max-width: 860px) { &__cols { grid-template-columns: 1fr; } }
}
</style>
