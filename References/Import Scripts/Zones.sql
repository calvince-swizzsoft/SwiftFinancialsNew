
INSERT INTO [SwiftFinancialsDB_DEV].[dbo].[swiftfin_Zones]([Id]
	  ,[DivisionId]
      ,[Description]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate])
SELECT TOP (1000) [Id]
      ,[DivisionId]
      ,[Description]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate]
  FROM [SwiftFinancialsDB_TEST].[dbo].[vfin_Zones]
