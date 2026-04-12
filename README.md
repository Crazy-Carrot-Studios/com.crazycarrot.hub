# CCS Hub (com.crazycarrot.hub)

**Version:** 0.2.42

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

**CCS Character Controller:** Optional install uses **`com.crazycarrot.charactercontroller`** from Git, pinned to **`v0.1.9-preview.1`** (Base Locomotion preview; default camera profile under **`Scripts/Profiles/camera/`**). Expect **New Input System** and **Cinemachine** from Hub **required** installs. The Console logs **`[CCS Hub] Installed successfully: …`** with the resolved **version** after each successful Git package add, and additional lines when the controller is copied to **Assets** and when the UPM entry is removed.

---

## Requirements

- Unity 6000.3+

---

## Versioning (Hub and Branding)

These are **two separate UPM packages**, each with its **own** `version` in its own `package.json`:

| Package | Version source | Typical use |
|--------|----------------|-------------|
| **com.crazycarrot.hub** | Hub repo `package.json` (e.g. **0.2.37**) | Hub release you add from Git; changelog tracks Hub-only fixes. |
| **com.crazycarrot.branding** | Branding repo `package.json` (e.g. **1.1.x**) | Resolved when UPM installs branding from Git; shown in Package Manager for that package. |

**Important:** Hub **does not** list Branding as a nested `dependencies` entry (Unity requires semver-only there for nested deps). Branding is installed on **first run** from **`CCSDependencyManifest.json`** via **Git URL**. The combination—**Hub version** + **Branding version** as installed from the branding repo—is the supported, validated setup.

---

## Notes

- First-run setup runs once per project
- Hub will not auto-open again after setup is completed or skipped
- **CCS Branding** and other required packages are installed by the Hub bootstrap (Git URLs from the dependency manifest), not as nested entries in Hub’s `package.json` — this keeps **Add package from Git URL** working in Unity’s Package Manager
