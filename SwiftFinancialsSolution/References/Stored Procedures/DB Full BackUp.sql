DECLARE @FileName NVARCHAR(500)
SET @FileName = 'D:\DBs\SwiftFinancialsDB_Test_' + 
    CONVERT(VARCHAR(20), GETDATE(), 112) + '_' + 
    REPLACE(CONVERT(VARCHAR(20), GETDATE(), 108), ':', '') + '.bak'

BACKUP DATABASE [SwiftFinancialsDB_Test]
TO DISK = @FileName
WITH FORMAT,
     MEDIANAME = 'SQLServerBackups',
     NAME = 'Full Backup of YourDatabaseName';