/****** Script for SelectTopNRows command from SSMS  ******/
INSERT INTO  [SwiftFinancialsDB_DEV].[dbo].[swiftfin_Banks]([Id]
      ,[Code]
      ,[Description]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate])

SELECT TOP (1000) [Id]
      ,[Code]
      ,[Description]
      ,[SequentialId]
      ,[CreatedBy]
      ,[CreatedDate]
  FROM [SwiftFinancialsDB_TEST].[dbo].[vfin_Banks]