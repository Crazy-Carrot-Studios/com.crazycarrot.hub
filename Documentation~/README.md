# CCS Hub — Package documentation

This folder holds **embedded documentation** for the **CCS Hub** UPM package (`com.crazycarrot.hub`).

Unity treats the `Documentation~` directory as optional package docs (not imported as runtime assets). Content here is for maintainers and for anyone browsing the package in a clone or Git checkout.

For the **authoritative** install and workflow guide, see the package **`README.md`** at the repository root and **`CHANGELOG.md`**.

---

## Versioning (maintainers)

- **Hub** and **Branding** use **independent** semantic versions in their respective `package.json` files.
- Do **not** add `com.crazycarrot.branding` to Hub’s `dependencies` for UPM validation reasons; required installs use the **manifest** + **Git URL** (see root `README.md` → *Versioning*).
- When releasing, bump Hub’s version for Hub changes; bump Branding’s version in the [branding](https://github.com/Crazy-Carrot-Studios/com.crazycarrot.branding) repo for branding-only changes.

## Character Controller (optional CCS package)

The **CCS Character Controller** optional install assumes **com.unity.inputsystem** and **com.unity.cinemachine** are already present. Those versions are pinned under **`required`** in `Runtime/Resources/CCSDependencyManifest.json` and installed in the Hub first-run **required** phase—not as nested entries in Hub’s `package.json`.

---

**Crazy Carrot Studios**
