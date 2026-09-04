# Documentation

## Where docs live

- **`docs/<topic>.md`** — one page per topic. Feature documentation goes here, always.
- **`docs/README.md`** — the index. Add one line for every new page, under the right heading, in
  alphabetical order within that group.
- **`README.md`** — the repository's front door: what the template is, a linked feature list, how
  to adopt it, and a quickstart. It is **not** a manual.

## The rule

**Never append feature prose to `README.md`.** Adding a documented feature means:

1. Write `docs/<topic>.md`.
2. Add one line to `docs/README.md`.
3. Only if the feature belongs in the headline list, add a row to README's "What's in the box"
   table — a sentence and a link, never the explanation itself.

**Why:** every feature branch that appends to README edits the same region of the same file, so
each one conflicts with every other and with `main`. Eight open pull requests once had to be
hand-merged for exactly this reason. A new file under `docs/` cannot conflict with anything, which
turns a recurring merge tax into a non-event.

`## Versioning` in README is the shape to copy: a short paragraph that links to
[`docs/versioning.md`](../../docs/versioning.md) and stops.

## Naming

Name the file after the topic, not the issue or the PR: `serial-disposable.md`, not
`issue-14-docs.md`. Lowercase, hyphenated, `.md`.

## Style

- Lead with what the reader is trying to do, not with a definition.
- Show the real API from this repo. If an example type doesn't exist in `src/`, say so explicitly —
  an illustrative placeholder is fine, a fictional API presented as real is not.
- Code samples carry their `using` directives when the reader is meant to copy them.
- Link related pages rather than restating them.
