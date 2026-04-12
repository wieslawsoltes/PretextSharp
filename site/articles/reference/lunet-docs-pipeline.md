---
title: "Lunet Docs Pipeline"
---

# Lunet Docs Pipeline

The docs site is built with Lunet from the `site/` folder.

## Files

- `.config/dotnet-tools.json`: pins the local `lunet` tool
- `build-docs.sh`: restores the tool and builds the site
- `serve-docs.sh`: restores the tool and starts a local Lunet preview server
- `site/config.scriban`: site identity and theme wiring
- `site/menu.yml`: top-level navigation
- `site/articles/**`: curated docs content for Pretext
- `site/.lunet/includes/_builtins/bundle.sbn-html`: local bundle-link override so API pages emit valid CSS and JS URLs
- `.github/workflows/docs.yml`: publishes the generated site to GitHub Pages
- `.github/workflows/ci.yml`: validates that docs still build on CI

## Local build

```bash
dotnet tool restore
./build-docs.sh
```

The generated site is written under `site/.lunet/build/www`.

## Local preview

```bash
./serve-docs.sh
```

Pass additional Lunet `serve` arguments through the script when needed.

## Notes

- Generated output is ignored in `.gitignore`
- `ci.yml` uploads a docs preview artifact
- This site currently focuses on authored docs rather than generated API reference pages
