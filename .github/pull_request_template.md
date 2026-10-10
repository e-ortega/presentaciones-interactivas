## What changes

<!-- What this pull request does and why, in a few lines. -->

## How it was verified

<!-- Tests run, and anything checked by hand. Say what was NOT verified too. -->

## Review checklist

Automatic, enforced by CI:

- [ ] Server builds with no warnings or analyzer findings, and `dotnet format` reports no changes
- [ ] Frontend passes the type-aware lint with no warnings, tests and build
- [ ] The app runs in its container and imports a deck (smoke test)

By the author, before merging:

- [ ] The diff was reviewed line by line by someone or something other than who wrote it, and the findings are fixed or listed below
- [ ] New behavior has a test that fails without the change
- [ ] No secrets, personal data or real decks in the diff (the repository is public)
- [ ] If an endpoint or DTO changed: `npm run generate:api` was run and `schema.d.ts` is committed
- [ ] If a decision was made: it is recorded in `docs/decisiones.md`

## Review findings

<!-- What the review found and what was done about each point. "None" is a valid answer. -->
