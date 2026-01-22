## Ansible baseline (production management)

This folder is a starter for OS and tool management via Ansible.

### Suggested inventory groups

- `k8s_control_plane`
- `k8s_workers`
- `ci_servers`
- `monitoring_servers`

### Quick start

- Create inventory in `ansible/inventory.ini`
- Run:
  - `ansible -i inventory.ini all -m ping`

