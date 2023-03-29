using System;
using System.Collections.Generic;

namespace Infrastructure.Crosscutting.Framework.Utils
{
    public class NavigationMenu
    {
        public Guid? ParentId { get; private set; }

        public bool IsArea { get; private set; }

        public string Description { get; private set; }

        public string Icon { get; private set; }

        public string ControllerName { get; private set; }

        public string ActionName { get; private set; }

        public string AreaName { get; private set; }

        public int Code { get; private set; }

        public int AreaCode { get; private set; }

        public List<NavigationMenu> GetMenus()
        {
            var menus = new List<NavigationMenu>()
            {
                //Admin - area 20,000
                new NavigationMenu{Description = "Administration", IsArea = true, Code = 0x4E20},
                new NavigationMenu{AreaCode = 0x4E20, IsArea = false, Description = "Companies", Icon="fa fa-users", ControllerName="Company", ActionName="Index", AreaName = "Admin", Code = 0x4E20 + 1},
                new NavigationMenu{AreaCode = 0x4E20, IsArea = false, Description = "Branches", Icon="fa fa-database", ControllerName="Branch", ActionName="Index", AreaName = "Admin", Code = 0x4E20 + 2},
                new NavigationMenu{AreaCode = 0x4E20, IsArea = false, Description = "System Roles", Icon="fa fa-low-vision", ControllerName="Role", ActionName="Index", AreaName = "Admin", Code = 0x4E20 + 3},
                new NavigationMenu{AreaCode = 0x4E20, IsArea = false, Description = "System Users", Icon="fa fa-user", ControllerName="Membership", ActionName="Index", AreaName = "Admin", Code = 0x4E20 + 4 },
                new NavigationMenu{AreaCode = 0x4E20, IsArea = false, Description = "Access Controls", Icon="fa fa-cog", ControllerName="Module", ActionName="Index", AreaName = "Admin", Code = 0x4E20 + 5 },

                //Underwriting - area 21,000
                new NavigationMenu{Description = "Clients", IsArea = true, Code = 0x5208},
                new NavigationMenu{AreaCode = 0x5208, IsArea = false, Description = "Employers", Icon="fa fa-usb", ControllerName="Employer", ActionName="Index", AreaName = "Client", Code = 0x5208 + 1},
                new NavigationMenu{AreaCode = 0x5208, IsArea = false, Description = "Insurers", Icon="fa fa-usb", ControllerName="Insurer", ActionName="Index", AreaName = "Client", Code = 0x5208 + 2},
                new NavigationMenu{AreaCode = 0x5208, IsArea = false, Description = "Customers", Icon="fa fa-users", ControllerName="Customer", ActionName="Index", AreaName = "Client", Code = 0x5208 + 3},
               
                 //Underwriting - area 27,000
                new NavigationMenu{Description = "Registry", IsArea = true, Code = 0x00006978},
                new NavigationMenu{AreaCode = 0x00006978, IsArea = false, Description = "Administrative Divisions", Icon="fa fa-calculator", ControllerName="AdministrativeDivision", ActionName="Index", AreaName = "Registry", Code = 0x00006978 + 1},
                //new NavigationMenu{AreaCode = 0x00006978, IsArea = false, Description = "Claim Types", Icon="fa fa-users", ControllerName="ClaimType", ActionName="Index", AreaName = "Registry", Code = 0x00006978 + 2},
                //new NavigationMenu{AreaCode = 0x00006978, IsArea = false, Description = "Policies", Icon="fa fa-low-vision", ControllerName="Policy", ActionName="Index", AreaName = "Registry", Code = 0x00006978 + 3},
                //new NavigationMenu{AreaCode = 0x00006978, IsArea = false, Description = "Claims", Icon="fa fa-book", ControllerName="Claim", ActionName="Index", AreaName = "Registry", Code = 0x00006978 + 4},


                //Underwriting - area 22,000
                new NavigationMenu{Description = "Underwriting", IsArea = true, Code = 0x000055F0},
                new NavigationMenu{AreaCode = 0x000055F0, IsArea = false, Description = "Policy Types", Icon="fa fa-calculator", ControllerName="PolicyType", ActionName="Index", AreaName = "Underwriting", Code = 0x000055F0 + 1},
                new NavigationMenu{AreaCode = 0x000055F0, IsArea = false, Description = "Claim Types", Icon="fa fa-users", ControllerName="ClaimType", ActionName="Index", AreaName = "Underwriting", Code = 0x000055F0 + 2},
                new NavigationMenu{AreaCode = 0x000055F0, IsArea = false, Description = "Policies", Icon="fa fa-low-vision", ControllerName="Policy", ActionName="Index", AreaName = "Underwriting", Code = 0x000055F0 + 3},
                new NavigationMenu{AreaCode = 0x000055F0, IsArea = false, Description = "Claims", Icon="fa fa-book", ControllerName="Claim", ActionName="Index", AreaName = "Underwriting", Code = 0x000055F0 + 4},

                //Underwriting - area 23,000
                new NavigationMenu{Description = "Finance", IsArea = true, Code = 0x000059D8},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Chart of Accounts", Icon="fa fa-usb", ControllerName="ChartOfAccount", ActionName="Index", AreaName = "Finance", Code = 0x000059D8 + 1},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Commissions", Icon="fa fa-users", ControllerName="Commissions", ActionName="Index", AreaName = "Finance", Code = 0x000059D8 + 2},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Journals", Icon="fa fa-calculator", ControllerName="Journal", ActionName="Index", AreaName = "Finance", Code = 0x000059D8 + 3},

                //messaging - area 25,000
                new NavigationMenu{Description = "Messaging", IsArea = true, Code = 0x61A8},
                new NavigationMenu{AreaCode = 0x61A8, IsArea = false, Description = "Text Alerts", Icon="fa fa-comments", ControllerName="TextAlert", ActionName="Index", AreaName = "Messaging", Code = 0x61A8 + 1},
                new NavigationMenu{AreaCode = 0x61A8, IsArea = false, Description = "Email Alerts", Icon="fa fa-envelope", ControllerName="EmailAlert", ActionName="Index", AreaName = "Messaging", Code = 0x61A8 + 2},

                //reports - area 26,000
                new NavigationMenu{Description = "Reporting", IsArea = true, Code = 0x6590},
                new NavigationMenu{AreaCode = 0x6590, IsArea = false, Description = "Reports", Icon="fa fa-bars", ControllerName="Report", ActionName="index", AreaName = "Reports", Code = 0x6590 + 1},
             };

            return menus;
        }
    }
}