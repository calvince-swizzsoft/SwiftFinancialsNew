select LoanRegistration_TermInMonths,LoanRegistration_PaymentFrequencyPerYear,LoanRegistration_GracePeriod,
LoanInterest_CalculationMode,LoanInterest_AnnualPercentageRate, DisbursedAmount,
0, LoanRegistration_PaymentDueDate from vfin_LoanCases where Id='51241FA2-514F-EF11-890E-64BC58A0AB3F'





exec [dbo].[RepaymentSchedule] 10,12,0,515,22.8,4000,0,0





SELECT * FROM [dbo].[RepaymentSchedule](10, 12, 0, 515, 22.8, 4000, 0, 0);





EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;




ALTER DATABASE SwiftFinancialsDB_Test SET TRUSTWORTHY ON;





-- Enable CLR integration
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;