# Local Markdown issue tracker

Store each feature in `.scratch/<feature-slug>/`.

- Publish the spec as `spec.md`.
- Write each implementation ticket in `issues/<NN>-<slug>.md`, numbered from `01`.
- Record triage state near the top as `Status: <label>`. See `triage-labels.md`.
- Append discussion under `## Comments`.
- Fetch a ticket by its path or by its number within the feature.

## Wayfinder

Keep the effort map in `map.md`, with Notes, Decisions-so-far, and Fog sections.
Put each question in its own numbered issue. Record its type on a `Type:` line
using `research`, `prototype`, `grilling`, or `task`. List dependencies on a
`Blocked by:` line using ticket numbers.

Claim the lowest-numbered open, unclaimed ticket whose dependencies are all
resolved. Save `Status: claimed` before starting. When finished, append the
answer under `## Answer`, set `Status: resolved`, and add a summary and ticket
link to Decisions-so-far in `map.md`.
