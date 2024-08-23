 INSERT INTO [SwiftFinancialsDB_DEV].[dbo].[swiftfin_ChequeTypes]([Id]
   ,[Description]
      ,[MaturityPeriod]
      ,[ChargeRecoveryMode]
      ,[IsLocked]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate])
SELECT TOP (1000) [Id]
      ,[Description]
      ,[MaturityPeriod]
      ,[ChargeRecoveryMode]
      ,[IsLocked]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate]
  FROM [SwiftFinancialsDB_TEST].[dbo].[vfin_ChequeTypes]

 
