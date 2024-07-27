INSERT INTO [SwiftFinancialsDB_DEV].[dbo].[swiftfin_CostCenters]([Id]
,[Description]
      ,[IsLocked]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate])


SELECT TOP (1000) [Id]
      ,[Description]
      ,[IsLocked]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate]
  FROM [SwiftFinancialsDB_Test].[dbo].[vfin_CostCenters]
