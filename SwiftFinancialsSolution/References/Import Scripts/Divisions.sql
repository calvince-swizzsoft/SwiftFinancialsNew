
INSERT INTO [SwiftFinancialsDB_DEV].[dbo].[swiftfin_Divisions]([Id]
	  ,[EmployerId]
      ,[Description]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate])
SELECT TOP (1000) [Id]
      ,[EmployerId]
      ,[Description]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate]
  FROM [SwiftFinancialsDB_TEST].[dbo].[vfin_Divisions]
