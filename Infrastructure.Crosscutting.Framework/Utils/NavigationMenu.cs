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
                new NavigationMenu{Description = "Administration", IsArea = true, Code = 0x00004E20},
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "Companies", Icon="fa fa-users", ControllerName="Company", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 1},
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "Branches", Icon="fa fa-database", ControllerName="Branch", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 2},
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "System Roles", Icon="fa fa-low-vision", ControllerName="Role", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 3},
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "System Users", Icon="fa fa-user", ControllerName="Membership", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 4 },
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "Access Controls", Icon="fa fa-cog", ControllerName="Module", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 5 },

                //Registry - area 21,000
                new NavigationMenu{Description = "Registry", IsArea = true, Code = 0x00005208},
                new NavigationMenu{AreaCode = 0x00005208, IsArea = false, Description = "Employers", Icon="fa fa-calculator", ControllerName="Employer", ActionName="Index", AreaName = "Registry", Code = 0x00005208 + 1},
                new NavigationMenu{AreaCode = 0x00005208, IsArea = false, Description = "Zones", Icon="fa fa-low-vision", ControllerName="Zone", ActionName="Index", AreaName = "Registry", Code = 0x00005208 + 2},
                new NavigationMenu{AreaCode = 0x00005208, IsArea = false, Description = "Customers", Icon="fa fa-users", ControllerName="Customer", ActionName="Index", AreaName = "Registry", Code = 0x00005208 + 3},
               
                //Human Resource - area 22,000
                new NavigationMenu{Description = "Human Resource", IsArea = true, Code = 0x000055F0},
                new NavigationMenu{AreaCode = 0x000055F0, IsArea = false, Description = "Employers", Icon="fa fa-usb", ControllerName="Employer", ActionName="Index", AreaName = "Client", Code = 0x000055F0 + 1},
                new NavigationMenu{AreaCode = 0x000055F0, IsArea = false, Description = "Insurers", Icon="fa fa-usb", ControllerName="Insurer", ActionName="Index", AreaName = "Client", Code = 0x000055F0 + 2},
                new NavigationMenu{AreaCode = 0x000055F0, IsArea = false, Description = "Customers", Icon="fa fa-users", ControllerName="Customer", ActionName="Index", AreaName = "Client", Code = 0x000055F0 + 3},
               
                //test - area 22,000
                new NavigationMenu{Description = "Human Resource", IsArea = true, Code = 0x000055F0},
                // menu-items (children)
                new NavigationMenu{AreaCode = 0x000055F0, IsArea = false, Description = "Setup", Icon="fa fa-users", Code = 0x000055F0 + 1},
                new NavigationMenu{AreaCode = 0x000055F0, IsArea = false, Description = "Operations", Icon="fa fa-users", Code = 0x000055F0 + 2},

                new NavigationMenu{AreaCode = 0x000055F0 + 1, IsArea = false, Description = "Departments", Icon="fa fa-users", ControllerName="Department", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 3},
                new NavigationMenu{AreaCode = 0x000055F0 + 1, IsArea = false, Description = "Designations", Icon="fa fa-users", ControllerName="Designation", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 4},
                new NavigationMenu{AreaCode = 0x000055F0 + 1, IsArea = false, Description = "Holidays", Icon="fa fa-users", ControllerName="Holiday", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 5},
                new NavigationMenu{AreaCode = 0x000055F0 + 1, IsArea = false, Description = "Employee Types", Icon="fa fa-users", ControllerName="EmployeeType", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 6},

                new NavigationMenu{AreaCode = 0x000055F0 + 2, IsArea = false, Description = "Employees", Icon="fa fa-users", ControllerName="Employee", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 6},
                new NavigationMenu{AreaCode = 0x000055F0 + 2, IsArea = false, Description = "Leave", Icon="fa fa-users", ControllerName="Leave", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 6},
                new NavigationMenu{AreaCode = 0x000055F0 + 2, IsArea = false, Description = "Salary", Icon="fa fa-users", ControllerName="Salary", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 6},

                //Accounts - area 23,000
                new NavigationMenu{Description = "Accounts", IsArea = true, Code = 0x000059D8},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Chart of Accounts", Icon="fa fa-usb", ControllerName="ChartOfAccount", ActionName="Index", AreaName = "Finance", Code = 0x000059D8 + 1},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Commissions", Icon="fa fa-users", ControllerName="Commissions", ActionName="Index", AreaName = "Finance", Code = 0x000059D8 + 2},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Journals", Icon="fa fa-calculator", ControllerName="Journal", ActionName="Index", AreaName = "Finance", Code = 0x000059D8 + 3},

                //Loaning - area 24,000
                new NavigationMenu{Description = "Loaning", IsArea = true, Code = 0x00005DC0},
                new NavigationMenu{AreaCode = 0x00005DC0, IsArea = false, Description = "Policy Types", Icon="fa fa-calculator", ControllerName="PolicyType", ActionName="Index", AreaName = "Underwriting", Code = 0x00005DC0 + 1},
                new NavigationMenu{AreaCode = 0x00005DC0, IsArea = false, Description = "Claim Types", Icon="fa fa-users", ControllerName="ClaimType", ActionName="Index", AreaName = "Underwriting", Code = 0x00005DC0 + 2},
                new NavigationMenu{AreaCode = 0x00005DC0, IsArea = false, Description = "Policies", Icon="fa fa-low-vision", ControllerName="Policy", ActionName="Index", AreaName = "Underwriting", Code = 0x00005DC0 + 3},
                new NavigationMenu{AreaCode = 0x00005DC0, IsArea = false, Description = "Claims", Icon="fa fa-book", ControllerName="Claim", ActionName="Index", AreaName = "Underwriting", Code = 0x00005DC0 + 4},

                //Analytics - area 25,000
                new NavigationMenu{Description = "Front Office", IsArea = true, Code = 0x000061A8},
                new NavigationMenu{AreaCode = 0x000061A8, IsArea = false, Description = "Policy Types", Icon="fa fa-calculator", ControllerName="PolicyType", ActionName="Index", AreaName = "Underwriting", Code = 0x000061A8 + 1},
                new NavigationMenu{AreaCode = 0x000061A8, IsArea = false, Description = "Claim Types", Icon="fa fa-users", ControllerName="ClaimType", ActionName="Index", AreaName = "Underwriting", Code = 0x000061A8 + 2},
                new NavigationMenu{AreaCode = 0x000061A8, IsArea = false, Description = "Policies", Icon="fa fa-low-vision", ControllerName="Policy", ActionName="Index", AreaName = "Underwriting", Code = 0x000061A8 + 3},
                new NavigationMenu{AreaCode = 0x000061A8, IsArea = false, Description = "Claims", Icon="fa fa-book", ControllerName="Claim", ActionName="Index", AreaName = "Underwriting", Code = 0x000061A8 + 4},

                //messaging - area 26,000
                new NavigationMenu{Description = "Messaging", IsArea = true, Code = 0x00006590},
                new NavigationMenu{AreaCode = 0x00006590, IsArea = false, Description = "Text Alerts", Icon="fa fa-comments", ControllerName="TextAlert", ActionName="Index", AreaName = "Messaging", Code = 0x00006590 + 1},
                new NavigationMenu{AreaCode = 0x00006590, IsArea = false, Description = "Email Alerts", Icon="fa fa-envelope", ControllerName="EmailAlert", ActionName="Index", AreaName = "Messaging", Code = 0x00006590 + 2},

                //Analytics - area 27,000
                new NavigationMenu{Description = "Analytics", IsArea = true, Code = 0x00006978},
                new NavigationMenu{AreaCode = 0x00006978, IsArea = false, Description = "Policy Types", Icon="fa fa-calculator", ControllerName="PolicyType", ActionName="Index", AreaName = "Underwriting", Code = 0x00006978 + 1},
              
                //reports - area 28,000
                new NavigationMenu{Description = "Reporting", IsArea = true, Code = 0x00006D60},
                new NavigationMenu{AreaCode = 0x00006D60, IsArea = false, Description = "Reports", Icon="fa fa-bars", ControllerName="Report", ActionName="index", AreaName = "Reports", Code = 0x00006D60 + 1},
             
            /*
                //test - area 62,000
                new NavigationMenu{Description = "Grandfather", IsArea = true, Code = 0xF230},
                //test menu-items (children)
                new NavigationMenu{AreaCode = 0xF230, IsArea = false, Description = "Father1", Icon="fa fa-users", Code = 0xF230 + 1},
                new NavigationMenu{AreaCode = 0xF230, IsArea = false, Description = "Father2", Icon="fa fa-users", Code = 0xF230 + 2},

                new NavigationMenu{AreaCode = 0xF230 + 1, IsArea = false, Description = "Child1_F1", Icon="fa fa-users", ControllerName="Home", ActionName="IndexOne", AreaName = "", Code = 0xF230 + 3},
                new NavigationMenu{AreaCode = 0xF230 + 1, IsArea = false, Description = "Child2_F1", Icon="fa fa-users", ControllerName="Home", ActionName="IndexTwo", AreaName = "", Code = 0xF230 + 4},

                new NavigationMenu{AreaCode = 0xF230 + 2, IsArea = false, Description = "Child1_F2", Icon="fa fa-users", Code = 0xF230 + 5},
                new NavigationMenu{AreaCode = 0xF230 + 2, IsArea = false, Description = "Child2_F2", Icon="fa fa-users", ControllerName="Home", ActionName="IndexThree", AreaName = "", Code = 0xF230 + 6},

                new NavigationMenu{AreaCode =  0xF230 + 5, IsArea = false, Description = "Child_Child1_F2", Icon="fa fa-users", Code = 0xF230 + 7},
                new NavigationMenu{AreaCode =  0xF230 + 5, IsArea = false, Description = "Child_Child2_F2", Icon="fa fa-users", ControllerName="Home", ActionName="IndexFour", AreaName = "", Code = 0xF230 + 8},

                new NavigationMenu{AreaCode =  0xF230 + 7, IsArea = false, Description = "Child_Child1_F2_2", Icon="fa fa-users", ControllerName="Home", ActionName="IndexFive", AreaName = "", Code = 0xF230 + 9},
            */
            };

            return menus;
        }
    }
}