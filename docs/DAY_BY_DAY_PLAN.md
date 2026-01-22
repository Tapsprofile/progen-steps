## Day-by-day delivery plan (5 focused hrs/day)

This plan maps exactly to your **Mon → Fri → Sat** focus areas, then repeats in **AWS**.

### Assumptions (explicit)

- **Team**: 1 Admin/SRE + 1 DevOps/Platform Dev + 1 App Dev (can be same person early) + 1 BA/QA part-time
- **Work window**: 5 serious hours/day (no meetings). Any meeting time reduces scope.
- **Target**: “One reference game workload” + “repeatable platform blueprint”

---

## Days 1–11 (Local/On‑prem reference platform)

### Day 1 (Monday) — Architecture & backlog freeze

- **Define target architecture** (local): Docker + kubeadm + Jenkins + Kafka + Prom/Grafana + OTel + Ansible
- **Define environments**:
  - Dev: Minikube
  - Stage/Prod-like: kubeadm multi-node
- **Select standards**:
  - OS: Ubuntu LTS
  - K8s CNI: pick one (Calico or Flannel)
  - Ingress: nginx
  - Packaging: Helm (preferred) or plain manifests (choose one and standardize)
- **Deliverables**
  - Architecture diagram (logical)
  - Backlog with priorities + “Definition of Done” checklist
  - Minimum server sizing table (see Day 2)

### Day 2 (Tuesday) — Base installs (OS, Docker, K8s, Jenkins, Ansible)

- **Server sizing and inventory**
  - Control plane: \(2–4\) vCPU, \(8–16\) GB RAM, \(100+\) GB SSD
  - Worker nodes (each): \(4+\) vCPU, \(16+\) GB RAM, \(200+\) GB SSD
  - Network: stable L2/L3, open ports for kubeadm, CNI, ingress
- **Install**
  - Docker / containerd
  - kubeadm cluster (single control plane + 2 workers minimum)
  - Minikube (developer parity)
  - Jenkins (Docker or K8s)
  - Ansible control node + inventory
- **Local Xonotic server** (validation workload)
- **Deliverables**
  - Scripts captured in repo (`/scripts`)
  - A “Setup Notes” markdown with all commands run and why (see `/docs/RUNBOOKS`)

### Day 3 (Wednesday) — Distributed streaming base (Kafka)

- **Goal**: Kafka+ZK running in Docker first, then multi-node concept validated
- **Tasks**
  - Docker compose for Zookeeper + Kafka
  - Multi-broker setup (3 brokers) + topic replication
  - Test producer/consumer to validate
- **Deliverables**
  - `docker compose up` works and passes the test scripts
  - Notes: kernel/network tuning encountered (ulimits, ports, file descriptors)

### Day 4 (Thursday) — CI/CD for one game workload

- **Select sample multi-user game/service repo** (from GitHub) OR a simple multiplayer service
- **Containerize**
  - Dockerfile
  - Health endpoints
- **Jenkins pipeline**
  - Build + test + scan (basic) + push to registry + deploy to K8s
  - Provide rollback step
- **Git user levels + cheat sheet**
  - Admin, Dev, BA roles and what commands each uses
- **Deliverables**
  - One green pipeline run from Git push to K8s deployment
  - Git role policy doc + command cheat sheet

### Day 5 (Friday) — K8s resilience, events, YAML mastery

- **Practice**
  - Delete pods/nodes, validate rescheduling
  - Validate service discovery, readiness/liveness probes
- **Kubernetes YAML notes**
  - Deployments, Services, Ingress, ConfigMaps, Secrets, HPA
- **Deliverables**
  - Runbook: “node loss / pod crash loop” operational procedure
  - YAML cheat sheet + examples

### Day 6 (Saturday) — Monitoring tools

- **Feasibility study**: pick 5 tools (baseline)
  - Prometheus, Grafana, Loki, Alertmanager, Tempo (or Jaeger)
- **Install**
  - Prometheus + Grafana (Helm preferred)
- **Configure**
  - Dashboards: node/pod health, ingress, workload CPU/mem, Kafka lag
- **Deliverables**
  - Cheatsheets for Grafana and Prometheus
  - Notes: OS resources used (ports, file paths, permissions, ulimits)

### Day 7 (Monday) — OpenTelemetry & fleet transition

- **Install OTel Collector**
  - Collect metrics/logs/traces from game service + cluster
  - Export to local backends (Prometheus/Grafana/Loki/Tempo)
- **Deliverables**
  - OTel config stored in repo
  - Event/telemetry runbook: what signals exist and where they go

### Day 8 (Tuesday) — Performance tuning checklist

- **Define metrics** to tune:
  - Game workload latency \(p50/p95\), CPU, memory, network, packet loss
  - Kafka: broker IO, request latency, consumer lag, partitions, ISR
  - K8s: etcd/control plane, node pressure, HPA behavior
  - CI/CD: build time, cache hit ratio, deployment lead time
  - OS: sysctl, file descriptors, conntrack, disk IO scheduler
- **Deliverables**
  - A tuning checklist document
  - Dashboard updates with tuning metrics tracked

### Day 9 (Wednesday) — Post-production operations (runbooks)

- **Runbooks**
  - Multi-node operations
  - Backups/snapshots (configs, persistent volumes, DB if used)
  - Log retention and analytics plan
- **State strategy**
  - Hot: RAM/Redis
  - Warm: NoSQL options (e.g., DynamoDB/MongoDB/Cassandra/ScyllaDB/Elastic)
  - Cold: 5 SQL DBs (Postgres, MySQL, SQL Server, Oracle, MariaDB)
- **Deliverables**
  - “Ops handbook” with incident procedures and recovery steps
  - Short but detailed cheatsheets: scaling, networking, state/logic

### Day 10 (Thursday) — Validate game run (3 users)

- **Run the workload**
  - 3 user sessions (real or simulated)
- **Monitor**
  - Validate dashboards and alerts
- **Deliverables**
  - Validation report + adjustments

### Day 11 (Friday) — AWS mapping prep (local -> cloud parity)

- **Map components** to AWS services
- **Prepare IaC skeleton** (Terraform/CloudFormation)
- **Deliverables**
  - AWS day-by-day plan (Days 12–22) confirmed
  - Repo contains AWS templates and checklists

---

## Days 12–22 (AWS execution — repeat with managed services)

This aligns with your provided Phase 1–4 architecture.

### Day 12 — Networking & Security (VPC + IAM/IRSA)

- VPC (3 AZ), public/private subnets, NAT, VPC endpoints where needed
- EKS OIDC provider + IRSA roles
- **Deliverable**: IaC code + least privilege policies

### Day 13 — Compute layer (EKS)

- eksctl/Terraform to create EKS cluster 1.29+
- Install AWS LB Controller
- **Deliverable**: EKS with 3 nodes across AZs, ingress functioning

### Day 14 — Persistence & assets (EFS + S3)

- EFS + mount targets; EFS CSI driver; StorageClass
- S3 buckets for binaries/replays/telemetry lake
- **Deliverable**: Pod mounts EFS successfully

### Day 15 — Database tier (RDS + ElastiCache)

- Multi-AZ RDS Postgres
- Redis cluster
- Security group rules restricted to EKS nodes/pods (via SGs)
- **Deliverable**: connectivity verified from a Pod

### Day 16 — Streaming (MSK)

- 3 broker MSK cluster
- Auth strategy (IAM auth preferred)
- **Deliverable**: test producer in EKS writes to MSK

### Day 17 — Cloud CI/CD & registry (ECR + CodeBuild)

- ECR repo w/ scanning
- CodeBuild project builds and pushes image
- **Deliverable**: image lands in ECR from a commit

### Day 18 — Deployment automation (CodePipeline -> EKS)

- GitHub trigger -> build -> deploy (kubectl/Helm) with rollback
- **Deliverable**: automated deploy on push

### Day 19 — Advanced monitoring (AMP/AMG + ADOT)

- AMP workspace, AMG workspace
- ADOT collector remote_write -> AMP
- **Deliverable**: Grafana dashboard shows EKS + Kafka lag

### Day 20 — ML integration (SageMaker)

- Notebook/Studio
- Sample training job triggered from EKS event (operator or API)
- **Deliverable**: sample job run + artifacts to S3

### Day 21 — Autoscaling & spot optimization (Karpenter)

- Karpenter + spot strategy for worker nodes
- **Deliverable**: scale test adds spot capacity automatically

### Day 22 — Stress test & final ops playbook

- Distributed load test (Fargate-based)
- Final runbooks: DB failover, MSK rebalancing, incident comms, SLOs
- **Deliverable**: load test report + signed-off playbook

