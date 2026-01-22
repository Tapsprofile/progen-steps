#!/usr/bin/env bash
set -euo pipefail

# Join a worker node to an existing kubeadm cluster.
# Usage:
#   sudo ./join_kubeadm_worker.sh "<kubeadm join ...>"

if [[ "${EUID}" -ne 0 ]]; then
  echo "Please run as root (or via sudo)." >&2
  exit 1
fi

JOIN_CMD="${1:-}"
if [[ -z "${JOIN_CMD}" ]]; then
  echo "Missing join command argument." >&2
  exit 1
fi

swapoff -a || true

eval "${JOIN_CMD}"

echo "Worker joined."

