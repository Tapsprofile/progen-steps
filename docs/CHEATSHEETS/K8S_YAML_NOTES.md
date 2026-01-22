## Kubernetes YAML notes (practical)

### Objects you will use daily

- **Namespace**: isolation boundary
- **Deployment**: stateless apps, rolling updates
- **StatefulSet**: ordered pods + stable identity (datastores, Kafka brokers)
- **Service**: stable networking (ClusterIP/NodePort/LoadBalancer)
- **Ingress**: HTTP routing into cluster
- **ConfigMap/Secret**: configuration injection
- **HPA**: horizontal scaling by CPU/memory/custom metrics
- **PodDisruptionBudget**: keep availability during maintenance

### Required fields (common)

- `metadata.name`, `metadata.namespace`
- `spec.selector` must match `spec.template.metadata.labels`
- `resources.requests/limits` (always add for production)
- `livenessProbe` and `readinessProbe`

### Debug commands to pair with YAML

- `kubectl get pods -A`
- `kubectl describe pod <pod>`
- `kubectl logs <pod> -c <container>`
- `kubectl get events -A --sort-by=.lastTimestamp`
- `kubectl rollout status deploy/<name>`
- `kubectl rollout undo deploy/<name>`

