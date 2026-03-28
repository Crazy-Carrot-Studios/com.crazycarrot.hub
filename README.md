# CCS Hub (`com.crazycarrot.hub`)

**Version: 0.2.4**

First-run **bootstrap installer** and package hub for **Crazy Carrot Studios** Unity projects. After you add this package by Git URL, Unity runs a **CCS Setup Wizard** (once per project until you complete, skip, or reset) that can install required Unity packages and optional CCS Git packages **sequentially** via Package Manager, and scaffold **`Assets/CCS`** content folders.

## Standalone Git / UPM repository

This package is maintained in its **own** Git repository. **`package.json` lives at the repository root** (not under another project’s `Packages/` folder). Unity’s **Add package from Git URL** clones that repo and treats the root as the UPM package, so installs stay clean. Optional: pin a release with `#v0.2.4` (or another tag) on the URL.

## Requirements

- Unity **6000.3** (developed against **6000.3.10f1**)

## Fresh project workflow

1. Create a Unity 6 project.
2. Add the hub using **one** of these methods:

   **A — Package Manager (Git URL field)**  
   **Window → Package Manager → + → Add package from Git URL**  
   In the URL box, paste **only** this (no label, no `Install:` prefix, no backticks):

   `https://github.com/Crazy-Carrot-Studios/com.crazycarrot.hub.git`

   If Unity says the package name is invalid and shows `Install: https://...`, you pasted the wrong text—use the bare URL above.

   **B — Edit `Packages/manifest.json` manually**  
   Inside the `"dependencies"` object, add:

   `"com.crazycarrot.hub": "https://github.com/Crazy-Carrot-Studios/com.crazycarrot.hub.git"`

   Save the file and return to Unity so it resolves the package.

3. After compile, the **CCS Hub — Setup** progress window opens for first-run required installs; when those finish and the editor is stable, **CCS Hub** opens for optional tools (unless you already completed or skipped setup for this project).
4. Use **CCS → CCS Hub** for optional tools; choose **CCS Character Controller** and **Install selected**.

## Character Controller (UPM source → Assets/CCS)

Unity Package Manager **never** writes optional CCS tools straight into `Assets/`; Git packages resolve under **`Packages/`**. CCS Hub uses a **two-step** flow so the result still feels like **Assets → Import Package**:

1. **Ensure source:** `Client.Add` installs **`com.crazycarrot.charactercontroller`** from Git into **`Packages/com.crazycarrot.charactercontroller/`** (standard UPM layout).
2. **Import / bootstrap:** The Hub copies **only** Character Controller content into **`Assets/CCS/CharacterController/`** (not the entire Git repo). If the package embeds a dev tree at **`Assets/CCS/CharacterController`**, it copies whitelisted folders there (**`Scripts`**, **`Content`**, **`Animations`**, **`Editor`**, **`Runtime`**, **`Samples~`**, **`Plugins`**) and **ignores** nested template assets such as **`Starter Assets`**, duplicate **`Assets/`**, **`Packages/`**, **`ProjectSettings/`**, **`Docs/`**, template **Scenes/Settings** at the repo root, etc. If there is no embedded layout, it copies standard UPM roots (**`Runtime`**, **`Editor`**, **`Content`**, …). It skips the repository **`package.json`** under that destination, materializes **`Samples~/BasicSetup`** into **`BasicSetup`** when present, then **`Client.Remove`** the package entry so you do not compile the same scripts twice.

**What appears under `Assets/CCS/CharacterController`:** the CCS controller sources (scripts, content, animations, optional plugins)—not a full Unity template project.

**Limitations / next steps:** Re-install overwrites that folder (destructive). If **`Client.Remove`** fails after copy, remove the package manually in Package Manager to avoid duplicate assemblies. The **`com.crazycarrot.charactercontroller`** Git repo should **not** ship a full sample project inside the package; keep template scenes/settings only in the **consumer** project’s **`Assets/`** (not under **`Assets/CCS`**). Updating Character Controller later may require a documented “replace from package” or Git workflow—the Hub does not diff-merge versions yet.

Manual menu:

- **CCS → CCS Hub**

## Hybrid dependency model

- **Do not rely on `package.json` alone** for optional CCS tools. The wizard uses `UnityEditor.PackageManager.Client.Add` for registry and Git URLs.
- **Required and optional package lists** are **data-driven** from `Runtime/Resources/CCSDependencyManifest.json` inside this package (pinned Unity packages use `installIdentifier` like `com.unity.inputsystem@1.18.0`). Extend that file to add more required or optional rows without scattering IDs across scripts.
- **CCS Branding** and core Unity packages (**Input System**, **Cinemachine**) are installed from the manifest **required** array, not only as transitive `package.json` dependencies.
- **DOTween** is listed under **Manual / Special**: the official Demigiant flow is not treated as a guaranteed one-click Git UPM install. Import via Asset Store or your approved internal workflow.

## Sequential installs

Only **one** `Client.Add` runs at a time. A queue persists pending definition ids in **Session State** so domain reloads can resume the **pending queue** (in-progress `AddRequest` cannot be resumed across reloads; refresh status and re-run if needed).

## Repository

https://github.com/Crazy-Carrot-Studios/com.crazycarrot.hub

---

**Version 0.2.4** (same value as `package.json` `"version"`).
