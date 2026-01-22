#!/usr/bin/env bash
set -euo pipefail

# Installs Docker Engine on Ubuntu using official Docker repos.
# Notes:
# - For Kubernetes, containerd is used under the hood.
# - Adjust for air-gapped networks if needed.

if [[ "${EUID}" -ne 0 ]]; then
  echo "Please run as root (or via sudo)." >&2
  exit 1
fi

export DEBIAN_FRONTEND=noninteractive

apt-get update
apt-get install -y --no-install-recommends ca-certificates curl gnupg

install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg

ARCH="$(dpkg --print-architecture)"
CODENAME="$(. /etc/os-release && echo "${VERSION_CODENAME}")"

cat >/etc/apt/sources.list.d/docker.list <<EOF
deb [arch=${ARCH} signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu ${CODENAME} stable
EOF

apt-get update
apt-get install -y --no-install-recommends docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

systemctl enable --now docker

# Recommended: use systemd cgroup driver for containerd.
mkdir -p /etc/containerd
containerd config default >/etc/containerd/config.toml
sed -i 's/SystemdCgroup = false/SystemdCgroup = true/' /etc/containerd/config.toml
systemctl restart containerd

echo "Docker install complete."
docker version

