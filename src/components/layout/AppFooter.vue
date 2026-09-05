<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSiteStore } from '@/stores/site-store'

const { t } = useI18n()
const site = useSiteStore()
const contactEmail = computed(() => site.config?.contactEmail)
const apiUrl = computed(() => site.config?.apiPublicUrl ?? '')
const mcpUrl = computed(() => site.config?.mcpPublicUrl ?? '')
</script>

<template>
  <footer class="footer">
    <div class="gm-container">
      <div class="footer__cols">
        <div class="footer__brand">
          <div class="footer__logo">{{ t('app_name') }}</div>
          <p class="gm-muted">{{ t('footer_mission') }}</p>
        </div>
        <div>
          <h4>{{ t('footer_store') }}</h4>
          <RouterLink to="/search">{{ t('search') }}</RouterLink>
          <RouterLink to="/wanted">{{ t('wanted') }}</RouterLink>
          <RouterLink to="/upload">{{ t('upload_app') }}</RouterLink>
          <RouterLink to="/about">{{ t('about') }}</RouterLink>
        </div>
        <div>
          <h4>{{ t('footer_agents') }}</h4>
          <RouterLink to="/for-agents">{{ t('for_agents') }}</RouterLink>
          <a :href="`${apiUrl}/scalar/v1`" target="_blank" rel="noopener">REST API</a>
          <a :href="mcpUrl" target="_blank" rel="noopener">MCP server</a>
          <a :href="`${apiUrl}/llms.txt`" target="_blank" rel="noopener">llms.txt</a>
        </div>
        <div>
          <h4>{{ t('footer_rules') }}</h4>
          <span>{{ t('rule_open_source') }}</span>
          <span>{{ t('rule_installable') }}</span>
          <span>{{ t('rule_free') }}</span>
          <a v-if="contactEmail" :href="`mailto:${contactEmail}`">{{ contactEmail }}</a>
        </div>
      </div>
      <div class="footer__bottom gm-muted">© {{ new Date().getFullYear() }} {{ t('app_name') }} · {{ t('footer_tagline') }}</div>
    </div>
  </footer>
</template>

<style scoped lang="scss">
.footer {
  margin-top: 40px;
  padding: 32px 0 20px;
  background: var(--gm-card-bg);
  border-top: 1px solid var(--gm-border);
  font-size: 13px;
  &__cols {
    display: grid;
    grid-template-columns: 2fr 1fr 1fr 1.4fr;
    gap: 24px;
    h4 {
      margin: 0 0 10px;
      font-size: 13px;
      text-transform: uppercase;
      letter-spacing: 0.4px;
      color: var(--gm-text-muted);
    }
    a,
    span {
      display: block;
      color: var(--gm-text);
      margin-bottom: 6px;
    }
    a:hover {
      color: var(--gm-primary);
    }
  }
  &__logo {
    font-weight: 800;
    font-size: 18px;
    margin-bottom: 6px;
  }
  &__bottom {
    margin-top: 24px;
    padding-top: 14px;
    border-top: 1px solid var(--gm-border);
  }
  @media (max-width: 760px) {
    &__cols {
      grid-template-columns: 1fr 1fr;
    }
  }
}
</style>
