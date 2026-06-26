USE [SwiftFinancialsDB_Live]
GO

/****** Object:  StoredProcedure [dbo].[sp_ActiveAccounts]    Script Date: 28/08/2024 18:28:04 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author, Joan>
-- Create date: <Create Date, 16/09/2015>
-- Description:	<Description, Active Accounts>
-- =============================================
--sp_ActiveAccounts '1', '08/15/2018'
CREATE PROCEDURE [dbo].[sp_ActiveAccounts]
	-- Add the parameters for the stored procedure here
	@Month int , 
	@CreatedDate DateTime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	set @CreatedDate=	(select DATEADD(day, DATEDIFF(day, 0, @CreatedDate), '23:59:59'));
	WITH ActiveAccountsCTE (LastTrxDATE, FosaAccount,FullName,PayrollNo,MobileLine,IdentityCardNumber,Email)
	AS(
    -- Insert statements for procedure here
	SELECT        (MAX( dbo.SwiftFin_JournalEntries.CreatedDate)) As LastTrxDate,dbo.vw_CustomerAccounts.Reference1, dbo.vw_CustomerAccounts.FullName ,dbo.vw_CustomerAccounts.Reference3,
	dbo.vw_CustomerAccounts.Address_MobileLine,dbo.vw_CustomerAccounts.Individual_IdentityCardNumber,dbo.vw_CustomerAccounts.Address_Email
	FROM          dbo.vw_CustomerAccounts INNER JOIN
				  dbo.SwiftFin_JournalEntries ON dbo.vw_CustomerAccounts.Id = dbo.SwiftFin_JournalEntries.CustomerAccountId INNER JOIN
                  dbo.SwiftFin_SavingsProducts ON dbo.SwiftFin_JournalEntries.ChartOfAccountId = dbo.SwiftFin_SavingsProducts.ChartOfAccountId
	Where		  dbo.SwiftFin_JournalEntries.CreatedDate<=@CreatedDate
	Group by     dbo.vw_CustomerAccounts.Address_Email, dbo.vw_CustomerAccounts.Reference1, dbo.vw_CustomerAccounts.FullName,dbo.vw_CustomerAccounts.Reference3,
	dbo.vw_CustomerAccounts.Address_MobileLine,dbo.vw_CustomerAccounts.Individual_IdentityCardNumber)

	
	SELECT DATEDIFF(MM,LastTrxDate,@CreatedDate) AS Month,* 
	FROM ActiveAccountsCTE WHERE DATEDIFF(DD,LastTrxDATE,@CreatedDate)<=@Month*30
END






GO


