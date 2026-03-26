# CCS Hub (`com.crazycarrot.hub`)

First-run **bootstrap installer** and package hub for **Crazy Carrot Studios** Unity projects. After you add this package by Git URL, Unity runs a **CCS Setup Wizard** (once per project until you complete, skip, or reset) that can install required Unity packages and optional CCS Git packages **sequentially** via Package Manager, and scaffold **`Assets/CCS`** content folders.

## Standalone Git / UPM repository

This package is maintained in its **own** Git repository. **`package.json` lives at the repository root** (not under another project’s `Packages/` folder). Unity’s **Add package from Git URL** clones that repo and treats the root as the UPM package, so installs stay clean. Optional: pin a release with `#v0.1.0` on the URL.

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

3. After compile, the **CCS Hub** window may open automatically (unless you already completed or skipped setup for this project).
4. Use **CCS → CCS Hub** for optional tools; use **Install selected** for Character Controller (imports into **Assets/CCS/CharacterController**, not as a separate UPM package).

Manual menus:

- **CCS → CCS Hub**
- **CCS → Developer → Reset Setup State** (testing)

## Hybrid dependency model

- **Do not rely on `package.json` alone** for optional CCS tools. The wizard uses `UnityEditor.PackageManager.Client.Add` for registry and Git URLs.
- **CCS Branding** and core Unity packages (**Input System**, **Cinemachine**) are installed from the wizard’s **Required** list, not only as transitive `package.json` dependencies.
- **DOTween** is listed under **Manual / Special**: the official Demigiant flow is not treated as a guaranteed one-click Git UPM install. Import via Asset Store or your approved internal workflow.

## Sequential installs

Only **one** `Client.Add` runs at a time. A queue persists pending definition ids in **Session State** so domain reloads can resume the **pending queue** (in-progress `AddRequest` cannot be resumed across reloads; refresh status and re-run if needed).

## Repository

https://github.com/Crazy-Carrot-Studios/com.crazycarrot.hub
