<script setup lang="ts">
import { computed, ref } from 'vue'

const apiBaseUrl = computed(() => import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000')

type PendingTask = {
  taskToken: string
  purchaseRequestId: string
  taskType: string
  actorId?: string | null
  description: string
  createdAtUtc: string
}
type WorkflowStep = {
  stepId: string
  executionId: string
  purchaseRequestId: string
  stepName: string
  stepType: string
  correlationId?: string | null
  input?: unknown
  output?: unknown
  status: string
  occurredAtUtc: string
}
type ExecutionView = {
  execution: {
    executionId: string
    purchaseRequestId: string
    definitionName: string
    definitionVersion: number
    status: string
    phaseIndex: number
    data: unknown
    createdAtUtc: string
    updatedAtUtc: string
  }
  steps: WorkflowStep[]
  pendingTasks: PendingTask[]
}

const prId = ref('')
const spendAmount = ref<number>(2500000)
const contractValue = ref<number>(2800000)
const createdBy = ref('REQUESTOR')
const part1dAuthorizers = ref('AUTH-001,AUTH-002,AUTH-003,AUTH-004')
const part2aAuthorizers = ref('AUTH-101,AUTH-102,AUTH-103')

const createResult = ref<{ purchaseRequestId: string; execution: ExecutionView } | null>(null)
const createError = ref<string | null>(null)
const pendingTasks = ref<PendingTask[]>([])
const selectedTaskToken = ref<string>('')
const steps = ref<WorkflowStep[]>([])

async function createPurchaseRequest() {
  createError.value = null
  createResult.value = null
  pendingTasks.value = []

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

  createResult.value = (await res.json()) as { purchaseRequestId: string; execution: ExecutionView }
  await refreshPendingTasks()
}

const callbackPurchaseRequestId = ref('PR-2025-000042')
const callbackOutputJson = ref<string>(JSON.stringify({ form: 'BID_EVALUATION_FORM_H', notes: 'Bid evaluation completed.' }, null, 2))
const callbackResult = ref<unknown | null>(null)
const callbackError = ref<string | null>(null)

async function refreshPendingTasks() {
  const id = createResult.value?.purchaseRequestId || callbackPurchaseRequestId.value
  if (!id) return
  const res = await fetch(`${apiBaseUrl.value}/purchase-requests/${encodeURIComponent(id)}/pending-tasks`)
  if (!res.ok) return
  pendingTasks.value = (await res.json()) as PendingTask[]
  if (!selectedTaskToken.value && pendingTasks.value.length > 0) selectedTaskToken.value = pendingTasks.value[0]!.taskToken

  const execId = createResult.value?.execution?.execution?.executionId
  if (execId) {
    const execRes = await fetch(`${apiBaseUrl.value}/workflow/executions/${encodeURIComponent(execId)}`)
    if (execRes.ok) {
      const view = (await execRes.json()) as ExecutionView
      steps.value = view.steps
    }
  }
}

async function submitCallback() {
  callbackError.value = null
  callbackResult.value = null

  let parsed: unknown
  try {
    parsed = callbackOutputJson.value ? JSON.parse(callbackOutputJson.value) : {}
  } catch (e) {
    callbackError.value = `Invalid JSON output: ${(e as Error).message}`
    return
  }

  const res = await fetch(`${apiBaseUrl.value}/workflow/callbacks`, {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({
      taskToken: selectedTaskToken.value,
      output: parsed,
    }),
  })

  const text = await res.text()
  callbackResult.value = text ? JSON.parse(text) : null
  if (!res.ok) callbackError.value = text
  await refreshPendingTasks()
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
        <button @click="refreshPendingTasks" :disabled="!(createResult?.purchaseRequestId || callbackPurchaseRequestId)">Refresh pending tasks</button>
      </div>

      <pre v-if="createResult" class="pre ok">{{ createResult }}</pre>
      <pre v-if="createError" class="pre err">{{ createError }}</pre>

      <div v-if="pendingTasks.length" class="pre ok">
        <strong>Pending tasks</strong>
        <div style="margin-top: 10px; display: grid; gap: 8px">
          <label class="span2">
            TaskToken
            <select v-model="selectedTaskToken">
              <option v-for="t in pendingTasks" :key="t.taskToken" :value="t.taskToken">
                {{ t.taskType }}{{ t.actorId ? ` (${t.actorId})` : '' }} - {{ t.description }}
              </option>
            </select>
          </label>
        </div>
      </div>

      <div v-if="steps.length" class="pre ok">
        <strong>Step history</strong>
        <div style="margin-top: 10px; display: grid; gap: 6px">
          <div v-for="s in steps" :key="s.stepId" style="border-top: 1px solid #e2e8f0; padding-top: 8px">
            <div><code>{{ s.occurredAtUtc }}</code> — <strong>{{ s.stepName }}</strong> / {{ s.stepType }} / {{ s.status }}</div>
            <div v-if="s.correlationId" class="muted">Correlation: <code>{{ s.correlationId }}</code></div>
          </div>
        </div>
      </div>
    </section>

    <section class="card">
      <h2>Submit callback (resume by TaskToken)</h2>
      <p class="muted">
        Pick a pending <code>TaskToken</code> and send output JSON to resume the handwritten workflow.
      </p>

      <div class="grid">
        <label class="span2">
          PurchaseRequestId
          <input v-model="callbackPurchaseRequestId" />
        </label>
        <label class="span2">
          Output (JSON)
          <textarea v-model="callbackOutputJson" rows="8" style="width: 100%"></textarea>
        </label>
      </div>

      <div class="row">
        <button @click="refreshPendingTasks">Refresh pending tasks</button>
        <button @click="submitCallback" :disabled="!selectedTaskToken">Submit callback</button>
      </div>

      <pre v-if="callbackResult" class="pre ok">{{ callbackResult }}</pre>
      <pre v-if="callbackError" class="pre err">{{ callbackError }}</pre>
    </section>
  </main>
</template>
