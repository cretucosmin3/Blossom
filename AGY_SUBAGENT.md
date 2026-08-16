# Using `agy` as implementer

`agy` = Antigravity CLI (`~/.local/bin/agy`).

**Default automation:** parent gives one prompt → AGY follows an existing plan → updates progress → writes a **short** result under `plans/agy-runs/`.

Full workflow: **[plans/agy-runs/WORKFLOW.md](./plans/agy-runs/WORKFLOW.md)**  
Prompt body: **[plans/agy-runs/PROMPT_TEMPLATE.md](./plans/agy-runs/PROMPT_TEMPLATE.md)**

---

## Critical: workspace (`--add-dir`)

Print-mode AGY often uses a **scratch workspace** under `~/.gemini/antigravity-cli/scratch/` instead of the git repo you `cd`’d into. Files then look “done” in the reply but **never appear in the real project**.

**Always:**

1. `cd` to the repo root **and**
2. Pass **`--add-dir` with the absolute repo path**
3. State the workspace path in the prompt (absolute paths for deliverables help)

```bash
REPO="/absolute/path/to/this/repo"   # e.g. /home/you/Documents/GitHub/MyProject
cd "$REPO"

agy -p "$(cat /tmp/agy-prompt.md)" \
  --add-dir "$REPO" \
  --dangerously-skip-permissions \
  --mode accept-edits \
  --print-timeout 45m \
  --output-format text
```

After a run, confirm files with `ls` / `git status` under `$REPO`, not only AGY’s chat summary.

**Smoke-test lesson:** without `--add-dir`, create-file landed in scratch; with `--add-dir "$REPO"` + absolute paths in the prompt, files landed in the real repo.

Parent agents (including Grok) must include `--add-dir <absolute-repo-root>` on every unattended `agy -p` invoke.

---

## Flow

```text
Parent points AGY at plan + progress (+ run-id)
  → agy -p "…" --add-dir <REPO> --dangerously-skip-permissions --mode accept-edits
  → AGY implements + ticks progress
  → AGY writes plans/agy-runs/<run-id>.md  (short: done / not done / errors)
  → Parent reads that file + git
```

Result files are **summaries only** — not a file list (use git for diffs).

---

## Launch

```bash
REPO="/absolute/path/to/this/repo"
cd "$REPO"

agy -p "$(cat /tmp/agy-prompt.md)" \
  --add-dir "$REPO" \
  --dangerously-skip-permissions \
  --mode accept-edits \
  --print-timeout 45m \
  --output-format text
```

| Flag | Why |
|---|---|
| `--add-dir <REPO>` | **Required** — bind real git workspace (avoid scratch) |
| `-p` | One-shot |
| `--dangerously-skip-permissions` | Unattended tools (no TTY prompts) |
| `--mode accept-edits` | Apply code changes |
| `--print-timeout 45m` | Coding needs more than default 5m |
| `--model` / `--effort` | Optional |
| `--output-format json` | Optional machine final blob |

Print mode has no permission TTY → skip-permissions is required for real work.

Interactive (human terminal): `agy` or `agy -i "…"` from the repo — you approve tools. Still prefer working inside the real project, not scratch.

---

## Parent checklist

1. Set `REPO` absolute path; always pass `--add-dir "$REPO"`.  
2. Pick plan + progress under `plans/` (or the plan path for this task).  
3. Fill prompt (paths + run-id + scope); mention workspace is `$REPO`.  
4. Run AGY.  
5. Verify with `git status` under `$REPO`; read `plans/agy-runs/<run-id>.md`.  
6. Next stage if needed.

Project rules (if present): [AGENTS.md](./AGENTS.md).
