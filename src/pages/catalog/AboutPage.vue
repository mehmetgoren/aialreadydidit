<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { SavingsDto } from '@/utils/models/catalog-models'
import { SiteService } from '@/utils/services/site-service'
import { useCommonStore } from '@/stores/common-store'
import SavingsCounter from '@/components/common/SavingsCounter.vue'

const { t } = useI18n()
const common = useCommonStore()
const savings = ref<SavingsDto | null>(null)
onMounted(async () => {
  common.setPageTitle(t('about'))
  savings.value = await new SiteService().savings().catch(() => null)
})
</script>

<template>
  <div class="gm-container about">
    <h1 class="gm-title">{{ t('about_title') }}</h1>
    <p class="about__lead">{{ t('about_lead') }}</p>
    <SavingsCounter :savings="savings" />
    <div class="about__cols">
      <section class="gm-card about__box">
        <h2>{{ t('about_problem_title') }}</h2>
        <p>{{ t('about_problem_text') }}</p>
      </section>
      <section class="gm-card about__box">
        <h2>{{ t('about_solution_title') }}</h2>
        <p>{{ t('about_solution_text') }}</p>
      </section>
    </div>
    <section class="gm-card about__box">
      <h2>{{ t('golden_rules') }}</h2>
      <ol>
        <li><strong>{{ t('rule_open_source') }}</strong> — {{ t('rule_open_source_why') }}</li>
        <li><strong>{{ t('rule_installable') }}</strong> — {{ t('rule_installable_why') }}</li>
        <li><strong>{{ t('rule_free') }}</strong> — {{ t('rule_free_why') }}</li>
      </ol>
    </section>
    <section class="gm-card about__box">
      <h2>{{ t('about_how_counter_title') }}</h2>
      <p>{{ t('about_how_counter_text') }}</p>
    </section>
  </div>
</template>

<style scoped lang="scss">
.about {
  padding-top: 8px;
  max-width: 920px;
  &__lead { font-size: 16px; color: var(--gm-text-muted); margin-bottom: 20px; }
  &__cols { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-top: 20px; }
  &__box { padding: 20px 22px; margin-top: 16px; h2 { margin: 0 0 8px; font-size: 18px; } p { line-height: 1.6; margin: 0; } ol { padding-left: 20px; line-height: 1.7; } }
  @media (max-width: 760px) { &__cols { grid-template-columns: 1fr; } }
}
</style>
