# CCS Hub (`com.crazycarrot.hub`)

First-run **bootstrap installer** and package hub for **Crazy Carrot Studios** Unity projects. After you add this package by Git URL, Unity runs a **CCS Setup Wizard** (once per project until you complete, skip, or reset) that can install required Unity packages and optional CCS Git packages **sequentially** via Package Manager, and scaffold **`Assets/CCS`** content folders.

## Standalone Git / UPM repository

This package is maintained in its **own** Git repository. **`package.json` lives at the repository root** (not under another project’s `Packages/` folder). Unity’s **Add package from Git URL** clones that repo and treats the root as the UPM package, so installs stay clean. Optional: pin a release with `#v0.1.0` on the URL.

## Requirements

- Unity **6000.3** (developed against **6000.3.10f1**)

## Fresh project workflow

1. Create a Unity 6 project.
2. **Window → Package Manager → + → Add package from Git URL**
3. Install: `https://github.com/Crazy-Carrot-Studios/com.crazycarrot.hub.git`
4. After compile, the **CCS Setup Wizard** opens automatically (unless you already completed or skipped setup for this project).
5. Choose optional CCS packages, then **Install Selected** or **Install Required Only**.
6. Use **Create CCS Project Folders** as needed.

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
