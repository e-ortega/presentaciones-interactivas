// What runs on `git commit`, only for the kinds of files being committed (see .husky/pre-commit).
// The goal is feedback in a couple of seconds; the full checks still run in CI.
import path from 'node:path'

const quote = (file) => `"${path.relative(process.cwd(), file)}"`

export default {
  // Frontend: lint and type-check the whole project, not just the staged files,
  // because a change in one file can break the types of another.
  'src/web/**/*.{ts,tsx}': () => ['npm --prefix src/web run lint', 'npm --prefix src/web run typecheck'],

  // Server: fix whitespace and indentation in the staged files; lint-staged adds the fixes
  // to the commit. This mode does not load the solution, so it takes about 2 seconds
  // instead of 11. Generated migrations are left alone.
  '*.cs': (files) => {
    const sources = files.filter((file) => !file.includes('/Migrations/'))
    return sources.length === 0 ? [] : `dotnet format whitespace . --folder --include ${sources.map(quote).join(' ')}`
  },
}
