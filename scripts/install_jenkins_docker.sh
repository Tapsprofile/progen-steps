#!/usr/bin/env bash
set -euo pipefail

# Jenkins via Docker (fast local bootstrap).
# For production, prefer Jenkins on Kubernetes + persistent volumes + backup.

if [[ "${EUID}" -ne 0 ]]; then
  echo "Please run as root (or via sudo)." >&2
  exit 1
fi

JENKINS_HOME_DIR="${JENKINS_HOME_DIR:-/var/jenkins_home}"
HTTP_PORT="${HTTP_PORT:-8080}"
AGENT_PORT="${AGENT_PORT:-50000}"

mkdir -p "${JENKINS_HOME_DIR}"
chown -R 1000:1000 "${JENKINS_HOME_DIR}" || true

docker network create jenkins || true

docker run -d \
  --name jenkins \
  --restart unless-stopped \
  --network jenkins \
  -p "${HTTP_PORT}:8080" \
  -p "${AGENT_PORT}:50000" \
  -v "${JENKINS_HOME_DIR}:/var/jenkins_home" \
  jenkins/jenkins:lts

echo "Jenkins started on port ${HTTP_PORT}."
echo "To get initial admin password:"
echo "  docker exec jenkins cat /var/jenkins_home/secrets/initialAdminPassword"

