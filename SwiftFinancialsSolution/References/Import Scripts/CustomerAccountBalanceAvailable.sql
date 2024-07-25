CREATE PROCEDURE [dbo].[sp_CustomerAccountBalanceAvailable] 
	-- Add the parameters for the stored procedure here
	@CustomerAccountID UniqueIdentifier,
	@CutoffDate Datetime ,
	@CustomerAccountType_TargetProductId Uniqueidentifier,
	@CustomerAccountType_ProductCode Int,
	@CustomerAccountBranchId Uniqueidentifier
AS
	Declare @Balance numeric(18,2)
	Declare @MinBalance numeric(18,2),
	@SavingsChartOfAccountID UniqueIdentifier,
	@ProducID UniqueIdentifier=@CustomerAccountType_TargetProductId,
	@BranchId UniqueIdentifier=@CustomerAccountBranchId

BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SET @SavingsChartOfAccountID=(SELECT TOP 1 ChartOfAccountId FROM SwiftFin_SavingsProducts where id=@ProducID)
	SET @CutoffDate=	(select DATEADD(day, DATEDIFF(day, 0, @CutoffDate), '23:59:59'));
	SELECT @Balance = 

				isnull((
						Select SUM(dbo.SwiftFin_JournalEntries.Amount) ---Products_1.MinBalance
						FROM   dbo.SwiftFin_JournalEntries WITH (NOLOCK) 
						WHERE CustomerAccountId =@CustomerAccountId and ChartOfAccountId =@SavingsChartOfAccountID
						 and CreatedDate<=@CutoffDate
						
				),0)

	IF EXISTS (select 1 from SwiftFin_SavingsProductExemptions where SavingsProductId=@ProducID and BranchId=@BranchId) 
	BEGIN
		SET @MinBalance =  (SELECT TOP 1 MinimumBalance from SwiftFin_SavingsProductExemptions where SavingsProductId=@ProducID and BranchId=@BranchId)
	END
	ELSE
	BEGIN
		SET @MinBalance = (select MinimumBalance from SwiftFin_SavingsProducts where Id=@ProducID)
	END

	SET @MinBalance = ISNULL(@MinBalance,0)

	SET @Balance=@Balance-@MinBalance
	
	SELECT ISNULL(@Balance,0) as Balance
END