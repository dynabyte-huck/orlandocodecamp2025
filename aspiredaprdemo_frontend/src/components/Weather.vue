<script setup lang="ts">
import { ref, onMounted } from 'vue'

const weatherUrl = import.meta.env.VITE_WEATHER_URL
const forecasts = ref<any[]>([])
const isLoading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    const res = await fetch(weatherUrl)
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    forecasts.value = await res.json()
  } catch (err: any) {
    error.value = err.message ?? 'Failed to fetch weather data.'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <div class="card">
    <h2>🌤️ 5-Day Weather Forecast</h2>

    <div v-if="isLoading">Loading forecast...</div>
    <div v-else-if="error" class="error">Error: {{ error }}</div>
    <div v-else class="forecast-grid">
      <div v-for="day in forecasts" :key="day.date" class="forecast-card">
        <h3>{{ new Date(day.date).toLocaleDateString() }}</h3>
        <p class="summary">{{ day.summary }}</p>
        <div class="temps">
          <span class="temp-c">{{ day.temperatureC }}°C</span>
          <span class="temp-f">{{ day.temperatureF }}°F</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.forecast-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap: 1rem;
  margin-top: 1rem;
}

.forecast-card {
  background: white;
  color: #111827;
  border-radius: 10px;
  padding: 1rem;
  text-align: center;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);
}

@media (prefers-color-scheme: dark) {
  .forecast-card {
    background: #1f2937;
    color: #f9fafb;
  }
}

.forecast-card h3 {
  margin: 0.25rem 0;
  font-size: 1.1rem;
}

.summary {
  font-weight: bold;
  color: #3b82f6;
  margin-bottom: 0.5rem;
}

.temps {
  display: flex;
  justify-content: center;
  gap: 1rem;
  font-size: 1rem;
}

.temp-c {
  color: #10b981;
}

.temp-f {
  color: #ef4444;
}

.error {
  color: red;
  margin-top: 1rem;
}
</style>
