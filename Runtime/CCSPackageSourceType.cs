// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSPackageSourceType
// GameObject: N/A
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: Identifies how a registry entry is installed or presented in the hub.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Runtime/
// ============================================================================

namespace CCS.Hub
{
    public enum CCSPackageSourceType
    {
        UnityRegistry = 0,
        GitUrl = 1,
        Manual = 2
    }
}
