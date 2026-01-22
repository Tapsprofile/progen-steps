#!/usr/bin/env bash
set -euo pipefail

# Installs Prometheus + Grafana into a Kubernetes cluster using Helm.
# Requires: kubectl configured to target the cluster.

if ! command -v kubectl >/dev/null 2>&1; then
  echo "kubectl not found." >&2
  exit 1
fi

if ! command -v helm >/dev/null 2>&1; then
  echo "helm not found. Install Helm first." >&2
  exit 1
fi

NAMESPACE="${NAMESPACE:-monitoring}"

kubectl create namespace "${NAMESPACE}" --dry-run=client -o yaml | kubectl apply -f -

helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add grafana https://grafana.github.io/helm-charts
helm repo update

# kube-prometheus-stack includes Prometheus + Alertmanager + Grafana.
helm upgrade --install kps prometheus-community/kube-prometheus-stack \
  --namespace "${NAMESPACE}" \
  --set grafana.enabled=true

echo "Installed kube-prometheus-stack into namespace ${NAMESPACE}."
echo "Tip: port-forward Grafana:"
echo "  kubectl -n ${NAMESPACE} port-forward svc/kps-grafana 3000:80"

