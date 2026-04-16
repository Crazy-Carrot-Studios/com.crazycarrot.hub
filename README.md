# CCS Hub (com.crazycarrot.hub)

**Version:** 0.2.48

First-run setup and package hub for Crazy Carrot Studios Unity projects.

---

## Install

In Unity:

Window → Package Manager → Add package from Git URL

Copy this URL:

```
https://github.com/Crazy-Carrot-Studios/com.crazycarrot.hub.git
```

---

## What it does

- Installs required CCS dependencies automatically
- Shows setup progress
- Opens CCS Hub for optional installs

---

## Open Hub Manually

CCS → CCS Hub → Open CCS Hub

---

## Optional Installs

Use the Hub to install optional CCS tools into:

Assets/CCS/

**CCS Git packages (Branding, Character Controller, catalog tools):** The Hub manifest uses **`#main`** on each `github.com/Crazy-Carrot-Studios/...git` URL so Unity’s Package Manager resolves the **latest commit on the default branch** when you install or when the manifest entry changes. **Character Controller** still bootstraps into **`Assets/CCS/CharacterController`** (see [controller README](https://github.com/Crazy-Carrot-Studios/com.crazycarrot.charactercontroller/blob/main/README.md)). For a **frozen** release, add the package manually in **`Packages/manifest.json`** with **`#vX.Y.Z`** or a commit hash instead. If Unity keeps an old checkout, remove **`Library/PackageCache`** entries for that package or use **Package Manager → Update** after pushing to **`main`**.

---

## Requirements

- Unity 6000.3+

---

## Versioning (Hub and Branding)

These are **two separate UPM packages**, each with its **own** `version` in its own `package.json`:

| Package | Version source | Typical use |
|--------|----------------|-------------|
| **com.crazycarrot.hub** | Hub repo `package.json` (e.g. **0.2.48**) | Hub release you add from Git; changelog tracks Hub-only fixes. |
| **com.crazycarrot.branding** | Branding repo `package.json` (e.g. **1.1.x**) | Resolved when UPM installs branding from Git; shown in Package Manager for that package. |

**Important:** Hub **does not** list Branding as a nested `dependencies` entry (Unity requires semver-only there for nested deps). Branding is installed on **first run** from **`CCSDependencyManifest.json`** via **Git URL**. The combination—**Hub version** + **Branding version** as installed from the branding repo—is the supported, validated setup.

---

## Notes

- First-run setup runs once per project
- Hub will not auto-open again after setup is completed or skipped
- **CCS Branding** and other required packages are installed by the Hub bootstrap (Git URLs from the dependency manifest), not as nested entries in Hub’s `package.json` — this keeps **Add package from Git URL** working in Unity’s Package Manager
