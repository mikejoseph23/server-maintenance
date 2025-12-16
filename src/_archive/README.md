# Archive

This folder contains legacy versions of the ServerMaintenance application kept for reference and backward compatibility.

## ServerMaintenance.NetFramework

The original .NET Framework 4.7.2 implementation. This version was archived in December 2024 when the application was migrated to .NET 8 to resolve assembly loading issues (`Microsoft.SqlServer.BatchParser.dll`) on newer Azure servers.

**Do not use for new deployments.** Use the current `src/ServerMaintenance` (.NET 8) version instead.
