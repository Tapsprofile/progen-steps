## Principal Architect Playbook — Smart MLOps Gaming Ops Platform (22 Days)

### 0) Executive intent (what we will deliver)

Within **22 working days** at **5 focused hours/day**, deliver:

- **A reference “DevOps Steroid” platform** to build, deploy, run, monitor, and troubleshoot game servers/services
- A **repeatable blueprint** that works **locally/on-prem first** and then **mirrors into AWS**
- A **minimum viable MLOps integration** path for anti-cheat / matchmaking / anomaly detection using telemetry + SageMaker

The output is not “a demo”; it is a **repeatable operating system** (scripts + IaC + runbooks + dashboards + pipelines).

---

### 1) Architecture at a glance (Hub & Spoke; local -> AWS)

#### 1.1 Local / On‑prem reference stack (Days 1–11)

- **Source control**: Git (GitHub or internal)
- **CI/CD**: Jenkins (local first; cloud later)
- **Container runtime**: Docker / containerd
- **Kubernetes**:
  - **Minikube** for developer validation
  - **kubeadm** cluster (multi-node) for “real” behavior
- **Game workloads**:
  - A sample multi-user game service (and/or **Xonotic** server locally)
  - Deployed as containers into Kubernetes
- **Streaming/Events**: Kafka + Zookeeper (Docker first, then Kubernetes)
- **Observability**:
  - Prometheus + Grafana
  - OpenTelemetry collector (OTel) for metrics/logs/traces export
- **Config/Automation**: Ansible for OS + app lifecycle

#### 1.2 AWS reference stack (Days 12–22)

- **Networking**: Hub-and-Spoke VPC, 3 AZs, public+private subnets, NAT, VPC endpoints
- **Compute**: EKS + managed node groups (and Karpenter for fast scaling)
- **Registry**: ECR with scanning
- **CI/CD**: CodeBuild + CodePipeline (GitHub trigger)
- **Streaming**: MSK (Kafka)
- **Persistence**:
  - EFS (shared “hot files”: configs/logs)
  - S3 (“cold assets”: binaries/replays/data lake)
  - RDS PostgreSQL (profiles)
  - ElastiCache Redis (real-time state/leaderboards)
- **Observability**:
  - ADOT Collector on EKS
  - AMP (managed Prometheus) + AMG (managed Grafana)
- **MLOps**:
  - SageMaker Studio/Notebook
  - Optional: SageMaker Operators for Kubernetes (trigger jobs via Kubernetes events)

---

### 2) Component inventory (what must exist to claim “done”)

#### 2.1 Core platform components

- **OS baseline**: Ubuntu LTS, hardened users/SSH, time sync, logging, firewall rules, sysctl tuning
- **Container platform**: Docker + containerd, images buildable locally and in CI
- **Kubernetes**:
  - One functional Minikube environment (developer parity)
  - One functional multi-node cluster (kubeadm), with:
    - CNI (Calico/Flannel—choose one and standardize)
    - Ingress controller (nginx or ALB in AWS)
    - Storage class strategy (local-path on-prem; EFS CSI in AWS)
- **CI/CD**:
  - One pipeline that builds a game service container, runs tests, pushes image, deploys to K8s
  - A “rollback” mechanism (Helm revision rollback / kubectl rollout undo)
- **Streaming**:
  - Kafka topic for game telemetry events
  - Producer + consumer test workload
- **Observability**:
  - Metrics dashboards for: cluster health, nodes, pods, Kafka consumer lag, game service CPU/mem, 95p latency
  - Central log strategy and minimum “golden signals” alerting
- **Security**:
  - Role-separated access (Admin/Dev/BA)
  - Secret management strategy (K8s secrets baseline; AWS KMS + IRSA in AWS)

#### 2.2 Scripts/templates you must maintain (repo deliverables)

- Ubuntu bootstrap scripts (packages, kernel modules, sysctl, users)
- Docker install scripts
- kubeadm cluster install scripts (control-plane + join workers)
- Minikube install/config scripts
- Jenkins install + reference pipeline templates
- Ansible baseline playbooks (OS + tools)
- Kafka/Zookeeper docker-compose + optional Kubernetes manifests
- Prometheus/Grafana install and values/config templates
- OTel collector config templates
- AWS IaC skeleton (Terraform/CloudFormation) for VPC, EKS, ECR, MSK, AMP/AMG, RDS/Redis, EFS/S3

---

### 3) RACI (roles and responsibilities)

#### Roles

- **Admin (Platform/SRE)**: infra + OS + cluster + security + monitoring baseline
- **Dev (Platform engineer / game devops)**: pipelines, containerization, K8s manifests, services
- **BA (Business Analyst / Product/QA)**: requirements, user roles definition, acceptance checks, test scenarios

#### RACI matrix (sample)

- **VPC/EKS/MSK provisioning**: Admin (R/A), Dev (C), BA (I)
- **Jenkins/CodePipeline workflows**: Dev (R/A), Admin (C), BA (I)
- **Dashboards/alerts**: Admin (R), Dev (C), BA (I)
- **Game service container & deployment**: Dev (R/A), Admin (C), BA (C)
- **Runbooks & incident response**: Admin (R/A), Dev (C), BA (I)

---

### 4) 22‑day scope control (how we finish in 22 days)

This is how we avoid “infinite platform build”:

- **One reference game workload** only (plus Xonotic server as a local test).
- **One K8s distribution** (kubeadm for on-prem, EKS for AWS).
- **One streaming backbone** (Kafka local, MSK AWS).
- **One CI/CD path** (Jenkins local, CodePipeline AWS).
- **One observability path** (Prom+Grafana local, AMP/AMG AWS, OTel everywhere).

Out-of-scope (explicitly deferred unless time remains):

- Multi-region DR, global traffic management
- Multi-tenant isolation per studio with separate clusters
- Service mesh (Istio/Linkerd), unless required
- Full-featured secrets platform (Vault) beyond baseline

---

### 5) Acceptance criteria (what “done” means)

You can claim completion when all are true:

- **Build**: A repo push triggers CI, produces a container image, and stores it in a registry (local or ECR).
- **Deploy**: The image is deployed into Kubernetes with a versioned rollout and rollback.
- **Run**: At least **3 simulated users** can connect/use the game workload (or a multi-user service).
- **Stream**: Game events flow into Kafka/MSK and are consumed successfully.
- **Observe**: Dashboards show health + latency + resource usage; alerts exist for core failure states.
- **Operate**: Runbooks exist for top incidents (node loss, pod crash loops, Kafka broker failure, DB failover in AWS).
- **Repeatability**: A new engineer can reproduce the platform using the scripts/templates with minimal manual steps.

---

### 6) Risks & mitigations (principal architect review)

- **Risk: Under-spec’d hardware causes instability**
  - **Mitigation**: publish minimum server sizing + reserve resources for control-plane
- **Risk: kubeadm cluster networking breaks (CNI/IPTables)**
  - **Mitigation**: standardize CNI; lock kernel modules/sysctl; keep a known-good config
- **Risk: Jenkins becomes snowflake**
  - **Mitigation**: “configuration as code” (JCasC), pipeline-as-code, store configs in repo
- **Risk: Kafka multi-node complexity**
  - **Mitigation**: start with Docker compose single-node, then scale to 3 brokers; use clear test producer/consumer
- **Risk: Observability added too late**
  - **Mitigation**: install Prom/Grafana early and keep dashboards updated each day
- **Risk: Security ignored until the end**
  - **Mitigation**: day-by-day RBAC and user separation; in AWS enforce IRSA and least privilege

