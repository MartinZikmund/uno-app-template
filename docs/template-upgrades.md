# Upgrading this app to a newer template

This app was created from the [Uno App template](https://github.com/MartinZikmund/uno-app-template).
When the template gets a fix or a feature, you can bring it across without starting again.

`.template-state.json` remembers which template version and features you chose, and which upgrade
notes this app already has. Keep it committed.

## See what's pending

```bash
dotnet tool update -g MZikmund.UnoApp.Cli    # or: dotnet tool install -g
mzuno-app upgrade                                     # lists the notes this app hasn't applied
```

The list only includes notes for features you actually have.

## Apply the changes

Each note explains why the change exists and gives exact steps. Take them one at a time:

```bash
mzuno-app upgrade --show <id>            # print a note's steps
mzuno-app upgrade --mark-applied <id>    # record it once done
```

For the mechanical part, ask the tool to merge the template's own changes into your code. It generates
the old and new template output with your app's name and options, then three-way merges the
difference, leaving ordinary conflict markers where your code diverged:

```bash
mzuno-app upgrade --patch
```

Do this on a clean branch and review the result like any other change. Files merge one at a time, so
if your app has deleted or replaced something the template changed, that file is listed as skipped and the rest
still arrive. It updates the template version in `.template-state.json` but doesn't mark notes as applied, because
notes can carry steps a merge can't do.

<!--#if (useAgents)-->
## Using a coding agent

Ask your agent to "upgrade this app to the latest template". The
[`upgrade-from-template`](../.claude/skills/upgrade-from-template/SKILL.md) skill covers the workflow.

<!--#endif-->
## Without the tool

Read the notes in the template's
[`docs/upgrades/`](https://github.com/MartinZikmund/uno-app-template/tree/main/docs/upgrades) folder.
Each file name is its id. Apply any id missing from `appliedUpgrades` in `.template-state.json` whose
`features` match yours, then add the id to that list.
