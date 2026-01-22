## Git cheatsheet (Admin / Developer / Business Analyst)

### Role model (simple and workable)

- **Administrator**
  - Manages repos, branch protections, CI secrets, access control
  - Merges to `main` (or `release/*`)
- **Developer**
  - Works in feature branches, opens PRs, writes commits
- **Business Analyst (BA)**
  - Reads code, reviews docs, opens issues, tags releases/notes (no direct merges)

### Everyday commands (Developer)

- **Clone**
  - `git clone <repo>`
- **Create a branch**
  - `git checkout -b feature/<name>`
- **Check changes**
  - `git status`
  - `git diff`
- **Stage and commit**
  - `git add .`
  - `git commit -m "Short message"`
- **Pull latest**
  - `git pull origin <branch>`
- **Push branch**
  - `git push -u origin feature/<name>`
- **Update from main**
  - `git fetch origin main`
  - `git rebase origin/main`  (or `git merge origin/main`)
- **Resolve conflicts**
  - edit files -> `git add <file>` -> `git rebase --continue`
- **Tag a release**
  - `git tag -a v0.1.0 -m "Release v0.1.0"`
  - `git push origin v0.1.0`

### Safe practices

- **Never commit secrets** (tokens, passwords, kubeconfig)
- **Small commits**: each commit should have one purpose
- **Rebase before merge** to reduce conflicts (team choice)

### Admin-only operations

- **Branch protections**
  - Require PR review, require CI, block force-push
- **Release management**
  - Tag versions, create release notes

