using Application.MainBoundedContext.DTO.AdministrationModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TestApis.Services
{
    public class CompanyService
    {
        private readonly string _connectionString;

        public CompanyService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
        }

        public IEnumerable<CompanyDTO> GetAll()
        {
            var list = new List<CompanyDTO>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT TOP 1000 * FROM [swiftFin_Companies] ORDER BY Description";
                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            list.Add(Map(reader));
                }
            }
            return list;
        }

        public CompanyDTO GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM [swiftFin_Companies] WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Map(reader);
                    }
                }
            }
            return null;
        }

        public CompanyDTO GetByRegistrationNumber(string registrationNumber)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM [swiftFin_Companies] WHERE RegistrationNumber = @RegistrationNumber";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@RegistrationNumber", registrationNumber);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Map(reader);
                    }
                }
            }
            return null;
        }

        public CompanyDTO Create(CompanyDTO company)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                // Generate new Guid if not provided
                if (company.Id == Guid.Empty)
                    company.Id = Guid.NewGuid();

                company.CreatedDate = DateTime.Now;

                string query = @"INSERT INTO [swiftFin_Companies] 
                                ([Id], [Description], [Vision], [Mission], [Motto], [RegistrationNumber], 
                                 [PersonalIdentificationNumber], [ApplicationDisplayName], [RecoveryPriority],
                                 [Address_AddressLine1], [Address_AddressLine2], [Address_Street], [Address_PostalCode],
                                 [Address_City], [Address_Email], [Address_LandLine], [Address_MobileLine],
                                 [TransactionReceiptTopIndentation], [TransactionReceiptLeftIndentation],
                                 [TransactionReceiptFooter], [FingerprintBiometricThreshold],
                                 [MembershipTerminationNoticePeriod], [TimeDuration_StartTime], [TimeDuration_EndTime],
                                 [ApplicationMembershipTextAlertsEnabled], [EnforceCustomerAccountMakerChecker],
                                 [BypassJournalVoucherAudit], [BypassCreditBatchAudit], [BypassDebitBatchAudit],
                                 [BypassRefundBatchAudit], [BypassWireTransferBatchAudit], [BypassLoanDisbursementBatchAudit],
                                 [BypassJournalReversalBatchAudit], [BypassInterAccountTransferBatchAudit],
                                 [BypassExpensePayableAudit], [BypassGeneralLedgerAudit],
                                 [ExcludeChargesInTransactionReceipt], [ExcludeChequeMaturityDateInTransactionReceipt],
                                 [TrackGuarantorCommittedInvestments], [TransferNetRefundableAmountToSavingsAccountOnDeathClaimSettlement],
                                 [ReceiveLoanRequestBeforeLoanRegistration], [LocalizeOnlineNotifications],
                                 [IsWithholdingTaxAgent], [EnforceBudgetControl], [IsFileTrackingEnforced],
                                 [ExcludeCustomerAccountBalanceInTransactionReceipt], [EnforceFixedDepositBands],
                                 [EnforceBiometricsForCashWithdrawal], [EnforceTwoFactorAuthentication],
                                 [RecoverArrearsOnCashDeposit], [RecoverArrearsOnExternalChequeClearance],
                                 [RecoverArrearsOnFixedDepositPayment], [AllowDebitBatchToOverdrawAccount],
                                 [EnforceSystemLock], [EnforceTellerLimits], [EnforceTellerCashTransferAcknowledgement],
                                 [EnforceSingleUserSession], [CustomerMembershipTextAlertsEnabled],
                                 [EnforceInvestmentProductExemptions], [EnforceMobileToBankReconciliationVerification],
                                 [IsLocked], [CreatedBy], [CreatedDate])
                                VALUES 
                                (@Id, @Description, @Vision, @Mission, @Motto, @RegistrationNumber, 
                                 @PersonalIdentificationNumber, @ApplicationDisplayName, @RecoveryPriority,
                                 @Address_AddressLine1, @Address_AddressLine2, @Address_Street, @Address_PostalCode,
                                 @Address_City, @Address_Email, @Address_LandLine, @Address_MobileLine,
                                 @TransactionReceiptTopIndentation, @TransactionReceiptLeftIndentation,
                                 @TransactionReceiptFooter, @FingerprintBiometricThreshold,
                                 @MembershipTerminationNoticePeriod, @TimeDuration_StartTime, @TimeDuration_EndTime,
                                 @ApplicationMembershipTextAlertsEnabled, @EnforceCustomerAccountMakerChecker,
                                 @BypassJournalVoucherAudit, @BypassCreditBatchAudit, @BypassDebitBatchAudit,
                                 @BypassRefundBatchAudit, @BypassWireTransferBatchAudit, @BypassLoanDisbursementBatchAudit,
                                 @BypassJournalReversalBatchAudit, @BypassInterAccountTransferBatchAudit,
                                 @BypassExpensePayableAudit, @BypassGeneralLedgerAudit,
                                 @ExcludeChargesInTransactionReceipt, @ExcludeChequeMaturityDateInTransactionReceipt,
                                 @TrackGuarantorCommittedInvestments, @TransferNetRefundableAmountToSavingsAccountOnDeathClaimSettlement,
                                 @ReceiveLoanRequestBeforeLoanRegistration, @LocalizeOnlineNotifications,
                                 @IsWithholdingTaxAgent, @EnforceBudgetControl, @IsFileTrackingEnforced,
                                 @ExcludeCustomerAccountBalanceInTransactionReceipt, @EnforceFixedDepositBands,
                                 @EnforceBiometricsForCashWithdrawal, @EnforceTwoFactorAuthentication,
                                 @RecoverArrearsOnCashDeposit, @RecoverArrearsOnExternalChequeClearance,
                                 @RecoverArrearsOnFixedDepositPayment, @AllowDebitBatchToOverdrawAccount,
                                 @EnforceSystemLock, @EnforceTellerLimits, @EnforceTellerCashTransferAcknowledgement,
                                 @EnforceSingleUserSession, @CustomerMembershipTextAlertsEnabled,
                                 @EnforceInvestmentProductExemptions, @EnforceMobileToBankReconciliationVerification,
                                 @IsLocked, @CreatedBy, @CreatedDate)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, company);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return company;
        }

        public void Update(CompanyDTO company)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"UPDATE [swiftFin_Companies] 
                                SET [Description] = @Description,
                                    [Vision] = @Vision,
                                    [Mission] = @Mission,
                                    [Motto] = @Motto,
                                    [RegistrationNumber] = @RegistrationNumber,
                                    [PersonalIdentificationNumber] = @PersonalIdentificationNumber,
                                    [ApplicationDisplayName] = @ApplicationDisplayName,
                                    [RecoveryPriority] = @RecoveryPriority,
                                    [Address_AddressLine1] = @Address_AddressLine1,
                                    [Address_AddressLine2] = @Address_AddressLine2,
                                    [Address_Street] = @Address_Street,
                                    [Address_PostalCode] = @Address_PostalCode,
                                    [Address_City] = @Address_City,
                                    [Address_Email] = @Address_Email,
                                    [Address_LandLine] = @Address_LandLine,
                                    [Address_MobileLine] = @Address_MobileLine,
                                    [TransactionReceiptTopIndentation] = @TransactionReceiptTopIndentation,
                                    [TransactionReceiptLeftIndentation] = @TransactionReceiptLeftIndentation,
                                    [TransactionReceiptFooter] = @TransactionReceiptFooter,
                                    [FingerprintBiometricThreshold] = @FingerprintBiometricThreshold,
                                    [MembershipTerminationNoticePeriod] = @MembershipTerminationNoticePeriod,
                                    [TimeDuration_StartTime] = @TimeDuration_StartTime,
                                    [TimeDuration_EndTime] = @TimeDuration_EndTime,
                                    [ApplicationMembershipTextAlertsEnabled] = @ApplicationMembershipTextAlertsEnabled,
                                    [EnforceCustomerAccountMakerChecker] = @EnforceCustomerAccountMakerChecker,
                                    [BypassJournalVoucherAudit] = @BypassJournalVoucherAudit,
                                    [BypassCreditBatchAudit] = @BypassCreditBatchAudit,
                                    [BypassDebitBatchAudit] = @BypassDebitBatchAudit,
                                    [BypassRefundBatchAudit] = @BypassRefundBatchAudit,
                                    [BypassWireTransferBatchAudit] = @BypassWireTransferBatchAudit,
                                    [BypassLoanDisbursementBatchAudit] = @BypassLoanDisbursementBatchAudit,
                                    [BypassJournalReversalBatchAudit] = @BypassJournalReversalBatchAudit,
                                    [BypassInterAccountTransferBatchAudit] = @BypassInterAccountTransferBatchAudit,
                                    [BypassExpensePayableAudit] = @BypassExpensePayableAudit,
                                    [BypassGeneralLedgerAudit] = @BypassGeneralLedgerAudit,
                                    [ExcludeChargesInTransactionReceipt] = @ExcludeChargesInTransactionReceipt,
                                    [ExcludeChequeMaturityDateInTransactionReceipt] = @ExcludeChequeMaturityDateInTransactionReceipt,
                                    [TrackGuarantorCommittedInvestments] = @TrackGuarantorCommittedInvestments,
                                    [TransferNetRefundableAmountToSavingsAccountOnDeathClaimSettlement] = @TransferNetRefundableAmountToSavingsAccountOnDeathClaimSettlement,
                                    [ReceiveLoanRequestBeforeLoanRegistration] = @ReceiveLoanRequestBeforeLoanRegistration,
                                    [LocalizeOnlineNotifications] = @LocalizeOnlineNotifications,
                                    [IsWithholdingTaxAgent] = @IsWithholdingTaxAgent,
                                    [EnforceBudgetControl] = @EnforceBudgetControl,
                                    [IsFileTrackingEnforced] = @IsFileTrackingEnforced,
                                    [ExcludeCustomerAccountBalanceInTransactionReceipt] = @ExcludeCustomerAccountBalanceInTransactionReceipt,
                                    [EnforceFixedDepositBands] = @EnforceFixedDepositBands,
                                    [EnforceBiometricsForCashWithdrawal] = @EnforceBiometricsForCashWithdrawal,
                                    [EnforceTwoFactorAuthentication] = @EnforceTwoFactorAuthentication,
                                    [RecoverArrearsOnCashDeposit] = @RecoverArrearsOnCashDeposit,
                                    [RecoverArrearsOnExternalChequeClearance] = @RecoverArrearsOnExternalChequeClearance,
                                    [RecoverArrearsOnFixedDepositPayment] = @RecoverArrearsOnFixedDepositPayment,
                                    [AllowDebitBatchToOverdrawAccount] = @AllowDebitBatchToOverdrawAccount,
                                    [EnforceSystemLock] = @EnforceSystemLock,
                                    [EnforceTellerLimits] = @EnforceTellerLimits,
                                    [EnforceTellerCashTransferAcknowledgement] = @EnforceTellerCashTransferAcknowledgement,
                                    [EnforceSingleUserSession] = @EnforceSingleUserSession,
                                    [CustomerMembershipTextAlertsEnabled] = @CustomerMembershipTextAlertsEnabled,
                                    [EnforceInvestmentProductExemptions] = @EnforceInvestmentProductExemptions,
                                    [EnforceMobileToBankReconciliationVerification] = @EnforceMobileToBankReconciliationVerification,
                                    [IsLocked] = @IsLocked,
                                    [CreatedBy] = @CreatedBy
                                WHERE [Id] = @Id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParams(cmd, company);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [swiftFin_Companies] WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void AddParams(SqlCommand cmd, CompanyDTO company)
        {
            cmd.Parameters.AddWithValue("@Id", company.Id);
            cmd.Parameters.AddWithValue("@Description", company.Description ?? "");
            cmd.Parameters.AddWithValue("@Vision", company.Vision ?? "");
            cmd.Parameters.AddWithValue("@Mission", company.Mission ?? "");
            cmd.Parameters.AddWithValue("@Motto", company.Motto ?? "");
            cmd.Parameters.AddWithValue("@RegistrationNumber", company.RegistrationNumber ?? "");
            cmd.Parameters.AddWithValue("@PersonalIdentificationNumber", company.PersonalIdentificationNumber ?? "");
            cmd.Parameters.AddWithValue("@ApplicationDisplayName", company.ApplicationDisplayName ?? "");
            cmd.Parameters.AddWithValue("@RecoveryPriority", company.RecoveryPriority ?? "");
            cmd.Parameters.AddWithValue("@Address_AddressLine1", company.AddressAddressLine1 ?? "");
            cmd.Parameters.AddWithValue("@Address_AddressLine2", company.AddressAddressLine2 ?? "");
            cmd.Parameters.AddWithValue("@Address_Street", company.AddressStreet ?? "");
            cmd.Parameters.AddWithValue("@Address_PostalCode", company.AddressPostalCode ?? "");
            cmd.Parameters.AddWithValue("@Address_City", company.AddressCity ?? "");
            cmd.Parameters.AddWithValue("@Address_Email", company.AddressEmail ?? "");
            cmd.Parameters.AddWithValue("@Address_LandLine", company.AddressLandLine ?? "");
            cmd.Parameters.AddWithValue("@Address_MobileLine", company.AddressMobileLine ?? "");
            cmd.Parameters.AddWithValue("@TransactionReceiptTopIndentation", company.TransactionReceiptTopIndentation);
            cmd.Parameters.AddWithValue("@TransactionReceiptLeftIndentation", company.TransactionReceiptLeftIndentation);
            cmd.Parameters.AddWithValue("@TransactionReceiptFooter", company.TransactionReceiptFooter ?? "");
            cmd.Parameters.AddWithValue("@FingerprintBiometricThreshold", company.FingerprintBiometricThreshold);
            cmd.Parameters.AddWithValue("@MembershipTerminationNoticePeriod", company.MembershipTerminationNoticePeriod);
            cmd.Parameters.AddWithValue("@TimeDuration_StartTime", company.TimeDurationStartTime);
            cmd.Parameters.AddWithValue("@TimeDuration_EndTime", company.TimeDurationEndTime);
            cmd.Parameters.AddWithValue("@ApplicationMembershipTextAlertsEnabled", company.ApplicationMembershipTextAlertsEnabled);
            cmd.Parameters.AddWithValue("@EnforceCustomerAccountMakerChecker", company.EnforceCustomerAccountMakerChecker);
            cmd.Parameters.AddWithValue("@BypassJournalVoucherAudit", company.BypassJournalVoucherAudit);
            cmd.Parameters.AddWithValue("@BypassCreditBatchAudit", company.BypassCreditBatchAudit);
            cmd.Parameters.AddWithValue("@BypassDebitBatchAudit", company.BypassDebitBatchAudit);
            cmd.Parameters.AddWithValue("@BypassRefundBatchAudit", company.BypassRefundBatchAudit);
            cmd.Parameters.AddWithValue("@BypassWireTransferBatchAudit", company.BypassWireTransferBatchAudit);
            cmd.Parameters.AddWithValue("@BypassLoanDisbursementBatchAudit", company.BypassLoanDisbursementBatchAudit);
            cmd.Parameters.AddWithValue("@BypassJournalReversalBatchAudit", company.BypassJournalReversalBatchAudit);
            cmd.Parameters.AddWithValue("@BypassInterAccountTransferBatchAudit", company.BypassInterAccountTransferBatchAudit);
            cmd.Parameters.AddWithValue("@BypassExpensePayableAudit", company.BypassExpensePayableAudit);
            cmd.Parameters.AddWithValue("@BypassGeneralLedgerAudit", company.BypassGeneralLedgerAudit);
            cmd.Parameters.AddWithValue("@ExcludeChargesInTransactionReceipt", company.ExcludeChargesInTransactionReceipt);
            cmd.Parameters.AddWithValue("@ExcludeChequeMaturityDateInTransactionReceipt", company.ExcludeChequeMaturityDateInTransactionReceipt);
            cmd.Parameters.AddWithValue("@TrackGuarantorCommittedInvestments", company.TrackGuarantorCommittedInvestments);
            cmd.Parameters.AddWithValue("@TransferNetRefundableAmountToSavingsAccountOnDeathClaimSettlement", company.TransferNetRefundableAmountToSavingsAccountOnDeathClaimSettlement);
            cmd.Parameters.AddWithValue("@ReceiveLoanRequestBeforeLoanRegistration", company.ReceiveLoanRequestBeforeLoanRegistration);
            cmd.Parameters.AddWithValue("@LocalizeOnlineNotifications", company.LocalizeOnlineNotifications);
            cmd.Parameters.AddWithValue("@IsWithholdingTaxAgent", company.IsWithholdingTaxAgent);
            cmd.Parameters.AddWithValue("@EnforceBudgetControl", company.EnforceBudgetControl);
            cmd.Parameters.AddWithValue("@IsFileTrackingEnforced", company.IsFileTrackingEnforced);
            cmd.Parameters.AddWithValue("@ExcludeCustomerAccountBalanceInTransactionReceipt", company.ExcludeCustomerAccountBalanceInTransactionReceipt);
            cmd.Parameters.AddWithValue("@EnforceFixedDepositBands", company.EnforceFixedDepositBands);
            cmd.Parameters.AddWithValue("@EnforceBiometricsForCashWithdrawal", company.EnforceBiometricsForCashWithdrawal);
            cmd.Parameters.AddWithValue("@EnforceTwoFactorAuthentication", company.EnforceTwoFactorAuthentication);
            cmd.Parameters.AddWithValue("@RecoverArrearsOnCashDeposit", company.RecoverArrearsOnCashDeposit);
            cmd.Parameters.AddWithValue("@RecoverArrearsOnExternalChequeClearance", company.RecoverArrearsOnExternalChequeClearance);
            cmd.Parameters.AddWithValue("@RecoverArrearsOnFixedDepositPayment", company.RecoverArrearsOnFixedDepositPayment);
            cmd.Parameters.AddWithValue("@AllowDebitBatchToOverdrawAccount", company.AllowDebitBatchToOverdrawAccount);
            cmd.Parameters.AddWithValue("@EnforceSystemLock", company.EnforceSystemLock);
            cmd.Parameters.AddWithValue("@EnforceTellerLimits", company.EnforceTellerLimits);
            cmd.Parameters.AddWithValue("@EnforceTellerCashTransferAcknowledgement", company.EnforceTellerCashTransferAcknowledgement);
            cmd.Parameters.AddWithValue("@EnforceSingleUserSession", company.EnforceSingleUserSession);
            cmd.Parameters.AddWithValue("@CustomerMembershipTextAlertsEnabled", company.CustomerMembershipTextAlertsEnabled);
            cmd.Parameters.AddWithValue("@EnforceInvestmentProductExemptions", company.EnforceInvestmentProductExemptions);
            cmd.Parameters.AddWithValue("@EnforceMobileToBankReconciliationVerification", company.EnforceMobileToBankReconciliationVerification);
            cmd.Parameters.AddWithValue("@IsLocked", company.IsLocked);
            cmd.Parameters.AddWithValue("@CreatedBy", company.CreatedBy ?? "");
            cmd.Parameters.AddWithValue("@CreatedDate", company.CreatedDate);
        }

        private CompanyDTO Map(IDataReader reader)
        {
            return new CompanyDTO
            {
                Id = (Guid)reader["Id"],
                Description = reader["Description"]?.ToString(),
                Vision = reader["Vision"]?.ToString(),
                Mission = reader["Mission"]?.ToString(),
                Motto = reader["Motto"]?.ToString(),
                RegistrationNumber = reader["RegistrationNumber"]?.ToString(),
                PersonalIdentificationNumber = reader["PersonalIdentificationNumber"]?.ToString(),
                ApplicationDisplayName = reader["ApplicationDisplayName"]?.ToString(),
                RecoveryPriority = reader["RecoveryPriority"]?.ToString(),
                AddressAddressLine1 = reader["Address_AddressLine1"]?.ToString(),
                AddressAddressLine2 = reader["Address_AddressLine2"]?.ToString(),
                AddressStreet = reader["Address_Street"]?.ToString(),
                AddressPostalCode = reader["Address_PostalCode"]?.ToString(),
                AddressCity = reader["Address_City"]?.ToString(),
                AddressEmail = reader["Address_Email"]?.ToString(),
                AddressLandLine = reader["Address_LandLine"]?.ToString(),
                AddressMobileLine = reader["Address_MobileLine"]?.ToString(),
                TransactionReceiptTopIndentation = Convert.ToByte(reader["TransactionReceiptTopIndentation"]),
                TransactionReceiptLeftIndentation = Convert.ToByte(reader["TransactionReceiptLeftIndentation"]),
                TransactionReceiptFooter = reader["TransactionReceiptFooter"]?.ToString(),
                FingerprintBiometricThreshold = Convert.ToInt32(reader["FingerprintBiometricThreshold"]),
                MembershipTerminationNoticePeriod = Convert.ToInt16(reader["MembershipTerminationNoticePeriod"]),
                TimeDurationStartTime = (TimeSpan)reader["TimeDuration_StartTime"],
                TimeDurationEndTime = (TimeSpan)reader["TimeDuration_EndTime"],
                ApplicationMembershipTextAlertsEnabled = Convert.ToBoolean(reader["ApplicationMembershipTextAlertsEnabled"]),
                EnforceCustomerAccountMakerChecker = Convert.ToBoolean(reader["EnforceCustomerAccountMakerChecker"]),
                BypassJournalVoucherAudit = Convert.ToBoolean(reader["BypassJournalVoucherAudit"]),
                BypassCreditBatchAudit = Convert.ToBoolean(reader["BypassCreditBatchAudit"]),
                BypassDebitBatchAudit = Convert.ToBoolean(reader["BypassDebitBatchAudit"]),
                BypassRefundBatchAudit = Convert.ToBoolean(reader["BypassRefundBatchAudit"]),
                BypassWireTransferBatchAudit = Convert.ToBoolean(reader["BypassWireTransferBatchAudit"]),
                BypassLoanDisbursementBatchAudit = Convert.ToBoolean(reader["BypassLoanDisbursementBatchAudit"]),
                BypassJournalReversalBatchAudit = Convert.ToBoolean(reader["BypassJournalReversalBatchAudit"]),
                BypassInterAccountTransferBatchAudit = Convert.ToBoolean(reader["BypassInterAccountTransferBatchAudit"]),
                BypassExpensePayableAudit = Convert.ToBoolean(reader["BypassExpensePayableAudit"]),
                BypassGeneralLedgerAudit = Convert.ToBoolean(reader["BypassGeneralLedgerAudit"]),
                ExcludeChargesInTransactionReceipt = Convert.ToBoolean(reader["ExcludeChargesInTransactionReceipt"]),
                ExcludeChequeMaturityDateInTransactionReceipt = Convert.ToBoolean(reader["ExcludeChequeMaturityDateInTransactionReceipt"]),
                TrackGuarantorCommittedInvestments = Convert.ToBoolean(reader["TrackGuarantorCommittedInvestments"]),
                TransferNetRefundableAmountToSavingsAccountOnDeathClaimSettlement = Convert.ToBoolean(reader["TransferNetRefundableAmountToSavingsAccountOnDeathClaimSettlement"]),
                ReceiveLoanRequestBeforeLoanRegistration = Convert.ToBoolean(reader["ReceiveLoanRequestBeforeLoanRegistration"]),
                LocalizeOnlineNotifications = Convert.ToBoolean(reader["LocalizeOnlineNotifications"]),
                IsWithholdingTaxAgent = Convert.ToBoolean(reader["IsWithholdingTaxAgent"]),
                EnforceBudgetControl = Convert.ToBoolean(reader["EnforceBudgetControl"]),
                IsFileTrackingEnforced = Convert.ToBoolean(reader["IsFileTrackingEnforced"]),
                ExcludeCustomerAccountBalanceInTransactionReceipt = Convert.ToBoolean(reader["ExcludeCustomerAccountBalanceInTransactionReceipt"]),
                EnforceFixedDepositBands = Convert.ToBoolean(reader["EnforceFixedDepositBands"]),
                EnforceBiometricsForCashWithdrawal = Convert.ToBoolean(reader["EnforceBiometricsForCashWithdrawal"]),
                EnforceTwoFactorAuthentication = Convert.ToBoolean(reader["EnforceTwoFactorAuthentication"]),
                RecoverArrearsOnCashDeposit = Convert.ToBoolean(reader["RecoverArrearsOnCashDeposit"]),
                RecoverArrearsOnExternalChequeClearance = Convert.ToBoolean(reader["RecoverArrearsOnExternalChequeClearance"]),
                RecoverArrearsOnFixedDepositPayment = Convert.ToBoolean(reader["RecoverArrearsOnFixedDepositPayment"]),
                AllowDebitBatchToOverdrawAccount = Convert.ToBoolean(reader["AllowDebitBatchToOverdrawAccount"]),
                EnforceSystemLock = Convert.ToBoolean(reader["EnforceSystemLock"]),
                EnforceTellerLimits = Convert.ToBoolean(reader["EnforceTellerLimits"]),
                EnforceTellerCashTransferAcknowledgement = Convert.ToBoolean(reader["EnforceTellerCashTransferAcknowledgement"]),
                EnforceSingleUserSession = Convert.ToBoolean(reader["EnforceSingleUserSession"]),
                CustomerMembershipTextAlertsEnabled = Convert.ToBoolean(reader["CustomerMembershipTextAlertsEnabled"]),
                EnforceInvestmentProductExemptions = Convert.ToBoolean(reader["EnforceInvestmentProductExemptions"]),
                EnforceMobileToBankReconciliationVerification = Convert.ToBoolean(reader["EnforceMobileToBankReconciliationVerification"]),
                IsLocked = Convert.ToBoolean(reader["IsLocked"]),
                CreatedBy = reader["CreatedBy"]?.ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
            };
        }
    }
}