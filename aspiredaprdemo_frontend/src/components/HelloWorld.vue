<script setup lang="ts">
import { ref, computed } from 'vue'

defineProps<{ msg: string }>()

const count = ref(0)

// Convert import.meta.env to array of key-value pairs
const envVars = computed(() => {
  return Object.entries(import.meta.env)
    .filter(([key]) => key.startsWith('VITE_'))
    .sort()
})
</script>

<template>
  <h1>{{ msg }}</h1>

  <div class="card">
    <button type="button" @click="count++">count is {{ count }}</button>
    <p>
      Edit <code>components/HelloWorld.vue</code> to test HMR
    </p>
  </div>

  <p>
    <strong>Environment Variables (VITE_*)</strong>
  </p>
  <ul>
    <li v-for="[key, value] in envVars" :key="key">
      <code>{{ key }}</code>: {{ value }}
    </li>
  </ul>

  <p>
    Check out
    <a href="https://vuejs.org/guide/quick-start.html#local" target="_blank"
      >create-vue</a
    >, the official Vue + Vite starter
  </p>
  <p>
    Learn more about IDE Support for Vue in the
    <a
      href="https://vuejs.org/guide/scaling-up/tooling.html#ide-support"
      target="_blank"
      >Vue Docs Scaling up Guide</a
    >.
  </p>
  <p class="read-the-docs">Click on the Vite and Vue logos to learn more</p>
</template>

<style scoped>
.read-the-docs {
  color: #888;
}
</style>
