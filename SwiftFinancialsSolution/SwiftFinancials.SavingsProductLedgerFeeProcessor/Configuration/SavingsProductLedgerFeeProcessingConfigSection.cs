using System.Configuration;

namespace SwiftFinancials.SavingsProductLedgerFeeProcessing.Configuration
{
    public class SavingsProductLedgerFeeProcessingConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("savingsProductLedgerFeeProcessingSettings")]
        public SavingsProductLedgerFeeProcessingSettingsCollection SavingsProductLedgerFeeProcessingSettingsItems
        {
            get { return ((SavingsProductLedgerFeeProcessingSettingsCollection)(base["savingsProductLedgerFeeProcessingSettings"])); }
        }
    }
}
