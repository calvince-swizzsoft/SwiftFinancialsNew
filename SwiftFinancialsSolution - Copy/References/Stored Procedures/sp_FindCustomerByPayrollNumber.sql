
CREATE PROCEDURE [dbo].[sp_FindCustomerByPayrollNumber]
	-- Add the parameters for the stored procedure here
	@pIndividual_PayrollNumber varchar(100) = ''
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	SELECT * FROM swiftfin_Customers WHERE Individual_PayrollNumbers Like('%' + @pIndividual_PayrollNumber + '%')

END
