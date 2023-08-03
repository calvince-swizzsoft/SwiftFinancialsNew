

INSERT INTO  [SwiftFinancialsDB_DEV].[dbo].[swiftfin_JournalVoucherEntries]([Id]
      ,[JournalVoucherId]
      ,[ChartOfAccountId]
      ,[CustomerAccountId]
      ,[Amount]
      ,[Status]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate])

SELECT [Id]
      ,[JournalVoucherId]
      ,[ChartOfAccountId]
      ,[CustomerAccountId]
      ,[Amount]
      ,[Status]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate]
  FROM [SwiftFinancialsDB_TEST].[dbo].[vfin_JournalVoucherEntries]
 