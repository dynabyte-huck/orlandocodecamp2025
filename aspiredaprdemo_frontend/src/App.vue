<script setup lang="ts">
import SubscriberForm from './components/SubscriberForm.vue'
import Weather from './components/Weather.vue'
</script>

<template>
  <div class="header">
    <a href="https://www.dynabytetech.com" target="_blank">
      <img src="./assets/dynabyte.svg" class="logo" alt="Dynabyte logo" />
    </a>
  </div>
  <h1>Aspire & Dapr.io Demo</h1>
  <div class="main-layout">
    <Weather />
    <SubscriberForm />
  </div>
</template>

<style scoped>
.logo {
  height: 6em;
  padding: 1.5em;
  will-change: filter;
  transition: filter 300ms;
}
.logo:hover {
  filter: drop-shadow(0 0 2em #646cffaa);
}
.logo.vue:hover {
  filter: drop-shadow(0 0 2em #42b883aa);
}

.main-layout {
  display: flex;
  flex-direction: column;
  gap: 2rem;
  margin-top: 2rem;
}

/* Side-by-side layout on larger screens */
@media (min-width: 768px) {
  .main-layout {
    flex-direction: row;
    align-items: flex-start;
    justify-content: center;
  }

  .main-layout > * {
    flex: 1 1 0;
    max-width: 600px;
  }
}
</style>
