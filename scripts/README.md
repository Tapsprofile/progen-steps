## Scripts (reference implementations)

These scripts are **templates** intended to be adapted to your environment.

### Conventions

- Run as root only when required (prefer `sudo`)
- Keep logs: redirect stdout/stderr to a dated file during execution
- Do not hardcode secrets; use env vars or secret managers

### Included

- `bootstrap_ubuntu.sh`: base OS prep (packages, sysctl, modules)
- `install_docker_ubuntu.sh`: Docker engine install
- `install_kubeadm_cluster.sh`: kubeadm cluster bootstrap (control-plane)
- `join_kubeadm_worker.sh`: join a worker node
- `install_minikube.sh`: dev cluster
- `install_jenkins_docker.sh`: Jenkins (Docker-based)
- `install_observability_helm.sh`: Prometheus/Grafana via Helm
- `install_otel_collector.sh`: OpenTelemetry collector (local/K8s)

