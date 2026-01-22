## Gaming Ops Platform — Delivery Playbook (22 Days)

This folder contains a **principal-architect reviewed** playbook to deliver a **Smart MLOps-based Gaming Ops Platform** that can:

- Build game software (CI)
- Package into containers (Docker)
- Deploy and operate on Kubernetes (on-prem/local first, then AWS)
- Stream events (Kafka/Zookeeper locally; MSK in AWS)
- Observe everything (Prometheus/Grafana + OpenTelemetry; AMP/AMG/ADOT in AWS)
- Prepare the platform for ML workflows (SageMaker / MLOps integration)

### Index

- `PLAYBOOK.md`: end-to-end architecture, components, RACI, deliverables, acceptance criteria
- `DAY_BY_DAY_PLAN.md`: day-by-day plan aligned to your Monday–Friday/Sat agenda (5 hrs/day)
- `RUNBOOKS/`: operational runbooks + incident/backup procedures
- `CHEATSHEETS/`: Git, YAML, kubectl, Helm, Jenkins basics

### Scripts & templates

See `/scripts` for install/bootstrap scripts and `/templates` for reference configs/manifests.

