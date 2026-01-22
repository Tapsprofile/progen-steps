#!/usr/bin/env bash
set -euo pipefail

# Reference installer for OpenTelemetry Collector (local binary).
# For Kubernetes, prefer deploying via Helm (ADOT/otel-collector chart).

if [[ "${EUID}" -ne 0 ]]; then
  echo "Please run as root (or via sudo)." >&2
  exit 1
fi

OTEL_VERSION="${OTEL_VERSION:-0.96.0}"
ARCH="${ARCH:-amd64}"

TMP_DIR="$(mktemp -d)"
cd "${TMP_DIR}"

curl -L -o otelcol.tgz "https://github.com/open-telemetry/opentelemetry-collector-releases/releases/download/v${OTEL_VERSION}/otelcol_${OTEL_VERSION}_linux_${ARCH}.tar.gz"
tar -xzf otelcol.tgz

install -m 0755 otelcol /usr/local/bin/otelcol

mkdir -p /etc/otelcol
cat >/etc/otelcol/config.yaml <<'EOF'
receivers:
  otlp:
    protocols:
      grpc:
      http:

exporters:
  logging:
    loglevel: info

service:
  pipelines:
    traces:
      receivers: [otlp]
      exporters: [logging]
EOF

cat >/etc/systemd/system/otelcol.service <<'EOF'
[Unit]
Description=OpenTelemetry Collector
After=network-online.target

[Service]
ExecStart=/usr/local/bin/otelcol --config=/etc/otelcol/config.yaml
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now otelcol

echo "otelcol installed and running."

