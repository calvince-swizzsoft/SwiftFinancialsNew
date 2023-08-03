/****** Script for SelectTopNRows command from SSMS  ******/

INSERT INTO [SwiftFinancialsDB_DEV].[dbo].[swiftfin_Budgets]
SELECT TOP (1000) [Id]
      ,[PostingPeriodId]
      ,[BranchId]
      ,[Description]
      ,[TotalValue]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate]
  FROM [SwiftFinancialsDB_Test].[dbo].[vfin_Budgets]