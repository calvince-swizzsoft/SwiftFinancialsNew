

INSERT INTO [SwiftFinancialsDB_DEV].[dbo].[swiftfin_CreditTypes] ([Id]
      ,[ChartOfAccountId]
      ,[Description]
      ,[TransactionOwnership]
      ,[IsLocked]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate])
  
SELECT  [Id]
      ,[ChartOfAccountId]
      ,[Description]
      ,[TransactionOwnership]
      ,[IsLocked]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate]
  FROM [SwiftFinancialsDB_TEST].[dbo].[vfin_CreditTypes]

  WHERE Id NOT IN (SELECT Id
  FROM
  [SwiftFinancialsDB_DEV].[dbo].[swiftfin_CreditTypes])