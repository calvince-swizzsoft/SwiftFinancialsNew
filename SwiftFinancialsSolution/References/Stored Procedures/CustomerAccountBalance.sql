Alter PROCEDURE [dbo].[sp_CustomerAccountBalance] 
	-- Add the parameters for the stored procedure here
	@CustomerAccountID Varchar(100) = 0, 
	@Type int = 0,
	@considerMaturityPeriodForInvestmentAccounts bit =0,
	@CutoffDate DateTime,
	@CustomerAccountType_TargetProductId Uniqueidentifier=null,
	@CustomerAccountType_ProductCode Int=null
AS
	Declare @Balance numeric(18,2)
	Declare @MinBalance numeric(18,2), @CustomerAccountID_Sniff uniqueidentifier=@CustomerAccountID,@CutoffDate_Sniff DateTime=@CutoffDate

BEGIN

	IF left(CAST(@CutoffDate_Sniff AS TIME),5) = '00:00'
	BEGIN
		SET @CutoffDate_Sniff = (SELECT DATEADD(day, DATEDIFF(day, 0, @CutoffDate_Sniff), '23:59:59'));
	END
	
	SET NOCOUNT ON;
	
	Declare @ProductID uniqueidentifier=@CustomerAccountType_TargetProductId,
			@Days int,
			@EndDate Datetime,
			@unMaturedAmount Numeric(18,2),
			@UnclearedCheques Numeric(18,2),
			@UnclearedChequesChartOfAccountID uniqueidentifier,
			@InterestReceivableChartOfAccountID Uniqueidentifier,
			@ChartOfAccountID UniqueIdentifier
			if @type=2
				begin
					set @InterestReceivableChartOfAccountID=(SELECT InterestReceivableChartOfAccountId FROM SwiftFin_LoanProducts WHERE ID =@ProductID)
				end
			if @type=3
				begin
					set @InterestReceivableChartOfAccountID=(SELECT ChartOfAccountId FROM SwiftFin_SystemGeneralLedgerAccountMappings WHERE SystemGeneralLedgerAccountCode =48832)
				end
	set @ChartOfAccountID= (
		SELECT case 
		when @CustomerAccountType_ProductCode=1 then 
		(select       ChartOfAccountId FROM SwiftFin_SavingsProducts  WHERE        (id =@ProductID))
		when @CustomerAccountType_ProductCode=2 then 
		(select       ChartOfAccountId FROM SwiftFin_LoanProducts  WHERE        (id =@ProductID))
		when @CustomerAccountType_ProductCode=3 then 
		(select       ChartOfAccountId FROM SwiftFin_InvestmentProducts  WHERE        (id =@ProductID))
		end)
	set @UnclearedChequesChartOfAccountID=
						(
							  SELECT[ChartOfAccountId]
							  FROM [dbo].[SwiftFin_SystemGeneralLedgerAccountMappings] 
							  WHERE SystemGeneralLedgerAccountCode=48827
						)
	set @days=iif(@considerMaturityPeriodForInvestmentAccounts=1,(select MaturityPeriod from SwiftFin_InvestmentProducts where id=@ProductID),0)
	set @unMaturedAmount=0
	set @Balance = 
		CASE 
			WHEN @Type=1 THEN ---Account Balance
				(
					SELECT        SUM(dbo.SwiftFin_JournalEntries.Amount) AS Expr1
					FROM            dbo.SwiftFin_JournalEntries  
					WHERE        (dbo.SwiftFin_JournalEntries.CustomerAccountId = @CustomerAccountID_Sniff) 
					AND (dbo.SwiftFin_JournalEntries.ChartOfAccountId in(@ChartOfAccountID,@UnclearedChequesChartOfAccountID))
					and CreatedDate<@CutoffDate_Sniff
				)
			when @type=2 then
			(		SELECT SUM(dbo.SwiftFin_JournalEntries.Amount) 
						FROM   dbo.SwiftFin_JournalEntries 
						WHERE CustomerAccountId =@CustomerAccountID_Sniff 
						 and ChartOfAccountId =@InterestReceivableChartOfAccountID
						 and CreatedDate<@CutoffDate_Sniff
				)
				when @Type=3 then
				(		SELECT SUM(dbo.SwiftFin_JournalEntries.Amount) 
						FROM   dbo.SwiftFin_JournalEntries 
						WHERE CustomerAccountId =@CustomerAccountID_Sniff 
						 and ChartOfAccountId =@InterestReceivableChartOfAccountID
						 and CreatedDate<@CutoffDate_Sniff
				)
			END 
		select @Balance= 
			CASE 
			  WHEN @considerMaturityPeriodForInvestmentAccounts=1 THEN  
					@Balance-@unMaturedAmount
			  ELSE
				    @Balance
			  END 
	SELECT ISNULL(@Balance,0) as Balance
END