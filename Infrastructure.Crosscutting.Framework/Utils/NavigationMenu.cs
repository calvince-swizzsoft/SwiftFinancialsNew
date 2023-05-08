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
                // Admin - area 20,000
                new NavigationMenu{Description = "Administration", IsArea = true, Code = 0x00004E20},
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "Companies", Icon="fa fa-users", ControllerName="Company", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 1},
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "Branches", Icon="fa fa-database", ControllerName="Branch", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 2},
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "Banks", Icon="fa fa-calculator", ControllerName="Bank", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 3},
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "System Roles", Icon="fa fa-low-vision", ControllerName="Role", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 4},
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "System Users", Icon="fa fa-user", ControllerName="Membership", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 5 },
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "Access Controls", Icon="fa fa-cog", ControllerName="Module", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 6 },
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "Reports", Icon="fa fa-calculator", ControllerName="Report", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 7},
                new NavigationMenu{AreaCode = 0x00004E20, IsArea = false, Description = "Locations", Icon="fa fa-low-vision", ControllerName="Location", ActionName="Index", AreaName = "Admin", Code = 0x00004E20 + 8},


                //Registry - area 21,000
                new NavigationMenu{Description = "Registry", IsArea = true, Code = 0x00005208},
                new NavigationMenu{AreaCode = 0x00005208, IsArea = false, Description = "Employers", Icon="fa fa-database", ControllerName="Employer", ActionName="Index", AreaName = "Registry", Code = 0x00005208 + 1},
                new NavigationMenu{AreaCode = 0x00005208, IsArea = false, Description = "Zones", Icon="fa fa-low-vision", ControllerName="Zone", ActionName="Index", AreaName = "Registry", Code = 0x00005208 + 2},
                new NavigationMenu{AreaCode = 0x00005208, IsArea = false, Description = "Customers", Icon="fa fa-users", ControllerName="Customer", ActionName="Index", AreaName = "Registry", Code = 0x00005208 + 3},
                new NavigationMenu{AreaCode = 0x00005208, IsArea = false, Description = "Educational Venues", Icon="fa fa-cog", ControllerName="EducationVenue", ActionName="Index", AreaName = "Registry", Code = 0x00005208 + 4},
                new NavigationMenu{AreaCode = 0x00005208, IsArea = false, Description = "Divisions", Icon="fa fa-usb", ControllerName="Division", ActionName="Index", AreaName = "Registry", Code = 0x00005208 + 5},
                new NavigationMenu{AreaCode = 0x00005208, IsArea = false, Description = "Stations", Icon="fa fa-calculator", ControllerName="Station", ActionName="Index", AreaName = "Registry", Code = 0x00005208 + 6},
               
                //Human Resource - area 22,000
                new NavigationMenu{Description = "Human Resource", IsArea = true, Code = 0x000055F0},
                // menu-items (children)
                new NavigationMenu{AreaCode = 0x000055F0 , IsArea = false, Description = "Departments", Icon="fa fa-usb", ControllerName="Department", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 1},
                new NavigationMenu{AreaCode = 0x000055F0 , IsArea = false, Description = "Designations", Icon="fa fa-usb", ControllerName="Designation", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 2},
                new NavigationMenu{AreaCode = 0x000055F0 , IsArea = false, Description = "Holidays", Icon="fa-spotify", ControllerName="Holiday", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 3},
                new NavigationMenu{AreaCode = 0x000055F0 , IsArea = false, Description = "Employee Types", Icon="fa fa-usb", ControllerName="EmployeeType", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 4},
                new NavigationMenu{AreaCode = 0x000055F0 , IsArea = false, Description = "Employees", Icon="fa fa-usb", ControllerName="Employee", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 5},
                new NavigationMenu{AreaCode = 0x000055F0 , IsArea = false, Description = "Leave", Icon="fa fa-usb", ControllerName="Leave", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 6},
                new NavigationMenu{AreaCode = 0x000055F0 , IsArea = false, Description = "Salary", Icon="fa fa-usb", ControllerName="Salary", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 7},
                new NavigationMenu{AreaCode = 0x000055F0 , IsArea = false, Description = "Training Periods", Icon="fa fa-bars", ControllerName="TrainingPeriod", ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 8},

                //Accounts - area 23,000
                new NavigationMenu{Description = "Accounts", IsArea = true, Code = 0x000059D8},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Chart of Accounts", Icon="fa fa-usb", ControllerName="ChartOfAccount", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 1},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Commissions", Icon="fa fa-users", ControllerName="Commission", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 2},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Journals", Icon="fa fa-calculator", ControllerName="Journal", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 3},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Posting Periods", Icon="fa fa-low-vision", ControllerName="PostingPeriod", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 7},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Cost Centers", Icon="fa fa-calculator", ControllerName="CostCenter", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 4},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Credit Types", Icon="fa fa-calculator", ControllerName="CreditType", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 5},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Debit Types", Icon="fa fa-calculator", ControllerName="DebitType", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 6},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Levies", Icon="fa fa-calculator", ControllerName="Levy", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 7},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Budgets", Icon="fa fa-money", ControllerName="Budget", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 8},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Budget Entries", Icon="fa fa-usd-alias", ControllerName="BudgetEntry", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 9},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Treasuries", Icon="fa fa-usd", ControllerName="Treasury", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 10},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Cheque Types", Icon="fa fa-book", ControllerName="ChequeType", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 11},
                new NavigationMenu{AreaCode = 0x000059D8, IsArea = false, Description = "Saving Products", Icon="fa-envelope-square", ControllerName="SavingsProduct", ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 12},
                //Loaning - area 24,000
                new NavigationMenu{Description = "Loaning", IsArea = true, Code = 0x00005DC0},
                // menu-items (children)
                new NavigationMenu{AreaCode = 0x00005DC0, IsArea = false, Description = "Setup", Icon="fa fa-bars", Code = 0x00005DC0 + 1},
                new NavigationMenu{AreaCode = 0x00005DC0, IsArea = false, Description = "Operations", Icon="fa fa-bars", Code = 0x00005DC0 + 2},

                new NavigationMenu{AreaCode = 0x00005DC0 + 1, IsArea = false, Description = "Loan Purposes", Icon="fa fa-bars", ControllerName="LoanPurpose", ActionName="Index", AreaName = "Loaning", Code = 0x00005DC0 + 3},
                new NavigationMenu{AreaCode = 0x00005DC0 + 1, IsArea = false, Description = "Loaning Remarks", Icon="fa fa-bars", ControllerName="LoaningRemark", ActionName="Index", AreaName = "Loaning", Code = 0x00005DC0 + 4},
                new NavigationMenu{AreaCode = 0x00005DC0 + 1, IsArea = false, Description = "Income Adjustments", Icon="fa fa-bars", ControllerName="IncomeAdjustment", ActionName="Index", AreaName = "Loaning", Code = 0x00005DC0 + 5},
                new NavigationMenu{AreaCode = 0x00005DC0 + 1, IsArea = false, Description = "Loaning Requests", Icon="fa fa-book", ControllerName="LoanRequest", ActionName="Index", AreaName = "Loaning", Code = 0x00005DC0 + 6},

                new NavigationMenu{AreaCode = 0x00005DC0 + 2, IsArea = false, Description = "Applications", Icon="fa fa-bars", ControllerName="Application", ActionName="Index", AreaName = "Loaning", Code = 0x00005DC0 + 6},
                new NavigationMenu{AreaCode = 0x00005DC0 + 2, IsArea = false, Description = "Loan Cases", Icon="fa fa-bars", ControllerName="Loaning", ActionName="Index", AreaName = "Loaning", Code = 0x00005DC0 + 7},
                new NavigationMenu{AreaCode = 0x00005DC0 + 2, IsArea = false, Description = "Data Capture", Icon="fa fa-bars", ControllerName="DataCapture", ActionName="Index", AreaName = "Loaning", Code = 0x00005DC0 + 8},

                //Front Office - area 25,000
                new NavigationMenu{Description = "Front Office", IsArea = true, Code = 0x000061A8},
                new NavigationMenu{AreaCode = 0x000061A8, IsArea = false, Description = "Treasury", Icon="fa fa-calculator", ControllerName="Treasury", ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 1},
                new NavigationMenu{AreaCode = 0x000061A8, IsArea = false, Description = "Teller", Icon="fa fa-calculator", ControllerName="Teller", ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 2},
                new NavigationMenu{AreaCode = 0x000061A8, IsArea = false, Description = "Cheques", Icon="fa fa-low-calculator", ControllerName="Cheque", ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 3},
                new NavigationMenu{AreaCode = 0x000061A8, IsArea = false, Description = "Fixed Deposits", Icon="fa fa-calculator", ControllerName="FixedDeposit", ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 4},
                new NavigationMenu{AreaCode = 0x000061A8, IsArea = false, Description = "Expense Payables", Icon="fa fa-low-calculator", ControllerName="ExpensePayable", ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 5},
                new NavigationMenu{AreaCode = 0x000061A8, IsArea = false, Description = "Account Closure", Icon="fa fa-calculator", ControllerName="AccountClosure", ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 6},

                //messaging - area 26,000
                new NavigationMenu{Description = "Messaging", IsArea = true, Code = 0x00006590},
                new NavigationMenu{AreaCode = 0x00006590, IsArea = false, Description = "Text Alerts", Icon="fa fa-comments", ControllerName="TextAlert", ActionName="Index", AreaName = "Messaging", Code = 0x00006590 + 1},
                new NavigationMenu{AreaCode = 0x00006590, IsArea = false, Description = "Email Alerts", Icon="fa fa-envelope", ControllerName="EmailAlert", ActionName="Index", AreaName = "Messaging", Code = 0x00006590 + 2},

                //Analytics - area 27,000
                new NavigationMenu{Description = "Analytics", IsArea = true, Code = 0x00006978},
                new NavigationMenu{AreaCode = 0x00006978, IsArea = false, Description = "Analytics", Icon="fa fa-calculator", ControllerName="Analytic", ActionName="Index", AreaName = "Analytics", Code = 0x00006978 + 1},
              
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