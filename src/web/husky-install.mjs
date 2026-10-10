// Runs on `npm install` (the "prepare" script) and points git at the hooks in /.husky.
// package.json lives here in src/web, but the hooks belong to the whole repository.

// CI and image builds do not commit: nothing to install there.
if (process.env.CI === 'true' || process.env.NODE_ENV === 'production') {
  process.exit(0)
}

process.chdir('../..')
const { default: husky } = await import('husky')
const message = husky('.husky')
if (message) {
  console.log(message)
}
