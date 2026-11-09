---
title: Short sentence saying what changed
impact: recommended
features: []
platforms: []
---

<!--
Save as docs/upgrades/YYYY-MM-DD-short-slug.md, dated the day you write it. The file name is the note's id:
never rename a note once it has merged.

impact:    breaking    - apps stop building or working on a new toolkit or SDK until they do this
           recommended - a fix or improvement every app should take
           optional    - nice to have, or only relevant to some apps
features:  feature ids from .template.config/wizard.json. Empty means it concerns every app.
platforms: android, ios, tvos, windows, desktop, wasm. Empty means every platform.

The reader is a developer or agent working in an app that has drifted from the template. They don't have your
context and they've renamed AppTemplate. Write AppTemplate anyway; they know to read it as their app's name.
Links must be absolute: https://github.com/MartinZikmund/uno-app-template/blob/main/...
Delete this comment.
-->

## Why

What was wrong or missing, and what an app gains by doing this.

## Who is affected

Which apps need this, and how to tell. "Skip this if ..." is welcome.

## Steps

Numbered and exact. Show the real before and after for anything an app may have changed. Say which steps need
something a patch can't deliver, such as secrets, store settings or new certificates.

## Verify

The command or check that proves it worked.
