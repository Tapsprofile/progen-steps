## Incident response runbook (gaming ops platform)

### Severity model

- **SEV1**: players cannot connect / revenue-impacting outage
- **SEV2**: degraded performance (high latency, matchmaking issues)
- **SEV3**: partial subsystem impact (Kafka lag, delayed telemetry)

### First 10 minutes checklist (SEV1/SEV2)

- **Confirm scope**
  - Which game/service? Which region/cluster?
  - How many users affected?
- **Check platform health**
  - `kubectl get nodes`
  - `kubectl get pods -A | head`
  - `kubectl get events -A --sort-by=.lastTimestamp | tail`
- **Check ingress/network**
  - Ingress controller pods healthy?
  - Service endpoints present?
- **Check recent changes**
  - last deployment time, last config changes, last node changes
- **Mitigate quickly**
  - Roll back: `kubectl rollout undo deploy/<name> -n <ns>`
  - Scale temporarily: `kubectl scale deploy/<name> --replicas=<n> -n <ns>`

### Common failure patterns

- **CrashLoopBackOff**
  - Look for config/secrets mismatch, missing env vars, image pull errors
- **ImagePullBackOff**
  - Registry auth, tag not pushed, network/DNS issues
- **NodeNotReady**
  - Disk pressure, memory pressure, kubelet/containerd down
- **Kafka consumer lag spike**
  - Backpressure, partition imbalance, broker IO saturation

### Required post-incident outputs

- Timeline (UTC)
- Root cause + contributing factors
- What detection missed and how to improve alerting
- Action items (owner + due date)

