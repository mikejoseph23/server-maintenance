DECLARE @name VARCHAR(50) -- database name
DECLARE @path VARCHAR(256) -- path for backup files
DECLARE @fileName VARCHAR(256) -- filename for backup
DECLARE @fileDate VARCHAR(20) -- used for file name

-- Express Edition (EngineEdition 4) rejects BACKUP ... WITH COMPRESSION with
-- error 1844, and that aborts the whole cursor. Decide per server rather than
-- per copy of this file - iadev-prod runs Express and kept a hand-edited script
-- with the clause stripped out, which the next deploy then overwrote.
DECLARE @compress BIT = CASE WHEN SERVERPROPERTY('EngineEdition') = 4 THEN 0 ELSE 1 END

-- specify database backup directory
SET @path = '[BACKUP_PATH]'

-- specify filename format
-- SELECT @fileDate = CONVERT(VARCHAR(20),GETDATE(),112)

-- Include the time
SELECT @fileDate = CONVERT(VARCHAR(20),GETDATE(),112) + REPLACE(CONVERT(VARCHAR(20),GETDATE(),108),':','')

-- System databases, plus whatever the ExcludedDatabases setting names.
DECLARE db_cursor CURSOR FOR
SELECT name
FROM master.dbo.sysdatabases
WHERE name NOT IN ([EXCLUDED_DATABASES])

OPEN db_cursor
FETCH NEXT FROM db_cursor INTO @name

WHILE @@FETCH_STATUS = 0
BEGIN
       SET @fileName = @path + @name + '_' + @fileDate + '.BAK'

       IF @compress = 1
              BACKUP DATABASE @name TO DISK = @fileName WITH COMPRESSION
       ELSE
              BACKUP DATABASE @name TO DISK = @fileName

       FETCH NEXT FROM db_cursor INTO @name
END

CLOSE db_cursor
DEALLOCATE db_cursor
