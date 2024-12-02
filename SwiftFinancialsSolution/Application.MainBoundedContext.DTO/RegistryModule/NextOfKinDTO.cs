using Application.Seedwork;
using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Application.MainBoundedContext.DTO.RegistryModule
{
    public class NextOfKinDTO : BindingModelBase<NextOfKinDTO>
    {
        public NextOfKinDTO()
        {
            AddAllAttributeValidators();
        }

        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [Display(Name = "Customer")]
        public Guid CustomerId { get; set; }

        [Display(Name = "Customer Type")]
        public byte CustomerType { get; set; }

        [Display(Name = "Customer Type")]
        public string CustomerTypeDescription
        {
            get
            {
                return EnumHelper.GetDescription((CustomerType)CustomerType);
            }
        }

        [Display(Name = "Customer Salutation")]
        public byte CustomerIndividualSalutation { get; set; }

        [Display(Name = "Customer Salutation")]
        public string CustomerIndividualSalutationDescription
        {
            get
            {
                return EnumHelper.GetDescription((Salutation)CustomerIndividualSalutation);
            }
        }

        [Display(Name = "Customer First Name")]
        public string CustomerIndividualFirstName { get; set; }

        [Display(Name = "Customer Other Names")]
        public string CustomerIndividualLastName { get; set; }
        
        [Display(Name = "Group Name")]
        public string CustomerNonIndividualDescription { get; set; }

        [Display(Name = "Customer Name")]
        public string CustomerFullName
        {
            get
            {
                var result = string.Empty;

                switch ((CustomerType)CustomerType)
                {
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Individual:
                        result = string.Format("{0} {1} {2}", CustomerIndividualSalutationDescription, CustomerIndividualFirstName, CustomerIndividualLastName).Trim();
                        break;
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Partnership:
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.Corporation:
                    case Infrastructure.Crosscutting.Framework.Utils.CustomerType.MicroCredit:
                        result = CustomerNonIndividualDescription;
                        break;
                    default:
                        break;
                }

                return result;
            }
        }

        [Display(Name = "Salutation")]
        public byte Salutation { get; set; }

        [Display(Name = "Salutation")]
        public string SalutationDescription
        {
            get
            {
                return EnumHelper.GetDescription((Salutation)Salutation);
            }
        }

        [Display(Name = "Gender")]
        public byte Gender { get; set; }

        [Display(Name = "Gender")]
        public string GenderDescription
        {
            get
            {
                return EnumHelper.GetDescription((Gender)Gender);
            }
        }

        [Display(Name = "Relationship")]
        public byte Relationship { get; set; }

        [Display(Name = "Relationship")]
        public string RelationshipDescription
        {
            get
            {
                return EnumHelper.GetDescription((NextOfKinRelationship)Relationship);
            }
        }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Other Names")]
        public string LastName { get; set; }

        [Display(Name = "Name")]
        public string FullName
        {
            get
            {
                return string.Format("{0} {1} {2}", SalutationDescription, FirstName, LastName);
            }
        }

        [Display(Name = "Identity Card Type")]
        public byte IdentityCardType { get; set; }

        [Display(Name = "Identity Card Type")]
        public string IdentityCardTypeDescription
        {
            get
            {
                return EnumHelper.GetDescription((IdentityCardType)IdentityCardType);
            }
        }

        [Display(Name = "Identity Card Number")]
        public string IdentityCardNumber { get; set; }

        [Display(Name = "Address Line 1")]
        public string AddressAddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        public string AddressAddressLine2 { get; set; }

        [Display(Name = "Street")]
        public string AddressStreet { get; set; }

        [Display(Name = "Postal Code")]
        public string AddressPostalCode { get; set; }

        [Display(Name = "City")]
        public string AddressCity { get; set; }

        [Display(Name = "E-mail")]
        public string AddressEmail { get; set; }

        [Display(Name = "Land Line")]
        public string AddressLandLine { get; set; }

        [Display(Name = "Mobile Line")]
        public string AddressMobileLine { get; set; }

        [Display(Name = "Nominated Percentage")]
        public double NominatedPercentage { get; set; }

        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }


        [DataMember]
        public CustomerDTO CustomerDTO { get; set; }

    }
}
