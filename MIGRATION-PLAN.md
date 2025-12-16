# Migration Plan: .NET Framework 4.7.2 → .NET 8

## Summary

**Goal:** Migrate the ServerMaintenance console application from .NET Framework 4.7.2 to .NET 8 to resolve the `Microsoft.SqlServer.BatchParser.dll` assembly loading issue on newer Azure servers.

**Key Objectives:**
- Replace legacy csproj with SDK-style project format
- Migrate configuration from App.config to appsettings.json
- Update all code to use modern `IConfiguration` pattern
- Support flexible SMTP configuration (pickup directory or network)

**Critical Success Factors:**
- Application builds and runs on .NET 8
- SQL backup functionality works with updated SMO packages
- Email notifications work with both SMTP modes
- No regression in existing functionality

---

### Progress: 89% Complete (8/9 tasks)

| Phase | Status | Tasks |
|-------|--------|-------|
| Project Setup | Complete | 2/2 |
| Code Migration | Complete | 4/4 |
| Cleanup & Validation | In Progress | 2/3 |

**Note:** Original .NET Framework project archived to `src/_archive/ServerMaintenance.NetFramework/`

---

## Table of Contents

1. [Phase 1: Project Setup](#phase-1-project-setup)
   - [1.1 Replace Project File](#11-replace-project-file)
   - [1.2 Create Configuration File](#12-create-configuration-file)
2. [Phase 2: Code Migration](#phase-2-code-migration)
   - [2.1 Update Program.cs](#21-update-programcs)
   - [2.2 Update MaintenanceMan.cs](#22-update-maintenemanancs)
   - [2.3 Update SqlScriptRunner.cs](#23-update-sqlscriptrunnercs)
   - [2.4 Update FolderCleaner.cs](#24-update-foldercleanercs)
3. [Phase 3: Cleanup & Validation](#phase-3-cleanup--validation)
   - [3.1 Remove Legacy Files](#31-remove-legacy-files)
   - [3.2 Build and Test](#32-build-and-test)
4. [Reference](#reference)
   - [Package Changes](#package-changes)
   - [Configuration Schema](#configuration-schema)
   - [Build Commands](#build-commands)

---

## Phase 1: Project Setup

### 1.1 Replace Project File

**File:** `src/ServerMaintenance/ServerMaintenance.csproj`

- [x] Create new SDK-style csproj targeting `net8.0`
- [x] Add Microsoft.SqlServer.SqlManagementObjects 171.30.0+
- [x] Add Microsoft.Data.SqlClient 5.2.0+
- [x] Add Microsoft.Extensions.Configuration 8.0.0
- [x] Add Microsoft.Extensions.Configuration.Json 8.0.0
- [x] Remove unnecessary packages (Windows SDK Contracts, WindowsRuntime)
- [x] Configure appsettings.json to copy to output

[↑ Return to Top](#migration-plan-net-framework-472--net-8)

---

### 1.2 Create Configuration File

**File:** `src/ServerMaintenance/appsettings.json`

- [x] Create appsettings.json with SQL backup settings
- [x] Add Notification section (Recipients, FromAddress, Subject)
- [x] Add flexible SMTP section supporting both delivery methods
- [x] Verify JSON structure is valid

[↑ Return to Top](#migration-plan-net-framework-472--net-8)

---

## Phase 2: Code Migration

### 2.1 Update Program.cs

**File:** `src/ServerMaintenance/Program.cs`

- [x] Add configuration builder to load appsettings.json
- [x] Pass IConfiguration to MaintenanceMan constructor
- [x] Remove any .NET Framework-specific code

[↑ Return to Top](#migration-plan-net-framework-472--net-8)

---

### 2.2 Update MaintenanceMan.cs

**File:** `src/ServerMaintenance/MaintenanceMan.cs`

- [x] Add IConfiguration constructor parameter
- [x] Replace ConfigurationManager.AppSettings with IConfiguration
- [x] Update SmtpClient configuration to read from IConfiguration
- [x] Implement dual SMTP mode (SpecifiedPickupDirectory vs Network)
- [x] Replace Newtonsoft.Json with System.Text.Json

[↑ Return to Top](#migration-plan-net-framework-472--net-8)

---

### 2.3 Update SqlScriptRunner.cs

**File:** `src/ServerMaintenance/Helpers/SqlScriptRunner.cs`

- [x] Add IConfiguration constructor parameter
- [x] Replace ConfigurationManager.AppSettings with IConfiguration
- [x] Verify SMO API compatibility with new package version

[↑ Return to Top](#migration-plan-net-framework-472--net-8)

---

### 2.4 Update FolderCleaner.cs

**File:** `src/ServerMaintenance/Helpers/FolderCleaner.cs`

- [x] Add IConfiguration constructor parameter
- [x] Replace ConfigurationManager.AppSettings with IConfiguration

[↑ Return to Top](#migration-plan-net-framework-472--net-8)

---

## Phase 3: Cleanup & Validation

### 3.1 Remove Legacy Files

- [x] ~~Delete~~ Archive `src/ServerMaintenance/App.config` → moved to `src/_archive/`
- [x] ~~Delete~~ Archive `src/ServerMaintenance/Properties/AssemblyInfo.cs` → moved to `src/_archive/`

[↑ Return to Top](#migration-plan-net-framework-472--net-8)

---

### 3.2 Build and Test

- [x] Run `dotnet build` - verify no compilation errors
- [x] Run `dotnet publish -c Release -r win-x64 --self-contained false`
- [ ] Test SQL backup execution locally (if SQL Server available)
- [ ] Test email notification (pickup directory mode)
- [ ] Verify application runs without BatchParser.dll error

[↑ Return to Top](#migration-plan-net-framework-472--net-8)

---

## Reference

### Package Changes

| Old Package | New Package/Version | Action |
|-------------|---------------------|--------|
| Microsoft.SqlServer.SqlManagementObjects 161.x | 171.30.0+ | Upgrade |
| Microsoft.Data.SqlClient 2.1.1 | 5.2.0+ | Upgrade |
| Newtonsoft.Json 13.0.1 | System.Text.Json (built-in) | Replace |
| Microsoft.Identity.Client 4.24.0 | - | Remove |
| Microsoft.Windows.SDK.Contracts | - | Remove |
| System.Runtime.WindowsRuntime.UI.Xaml | - | Remove |
| - | Microsoft.Extensions.Configuration 8.0.0 | Add |
| - | Microsoft.Extensions.Configuration.Json 8.0.0 | Add |

[↑ Return to Top](#migration-plan-net-framework-472--net-8)

---

### Configuration Schema

```json
{
  "SqlScriptPath": "C:\\path\\to\\BackupAllDatabases.sql",
  "SqlBackupPath": "C:\\Temp\\SqlServerBackup\\",
  "DbServerName": ".\\SQLEXPRESS",
  "MaxAgeOfBackupsInDays": 5,
  "Notification": {
    "Recipients": "admin@domain.com",
    "FromAddress": "noreply@domain.com",
    "Subject": "System Maintenance Performed"
  },
  "Smtp": {
    "DeliveryMethod": "SpecifiedPickupDirectory",
    "PickupDirectoryLocation": "c:\\SmtpPickup\\",
    "Host": "",
    "Port": 25,
    "EnableSsl": false,
    "Username": "",
    "Password": ""
  }
}
```

**SMTP DeliveryMethod options:**
- `SpecifiedPickupDirectory` - Drops .eml files to local folder
- `Network` - Sends via SMTP server (requires Host, Port, optionally credentials)

[↑ Return to Top](#migration-plan-net-framework-472--net-8)

---

### Build Commands

```bash
# Build
cd src/ServerMaintenance
dotnet build

# Publish (framework-dependent)
dotnet publish -c Release -r win-x64 --self-contained false

# Output location
src/ServerMaintenance/bin/Release/net8.0/win-x64/publish/
```

**Deployment:** Framework-dependent (requires .NET 8 runtime installed on server)

[↑ Return to Top](#migration-plan-net-framework-472--net-8)
