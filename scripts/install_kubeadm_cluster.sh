#!/usr/bin/env bash
set -euo pipefail

# Bootstrap a kubeadm control-plane node on Ubuntu.
# This is a reference script. Validate versions and CNI choice.
#
# Requirements:
# - Run after Docker/containerd install (containerd running)
# - Swap disabled
#
# Outputs:
# - kubeadm join command to add worker nodes

K8S_VERSION="${K8S_VERSION:-1.29.0}"
POD_CIDR="${POD_CIDR:-10.244.0.0/16}" # Flannel default; change for Calico

if [[ "${EUID}" -ne 0 ]]; then
  echo "Please run as root (or via sudo)." >&2
  exit 1
fi

swapoff -a || true
sed -i.bak '/ swap / s/^\(.*\)$/#\1/g' /etc/fstab || true

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends apt-transport-https ca-certificates curl gpg

# Kubernetes apt repo (pkgs.k8s.io)
install -m 0755 -d /etc/apt/keyrings
curl -fsSL "https://pkgs.k8s.io/core:/stable:/v1.29/deb/Release.key" | gpg --dearmor -o /etc/apt/keyrings/kubernetes-apt-keyring.gpg
chmod a+r /etc/apt/keyrings/kubernetes-apt-keyring.gpg

cat >/etc/apt/sources.list.d/kubernetes.list <<'EOF'
deb [signed-by=/etc/apt/keyrings/kubernetes-apt-keyring.gpg] https://pkgs.k8s.io/core:/stable:/v1.29/deb/ /
EOF

apt-get update
apt-get install -y --no-install-recommends kubelet kubeadm kubectl
apt-mark hold kubelet kubeadm kubectl

systemctl enable --now kubelet

# Initialize cluster
kubeadm init --kubernetes-version "${K8S_VERSION}" --pod-network-cidr "${POD_CIDR}"

# Configure kubectl for the admin user (root by default here)
export KUBECONFIG=/etc/kubernetes/admin.conf
mkdir -p "${HOME}/.kube"
cp -i /etc/kubernetes/admin.conf "${HOME}/.kube/config"
chown "$(id -u)":"$(id -g)" "${HOME}/.kube/config"

echo
echo "Cluster initialized."
echo "Next: install a CNI (Flannel/Calico) and then join workers."
echo
echo "Join command:"
kubeadm token create --print-join-command

