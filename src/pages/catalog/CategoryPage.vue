<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import SearchPage from './SearchPage.vue'

/** /category/:slug — the search page pinned to a category subtree. */
const route = useRoute()
const slug = computed(() => String(route.params.slug))
</script>

<template>
  <SearchPage :key="slug" :fixed-category="slug" />
</template>
