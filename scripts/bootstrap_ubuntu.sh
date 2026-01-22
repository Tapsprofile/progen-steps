#!/usr/bin/env bash
set -euo pipefail

# Ubuntu baseline bootstrap for DevOps/K8s nodes.
# Validate distro: Ubuntu 20.04/22.04/24.04 preferred.

if [[ "${EUID}" -ne 0 ]]; then
  echo "Please run as root (or via sudo)." >&2
  exit 1
fi

export DEBIAN_FRONTEND=noninteractive

apt-get update
apt-get install -y --no-install-recommends \
  ca-certificates \
  curl \
  gnupg \
  lsb-release \
  jq \
  unzip \
  git \
  python3 \
  python3-pip \
  net-tools \
  iptables \
  iproute2 \
  conntrack \
  socat \
  ebtables \
  ethtool \
  chrony

# Time sync
systemctl enable --now chrony || true

# Kernel modules required by container runtime / Kubernetes.
cat >/etc/modules-load.d/k8s.conf <<'EOF'
overlay
br_netfilter
EOF
modprobe overlay || true
modprobe br_netfilter || true

# Sysctl required for Kubernetes networking.
cat >/etc/sysctl.d/99-kubernetes-cri.conf <<'EOF'
net.bridge.bridge-nf-call-iptables  = 1
net.bridge.bridge-nf-call-ip6tables = 1
net.ipv4.ip_forward                 = 1
EOF
sysctl --system

echo "Bootstrap complete."

