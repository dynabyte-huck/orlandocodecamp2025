<script setup lang="ts">
import { ref } from 'vue'

// GUID generator
function generateGuid(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    const r = (Math.random() * 16) | 0
    const v = c === 'x' ? r : (r & 0x3) | 0x8
    return v.toString(16)
  })
}

interface Subscriber {
  Email: string
  FirstName: string
  LastName: string
}

const subscriber = ref<Subscriber>({
  Email: '',
  FirstName: '',
  LastName: ''
})

const errors = ref<Record<string, string>>({})
const serverError = ref<string | null>(null)
const isSubmitting = ref(false)
const success = ref(false)

const validate = (): boolean => {
  errors.value = {}
  let valid = true

  if (!subscriber.value.Email?.trim()) {
    errors.value.Email = 'Email is required.'
    valid = false
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(subscriber.value.Email)) {
    errors.value.Email = 'Invalid email format.'
    valid = false
  }

  if (!subscriber.value.FirstName?.trim()) {
    errors.value.FirstName = 'FirstName is required.'
    valid = false
  }

  if (!subscriber.value.LastName?.trim()) {
    errors.value.LastName = 'LastName is required.'
    valid = false
  }

  return valid
}

const submit = async () => {
  success.value = false
  serverError.value = null
  if (!validate()) return

  isSubmitting.value = true
  const generatedId = generateGuid()

  try {
    const url = `${import.meta.env.VITE_SUBSCRIBER_URL}/${encodeURIComponent(generatedId)}`
    const res = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        SubscriberId: generatedId,
        ...subscriber.value
      })
    })

    if (res.ok) {
      success.value = true
      return
    }

    const data = await res.json()

    if (res.status === 400 && data?.extensions?.validationErrors) {
      for (const err of data.extensions.validationErrors) {
        if (err.Field) {
          errors.value[err.Field] = err.Message
        }
      }
    } else {
      serverError.value = data?.detail || 'An error occurred.'
    }

  } catch (err: any) {
    serverError.value = err.message || 'Unexpected error occurred.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="card">
    <h2>📬 New Subscriber Form</h2>

    <form @submit.prevent="submit" class="form-grid">
      <div class="form-field">
        <label for="email">Email</label>
        <input id="email" type="email" v-model="subscriber.Email" />
        <span class="error" v-if="errors.Email">{{ errors.Email }}</span>
      </div>

      <div class="form-field">
        <label for="firstName">First Name</label>
        <input id="firstName" v-model="subscriber.FirstName" />
        <span class="error" v-if="errors.FirstName">{{ errors.FirstName }}</span>
      </div>

      <div class="form-field">
        <label for="lastName">Last Name</label>
        <input id="lastName" v-model="subscriber.LastName" />
        <span class="error" v-if="errors.LastName">{{ errors.LastName }}</span>
      </div>

      <div class="submit-group">
        <button type="submit" :disabled="isSubmitting">
          {{ isSubmitting ? 'Submitting...' : 'Submit' }}
        </button>
      </div>

      <div v-if="serverError" class="error-block">Error: {{ serverError }}</div>
      <div v-if="success" class="success-block">✅ Subscriber created successfully!</div>
    </form>
  </div>
</template>

<style scoped>
.form-grid {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-top: 1rem;
}

.form-field {
  display: flex;
  flex-direction: column;
  text-align: left;
}

.form-field label {
  font-weight: 600;
  margin-bottom: 0.25rem;
}

input {
  padding: 0.5rem;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  font-size: 1rem;
}

.error {
  font-size: 0.875rem;
  color: #ef4444;
  margin-top: 0.25rem;
}

.error-block {
  color: red;
  font-weight: bold;
  margin-top: 1rem;
}

.success-block {
  color: #10b981;
  font-weight: bold;
  margin-top: 1rem;
}

.submit-group {
  margin-top: 1rem;
}

.card {
  padding: 1rem;
  background: #f3f4f6;
  border-radius: 12px;
  margin-top: 1rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

@media (prefers-color-scheme: dark) {
  .card {
    background: #1f2937;
    color: #f9fafb;
  }

  input {
    background-color: #374151;
    color: white;
    border: 1px solid #4b5563;
  }
}
</style>
