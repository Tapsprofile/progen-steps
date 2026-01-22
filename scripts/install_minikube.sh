#!/usr/bin/env bash
set -euo pipefail

# Installs Minikube and kubectl for a developer workstation/server.
# Recommended: run as a non-root user with sudo privileges.

MINIKUBE_VERSION="${MINIKUBE_VERSION:-latest}"

if [[ "${EUID}" -eq 0 ]]; then
  echo "Do not run this script as root. Use a sudo-enabled user." >&2
  exit 1
fi

sudo apt-get update
sudo apt-get install -y --no-install-recommends curl ca-certificates conntrack

curl -LO "https://storage.googleapis.com/minikube/releases/${MINIKUBE_VERSION}/minikube-linux-amd64"
sudo install minikube-linux-amd64 /usr/local/bin/minikube
rm -f minikube-linux-amd64

# kubectl (stable)
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -m 0755 kubectl /usr/local/bin/kubectl
rm -f kubectl

echo "Starting Minikube..."
minikube start --driver=docker --cpus=4 --memory=8192

echo "Minikube ready."

