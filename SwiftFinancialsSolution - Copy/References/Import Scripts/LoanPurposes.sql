
INSERT INTO [SwiftFinancialsDB_DEV].[dbo].[swiftfin_LoanPurposes]( [Id]
      ,[Description]
      ,[IsLocked]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate])
   
SELECT [Id]
      ,[Description]
      ,[IsLocked]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate]
  FROM [SwiftFinancialsDB_TEST].[dbo].[vfin_LoanPurposes]