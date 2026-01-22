## OS-level tuning notes (Ubuntu) for Gaming + K8s + Kafka

### CPU & scheduling

- **Pin noisy neighbors**: reserve CPU for control plane (kube-apiserver, etcd)
- **CPU limits**: avoid aggressive CPU throttling for latency-sensitive game loops

### Memory

- Ensure kubelet eviction thresholds are understood (avoid sudden evictions)
- Disable swap for Kubernetes nodes

### Network

- **conntrack**: increase for large numbers of concurrent connections
- **TCP backlog**: adjust `somaxconn`, `tcp_max_syn_backlog` for spikes
- **MTU**: verify CNI MTU matches underlay to avoid fragmentation

### File descriptors / IO

- Raise `nofile` limits for Kafka and game servers
- Use SSD/NVMe for Kafka logs and container images if possible

### Example sysctl (baseline)

These are starting points; test before applying broadly:

- `net.ipv4.ip_forward=1`
- `net.core.somaxconn=1024`
- `net.netfilter.nf_conntrack_max=262144`

