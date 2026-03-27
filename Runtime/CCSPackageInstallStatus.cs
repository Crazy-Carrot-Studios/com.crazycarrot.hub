// ============================================================================
// Project: Crazy Carrot Studios (CCS) - CCS Hub
// Script: CCSPackageInstallStatus
// GameObject: N/A
// Author: James Schilz (Developer)
// Created: March 25, 2025
// Last Modified: March 25, 2025
// Summary: UI and service state for a registry entry relative to Package Manager and the install queue.
// Required Components: None
// Where to Place: Packages/com.crazycarrot.hub/Runtime/
// ============================================================================

namespace CCS.Hub
{
    public enum CCSPackageInstallStatus
    {
        Unknown = 0,
        NotInstalled = 1,
        Installed = 2,
        Pending = 3,
        Installing = 4,
        Failed = 5,
        Manual = 6,

        /// <summary>Queue skipped this entry because the package was already present (no Client.Add).</summary>
        Skipped = 7
    }
}
