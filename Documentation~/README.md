# CCS Hub — Documentation

## Overview

**CCS Hub** is a first-run **bootstrap installer** for CCS projects. UPM package code stays under **Packages** (or Package Cache). The wizard can create **project content** under **`Assets/CCS`** only (folders), not embedded package files.

## Package Manager

Installs use **`UnityEditor.PackageManager.Client.Add`**. Only **one** add operation runs at a time; the hub queues the rest. Pending queue ids are stored in **Session State** so a **pending** list can survive **script reload** (an in-flight request cannot be resumed).

## Hybrid dependencies

`package.json` intentionally keeps **`dependencies` empty** so optional CCS tools are not pulled only by static references. **CCS Branding**, **Input System**, and **Cinemachine** are installed from the wizard’s **Required** section (or **Install Required Only**).

## DOTween

**DOTween** is listed under **Manual / Special**. The official Demigiant distribution is **not** treated as a reliable one-click Git UPM install from this hub.

## Extending the registry

Edit **`CCSPackageRegistry`** in `Runtime/CCSPackageRegistry.cs` to add rows. Use **`CCSPackageDefinition`** and the category / source enums for consistent UI and install behavior.
