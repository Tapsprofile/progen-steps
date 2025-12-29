<script setup lang="ts">
import { computed, ref } from 'vue'

const apiBaseUrl = computed(() => import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000')

type CreatePurchaseRequestResponse = { purchaseRequestId: string; workflowExecutionArn: string }

const prId = ref('')
const spendAmount = ref<number>(2500000)
const contractValue = ref<number>(2800000)
const createdBy = ref('REQUESTOR')
const part1dAuthorizers = ref('AUTH-001,AUTH-002,AUTH-003,AUTH-004')
const part2aAuthorizers = ref('AUTH-101,AUTH-102,AUTH-103')

const createResult = ref<CreatePurchaseRequestResponse | null>(null)
const createError = ref<string | null>(null)

async function createPurchaseRequest() {
  createError.value = null
  createResult.value = null

  const res = await fetch(`${apiBaseUrl.value}/purchase-requests`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      purchaseRequestId: prId.value || null,
      spendAmount: Number(spendAmount.value),
      contractValue: Number(contractValue.value),
      createdBy: createdBy.value,
      part1dAuthorizers: part1dAuthorizers.value
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean),
      part2aAuthorizers: part2aAuthorizers.value
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean),
    }),
  })

  if (!res.ok) {
    createError.value = await res.text()
    return
  }

  createResult.value = (await res.json()) as CreatePurchaseRequestResponse
}

const callbackPurchaseRequestId = ref('PR-2025-000042')
const bidEvalNotes = ref('Bid evaluation completed.')
const bidEvalAwardAmount = ref<number>(2500000)
const callbackResult = ref<unknown | null>(null)
const callbackError = ref<string | null>(null)

async function submitBidEvaluationFormH() {
  callbackError.value = null
  callbackResult.value = null

  const res = await fetch(`${apiBaseUrl.value}/callbacks/bid-evaluation`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      purchaseRequestId: callbackPurchaseRequestId.value,
      notes: bidEvalNotes.value,
      awardAmount: Number(bidEvalAwardAmount.value),
    }),
  })

  const text = await res.text()
  callbackResult.value = text ? JSON.parse(text) : null
  if (!res.ok) callbackError.value = text
}
</script>

<template>
  <main class="page">
    <header class="header">
      <h1>Procurement Orchestration Engine</h1>
      <p class="muted">Create a Purchase Request and submit callbacks (Form H).</p>
      <p class="muted">
        API base URL: <code>{{ apiBaseUrl }}</code>
      </p>
    </header>

    <section class="card">
      <h2>Create + Submit Purchase Request</h2>

      <div class="grid">
        <label>
          PurchaseRequestId (optional)
          <input v-model="prId" placeholder="PR-2025-000042" />
        </label>
        <label>
          SpendAmount
          <input v-model.number="spendAmount" type="number" min="1" step="1" />
        </label>
        <label>
          ContractValue
          <input v-model.number="contractValue" type="number" min="1" step="1" />
        </label>
        <label>
          CreatedBy
          <input v-model="createdBy" placeholder="REQUESTOR" />
        </label>
        <label class="span2">
          Part 1d Authorizers (comma-separated)
          <input v-model="part1dAuthorizers" />
        </label>
        <label class="span2">
          Part 2a Authorizers (comma-separated)
          <input v-model="part2aAuthorizers" />
        </label>
      </div>

      <div class="row">
        <button @click="createPurchaseRequest">Create + Start Workflow</button>
      </div>

      <pre v-if="createResult" class="pre ok">{{ createResult }}</pre>
      <pre v-if="createError" class="pre err">{{ createError }}</pre>
    </section>

    <section class="card">
      <h2>Submit Bid Evaluation Form H (callback)</h2>
      <p class="muted">
        This will only succeed once the workflow has reached the <code>waitForTaskToken</code> state and a token has been
        persisted for the PurchaseRequestId.
      </p>

      <div class="grid">
        <label class="span2">
          PurchaseRequestId
          <input v-model="callbackPurchaseRequestId" />
        </label>
        <label class="span2">
          Notes
          <input v-model="bidEvalNotes" />
        </label>
        <label>
          AwardAmount
          <input v-model.number="bidEvalAwardAmount" type="number" min="1" step="1" />
        </label>
      </div>

      <div class="row">
        <button @click="submitBidEvaluationFormH">Submit Form H</button>
      </div>

      <pre v-if="callbackResult" class="pre ok">{{ callbackResult }}</pre>
      <pre v-if="callbackError" class="pre err">{{ callbackError }}</pre>
    </section>
  </main>
</template>
