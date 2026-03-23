# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Server Maintenance is a C# console application (.NET Framework 4.7.2) that automates SQL Server database backups for SQL Server Express instances (which lack built-in maintenance plans). It executes backup scripts, enforces retention policies, and sends email notifications.

## Build Commands

```bash
# Build from command line (requires MSBuild/Visual Studio)
msbuild src/ServerMaintenance.sln /p:Configuration=Release /p:Platform="Any CPU"

# Output executable
src/ServerMaintenance/bin/Release/ServerMaintenance.exe
```

No test framework is present in this project.

## Architecture

**Execution Flow:**
```
Program.Main() → MaintenanceMan.RunAll()
                    ├─> SqlScriptRunner.Run() - Executes BackupAllDatabases.sql via SMO
                    ├─> FolderCleaner.Run() - Deletes backups older than retention period
                    └─> Sends email notification with activity log
```

**Key Classes:**
- `MaintenanceMan` - Main orchestrator that coordinates backup, cleanup, and notification
- `Helpers/SqlScriptRunner` - Executes SQL scripts via Microsoft.SqlServer.Management.Smo, replaces `[BACKUP_PATH]` placeholder
- `Helpers/FolderCleaner` - Enforces retention policy (deletes files older than MaxAgeOfBackupsInDays)
- `Helpers/ActivityLog` - Maintains log entries with timestamps and error flags

**Configuration:** All settings are in `src/ServerMaintenance/App.config` including SQL paths, server name, retention days, and SMTP settings.

## Deployment

Runs as a Windows Scheduled Task under a service account with SQL Server access and file system write permissions to the backup folder.

## Conventions

- ZOMBIE-prefixed commented code blocks are intentional. Don't delete them — they may be re-implemented later.
