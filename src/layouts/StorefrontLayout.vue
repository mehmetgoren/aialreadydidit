<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import AnnouncementBar from '@/components/layout/AnnouncementBar.vue'
import AppHeader from '@/components/layout/AppHeader.vue'
import CategoryNav from '@/components/layout/CategoryNav.vue'
import PageBreadcrumb from '@/components/layout/PageBreadcrumb.vue'
import AppFooter from '@/components/layout/AppFooter.vue'
import { useUserStore } from '@/stores/user-store'
import { useCommonStore } from '@/stores/common-store'
import { useSiteStore } from '@/stores/site-store'

const user = useUserStore()
const common = useCommonStore()
const site = useSiteStore()
const route = useRoute()

onMounted(() => {
  site.ensureLoaded().catch(() => {})
  user.refreshMe().catch(() => {})
})
// pages set their own breadcrumb; reset when navigating
watch(
  () => route.fullPath,
  () => common.setBreadcrumb([]),
)
</script>

<template>
  <div class="storefront">
    <AnnouncementBar />
    <AppHeader />
    <CategoryNav />
    <PageBreadcrumb />
    <main class="storefront__main">
      <RouterView v-slot="{ Component }">
        <Transition name="fade" mode="out-in">
          <component :is="Component" />
        </Transition>
      </RouterView>
    </main>
    <AppFooter />
  </div>
</template>

<style scoped>
.storefront {
  min-height: 100%;
  display: flex;
  flex-direction: column;
}
.storefront__main {
  flex: 1;
}
</style>
