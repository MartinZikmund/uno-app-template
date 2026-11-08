# Upgrade notes

One file per template change that an existing app needs to act on, so an app can catch up without reading a
commit log. `mzuno-app upgrade` reads these files and lists the ones a given app still needs, based on the features
and notes recorded in its `.template-state.json`.

- **Upgrading an app?** See [template-upgrades.md](../template-upgrades.md).
- **Changing the template?** See [`.claude/rules/template-upgrades.md`](../../.claude/rules/template-upgrades.md),
  and copy [`_TEMPLATE.md`](./_TEMPLATE.md). CI fails a pull request that changes what apps receive without a note.

There's deliberately no index to maintain here: the folder is the list, sorted by date because every file name
starts with one. A new file can't conflict with another pull request.
