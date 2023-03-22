using Application.Seedwork;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Application.MainBoundedContext.DTO.AdministrationModule
{
    public class ModuleNavigationItemDTO : BindingModelBase<ModuleNavigationItemDTO>
    {
        public ModuleNavigationItemDTO()
        {
            AddAllAttributeValidators();
        }

        [DataMember]
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember]
        [Display(Name = "Module")]
        public Guid ModuleId { get; set; }

        [DataMember]
        [Display(Name = "Module Description")]
        public string ModuleDescription { get; set; }

        [DataMember]
        [Display(Name = "Item Code")]
        public int ItemCode { get; set; }

        [DataMember]
        [Display(Name = "Item Description")]
        public string ItemDescription { get; set; }

        [DataMember]
        [Display(Name = "Parent Item Code")]
        public int ParentItemCode { get; set; }

        [DataMember]
        [Display(Name = "Parent Item Description")]
        public string ParentItemDescription { get; set; }

        [DataMember]
        [Display(Name = "Indented Name")]
        public string IndentedName { get; set; }

        [DataMember]
        [Display(Name = "Depth")]
        public int Depth { get; set; }

        [DataMember]
        [Display(Name = "Is Navigable?")]
        public bool IsNavigable { get; set; }

        [DataMember]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [DataMember]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
    }
}
