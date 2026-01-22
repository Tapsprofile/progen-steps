## Backup, snapshots, and recovery (multi-node gaming platform)

### What must be backed up (minimum viable)

- **Git**: source of truth (repo hosting already handles this)
- **CI/CD**:
  - Jenkins: `JENKINS_HOME` volume (jobs, plugins, credentials)
  - Pipeline definitions must be in Git (preferred)
- **Kubernetes**
  - Cluster state: manifests/Helm values in Git (GitOps-like)
  - Persistent data: PVCs (game configs/logs, databases if self-managed)
- **Kafka**
  - Topic config + ACLs (export scripts)
  - Data retention strategy (don’t “backup Kafka” blindly; design replay from source)
- **Observability**
  - Grafana dashboards (export JSON, store in Git)
  - Prometheus retention and remote storage if required

### Snapshot approach (practical)

- **On-prem/local**
  - **Configs**: stored in Git + replicated
  - **Volumes**: filesystem snapshots if using LVM/ZFS/CSI snapshot features
  - **Jenkins**: nightly tarball snapshot of `JENKINS_HOME` + plugin list
- **AWS**
  - **EFS**: AWS Backup plan (daily + weekly retention)
  - **RDS**: automated backups + PITR; Multi-AZ for HA
  - **ElastiCache Redis**: snapshot + replication group (if needed)
  - **S3**: versioning + lifecycle rules + access logs

### Recovery drills (must be practiced)

- Restore Jenkins from backup and run a pipeline
- Recreate cluster from IaC and redeploy apps
- Validate Kafka consumer can reprocess events (replay) without data loss assumptions

