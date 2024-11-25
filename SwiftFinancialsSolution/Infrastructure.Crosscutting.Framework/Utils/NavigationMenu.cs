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
               // Administration Module...
                new NavigationMenu { Description = "Administration", IsArea = true, Code = 0x00004E20 },
                new NavigationMenu { AreaCode = 0x00004E20, IsArea = false, Description = "Setup", Icon = "fa fa-bars", Code = 0x00004E20 + 1 },
                new NavigationMenu { AreaCode = 0x00004E20, IsArea = false, Description = "Operations", Icon = "fa fa-bars", Code = 0x00004E20 + 2 },

                // Setup
                new NavigationMenu { AreaCode = 0x00004E20 + 1, IsArea = false, Description = "Companies", Icon = "fa fa-university", ControllerName = "Company", ActionName = "Index", AreaName = "Admin",
                    Code = 0x00004E20 + 3 },
                new NavigationMenu { AreaCode = 0x00004E20 + 1, IsArea = false, Description = "Branches", Icon = "fa fa-list", ControllerName = "Branch", ActionName = "Index", AreaName = "Admin",
                    Code = 0x00004E20 + 4 },
                new NavigationMenu { AreaCode = 0x00004E20 + 1, IsArea = false, Description = "Banks", Icon = "fa fa-university", ControllerName = "Bank", ActionName = "Index", AreaName = "Admin",
                    Code = 0x00004E20 + 5 },
                new NavigationMenu { AreaCode = 0x00004E20 + 1, IsArea = false, Description = "Locations", Icon = "fa fa-map-marker", ControllerName = "Location", ActionName = "Index", AreaName = "Admin",
                    Code = 0x00004E20 + 6 },

                // Operations
                new NavigationMenu { AreaCode = 0x00004E20 + 2, IsArea = true, Description = "Security", Icon = "", Code = 0x00004E20 + 7 },
                new NavigationMenu { AreaCode = 0x00004E20 + 7, IsArea = false, Description = "Audit Logs", Icon = "fa fa-file-text-o", ControllerName = "Controller", ActionName = "Index", AreaName = "Admin",
                    Code = 0x00004E20 + 8 },
                new NavigationMenu { AreaCode = 0x00004E20 + 7, IsArea = false, Description = "Roles", Icon = "fa fa-tags", ControllerName = "Controller", ActionName = "Index", AreaName = "Admin",
                    Code = 0x00004E20 + 9 },
                new NavigationMenu { AreaCode = 0x00004E20 + 7, IsArea = false, Description = "Users", Icon = "fa fa-users", ControllerName = "Controller", ActionName = "Index", AreaName = "Admin",
                    Code = 0x00004E20 + 10 },

                new NavigationMenu { AreaCode = 0x00004E20 + 2, IsArea = true, Description = "Access Control List", Icon = "", Code = 0x00004E20 + 11 },
                new NavigationMenu { AreaCode = 0x00004E20 + 11, IsArea = false, Description = "Modules", Icon = "fa fa-cog", ControllerName = "Controller", ActionName = "Index", AreaName = "Admin",
                    Code = 0x00004E20 + 12 },
                new NavigationMenu { AreaCode = 0x00004E20 + 11, IsArea = false, Description = "Operations", Icon = "fa fa-code-fork", ControllerName = "Controller", ActionName = "Index", AreaName = "Admin",
                    Code = 0x00004E20 + 13 },

                new NavigationMenu { AreaCode = 0x00004E20 + 2, IsArea = false, Description = "System Permission", Icon = "fa fa-gamepad", ControllerName = "Controller", ActionName = "Create",
                    AreaName = "Admin", Code = 0x00004E20 + 14 },
                new NavigationMenu { AreaCode = 0x00004E20 + 2, IsArea = false, Description = "Report Setting", Icon = "fa fa-table", ControllerName = "SSRSReportSetting", ActionName = "Create",
                    AreaName = "Admin", Code = 0x00004E20 + 15 },



                // Accounts...
                new NavigationMenu{Description = "Accounts", IsArea = true, Code = 0x000059D8},
                new NavigationMenu { AreaCode = 0x000059D8, IsArea = false, Description = "Setup", Icon = "fa fa-bars", Code = 0x000059D8 + 1 },
                new NavigationMenu { AreaCode = 0x000059D8, IsArea = false, Description = "Operations", Icon = "fa fa-bars", Code = 0x000059D8 + 2 },

                // Setup
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = true, Description = "G/L Accounts", Icon = "", Code = 0x000059D8 + 3 },
                new NavigationMenu { AreaCode = 0x000059D8 + 3, IsArea = false, Description = "Cost Centers", Icon = "fa fa-briefcase", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 4 },
                new NavigationMenu { AreaCode = 0x000059D8 + 3, IsArea = false, Description = "Chart Of Accounts", Icon = "fa fa-address-book", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 5 },
                new NavigationMenu { AreaCode = 0x000059D8 + 3, IsArea = false, Description = "G/L Account Determination", Icon = "fa fa-cogs", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 6 },

                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = true, Description = "Levies & Charges", Icon = "", Code = 0x000059D8 + 7 },
                new NavigationMenu { AreaCode = 0x000059D8 + 7, IsArea = false, Description = "Levies", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 8 },
                new NavigationMenu { AreaCode = 0x000059D8 + 7, IsArea = false, Description = "Charges", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 9 },

                new NavigationMenu { AreaCode = 0x000059D8 + 7, IsArea = true, Description = "Charge Determination", Icon = "", Code = 0x000059D8 + 10 },
                new NavigationMenu { AreaCode = 0x000059D8 + 10, IsArea = false, Description = "Well-Known Charges", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 11 },
                new NavigationMenu { AreaCode = 0x000059D8 + 10, IsArea = false, Description = "Indefinite Charges", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 12 },
                new NavigationMenu { AreaCode = 0x000059D8 + 10, IsArea = false, Description = "Text Alert Charges", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 13 },
                new NavigationMenu { AreaCode = 0x000059D8 + 10, IsArea = false, Description = "Alternate Channels", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 14 },

                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = true, Description = "Financial Products", Icon = "", Code = 0x000059D8 + 15 },
                new NavigationMenu { AreaCode = 0x000059D8 + 15, IsArea = false, Description = "Savings", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 16 },
                new NavigationMenu { AreaCode = 0x000059D8 + 15, IsArea = false, Description = "Investments", Icon = "fa fa-car", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 17 },
                new NavigationMenu { AreaCode = 0x000059D8 + 15, IsArea = false, Description = "Loans", Icon = "fa fa-handshake-o", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 18 },

                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Posting Periods", Icon = "fa fa-calendar-check-o", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 19 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Treasuries", Icon = "fa fa-users", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 20 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Tellers", Icon = "fa fa-users", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 21 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Bank Linkages", Icon = "fa fa-university", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 22 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Cheque Types", Icon = "fa fa-envelope-open-o", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 23 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Direct Debits", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 24 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Credit Types", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 25 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Debit Types", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 26 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Insurers", Icon = "fa fa-building", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 27 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Unpay Reasons", Icon = "fa fa-hand-paper-o", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 28 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Fixed Deposit Types", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 29 },
                new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Wire Transfer Types", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 30 },


                // Operations
                new NavigationMenu { AreaCode = 0x000059D8 + 2, IsArea = true, Description = "Transactions Journal", Icon = "", Code = 0x000059D8 + 31 },
                new NavigationMenu { AreaCode = 0x000059D8 + 31, IsArea = false, Description = "System", Icon = "fa fa-cog", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 32 },
                new NavigationMenu { AreaCode = 0x000059D8 + 31, IsArea = false, Description = "G/L Account", Icon = "fa fa-address-book", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 33 },
                new NavigationMenu { AreaCode = 0x000059D8 + 31, IsArea = false, Description = "Posting Period Closing", Icon = "fa fa-calendar-times-o", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 34 },

                new NavigationMenu { AreaCode = 0x000059D8 + 31, IsArea = false, Description = "Batch Procedures", Icon = "fa fa-tasks", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 35 },

                new NavigationMenu { AreaCode = 0x000059D8 + 2, IsArea = true, Description = "Recurring Procedures", Icon = "", Code = 0x000059D8 + 36 },
                new NavigationMenu { AreaCode = 0x000059D8 + 36, IsArea = false, Description = "Loan Indefinite Charges", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 37 },
                new NavigationMenu { AreaCode = 0x000059D8 + 36, IsArea = false, Description = "Savings Dynamic Fees", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 38 },
                new NavigationMenu { AreaCode = 0x000059D8 + 36, IsArea = false, Description = "Loan Interest Capitalization", Icon = "fa fa-money", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 39 },
                new NavigationMenu { AreaCode = 0x000059D8 + 36, IsArea = false, Description = "Standing Order Execution", Icon = "fa fa-tasks", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 40 },

                new NavigationMenu { AreaCode = 0x000059D8 + 36, IsArea = true, Description = "Discrepancy Matching", Icon = "", Code = 0x000059D8 + 41 },
                new NavigationMenu { AreaCode = 0x000059D8 + 41, IsArea = false, Description = "Matching Singly", Icon = "fa fa-cog", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 42 },
                new NavigationMenu { AreaCode = 0x000059D8 + 41, IsArea = false, Description = "Matching Jointly", Icon = "fa fa-cogs", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 43 },


                new NavigationMenu { AreaCode = 0x000059D8 + 2, IsArea = true, Description = "Customer Accounts", Icon = "", Code = 0x000059D8 + 44 },
                new NavigationMenu { AreaCode = 0x000059D8 + 44, IsArea = false, Description = "Register", Icon = "fa fa-clipboard", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 45 },
                new NavigationMenu { AreaCode = 0x000059D8 + 44, IsArea = false, Description = "Management", Icon = "fa fa-cogs", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 46 },
                new NavigationMenu { AreaCode = 0x000059D8 + 44, IsArea = false, Description = "Signatories", Icon = "fa fa-users", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 47 },
                new NavigationMenu { AreaCode = 0x000059D8 + 44, IsArea = false, Description = "Cheque Books", Icon = "fa fa-envelope-square", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 48 },
                new NavigationMenu { AreaCode = 0x000059D8 + 44, IsArea = false, Description = "Standing Orders", Icon = "fa fa-tasks", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 49 },
                new NavigationMenu { AreaCode = 0x000059D8 + 44, IsArea = false, Description = "Intra Account Transfer", Icon = "fa fa-exchange", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 50 },
                new NavigationMenu { AreaCode = 0x000059D8 + 44, IsArea = false, Description = "Mobile To Bank", Icon = "fa fa-exchange", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 51 },
                new NavigationMenu { AreaCode = 0x000059D8 + 44, IsArea = false, Description = "E-Statements", Icon = "fa fa-envelope-o", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 52 },

                new NavigationMenu { AreaCode = 0x000059D8 + 2, IsArea = true, Description = "Alternate Channels", Icon = "", Code = 0x000059D8 + 53 },
                new NavigationMenu { AreaCode = 0x000059D8 + 53, IsArea = false, Description = "Register", Icon = "fa fa-clipboard", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 54 },
                new NavigationMenu { AreaCode = 0x000059D8 + 53, IsArea = false, Description = "Management", Icon = "fa fa-cogs", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 55 },

                new NavigationMenu { AreaCode = 0x000059D8 + 53, IsArea = true, Description = "Bank Reconciliation", Icon = "", Code = 0x000059D8 + 56 },
                new NavigationMenu { AreaCode = 0x000059D8 + 56, IsArea = false, Description = "Periods", Icon = "fa fa-calendar-o", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 57 },
                new NavigationMenu { AreaCode = 0x000059D8 + 56, IsArea = false, Description = "Processing", Icon = "fa fa-cogs", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 58 },
                new NavigationMenu { AreaCode = 0x000059D8 + 56, IsArea = false, Description = "Closing", Icon = "fa fa-calendar-times-o", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 59 },
                new NavigationMenu { AreaCode = 0x000059D8 + 56, IsArea = false, Description = "Catalogue", Icon = "fa fa-table", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 60 },

                new NavigationMenu { AreaCode = 0x000059D8 + 2, IsArea = true, Description = "Budget Management", Icon = "", Code = 0x000059D8 + 61 },
                new NavigationMenu { AreaCode = 0x000059D8 + 61, IsArea = false, Description = "Periods", Icon = "fa fa-calendar-o", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 62 },
                new NavigationMenu { AreaCode = 0x000059D8 + 61, IsArea = false, Description = "Appropriation", Icon = "fa fa-table", ControllerName = "Controller", ActionName = "Index", AreaName = "Accounts",
                    Code = 0x000059D8 + 63 },





                 //Registry Moule...
                //new NavigationMenu{Description = "Registry", IsArea = true, Code = 0x00005208},
                //new NavigationMenu { AreaCode = 0x00005208, IsArea = false, Description = "Setup", Icon = "fa fa-bars", Code = 0x00005208 + 1 },
                //new NavigationMenu { AreaCode = 0x00005208, IsArea = false, Description = "Operations", Icon = "fa fa-bars", Code = 0x00005208 + 2 },

                //// Setup
                //new NavigationMenu{AreaCode = 0x00005208 +1 , IsArea = false, Description = "Employers", Icon="fa fa-users", ControllerName="Employer",
                //    ActionName="Index", AreaName = "Registry", Code = 0x00005208 + 3},
                //new NavigationMenu{AreaCode = 0x00005208 + 1, IsArea = false, Description = "Zones", Icon="fa fa-globe", ControllerName="Zone", ActionName="Index",
                //    AreaName = "Registry", Code = 0x00005208 + 4},

                //// Operations
                //new NavigationMenu{AreaCode = 0x00005208 + 2, IsArea = false, Description = "Customers", Icon="fa fa-users", ControllerName="Customer", ActionName="Index",
                //    AreaName = "Registry", Code = 0x00005208 + 5},

                //new NavigationMenu { AreaCode = 0x00005208 + 2, IsArea = true, Description = "File Tracking", Icon = "", Code = 0x00005208 + 6},
                //new NavigationMenu { AreaCode = 0x00005208 + 6, IsArea = true, Description = "Dispatch", Icon = "", Code = 0x00005208 + 7},
                //new NavigationMenu{AreaCode = 0x00005208 + 7, IsArea = false, Description = "Multi-Destination", Icon="fa fa-cog", ControllerName="MultiDestination", ActionName="Create",
                //    AreaName = "Registry", Code = 0x00005208 + 8},
                //new NavigationMenu{AreaCode = 0x00005208 + 7, IsArea = false, Description = "Single-Destination ", Icon="fa fa-cog", ControllerName="SingleDestination ", ActionName="Index",
                //    AreaName = "Registry", Code = 0x00005208 + 9},

                //new NavigationMenu { AreaCode = 0x00005208 + 2, IsArea = false, Description = "Receive", Icon = "fa fa-hand-paper-o", ControllerName = "Receive", ActionName = "Create",
                //    AreaName = "Admin", Code = 0x00005208 + 10 },
                //new NavigationMenu { AreaCode = 0x00005208 + 2, IsArea = false, Description = "Recall", Icon = "fa fa-eye-slash", ControllerName = "Recall", ActionName = "Create",
                //    AreaName = "Admin", Code = 0x00005208 + 11 },
                //new NavigationMenu { AreaCode = 0x00005208 + 2, IsArea = false, Description = "Catalogue", Icon = "fa fa-database", ControllerName = "Catalogue", ActionName = "Create",
                //    AreaName = "Admin", Code = 0x00005208 + 12 },

                //new NavigationMenu{AreaCode = 0x00005208 + 2, IsArea = false, Description = "Membership Termination", Icon="fa fa-male", ControllerName=" ", ActionName="Index",
                //    AreaName = "Registry", Code = 0x00005208 + 13},

                //new NavigationMenu { AreaCode = 0x00005208 + 2, IsArea = true, Description = "Education", Icon = "", Code = 0x00005208 + 14},
                //new NavigationMenu{AreaCode = 0x00005208 + 14, IsArea = false, Description = "Venues", Icon="fa fa-home", ControllerName="", ActionName="Index",
                //    AreaName = "Registry", Code = 0x00005208 + 15},
                //new NavigationMenu{AreaCode = 0x00005208 + 14, IsArea = false, Description = "Registration", Icon="fa fa-clipboard", ControllerName="", ActionName="Index",
                //    AreaName = "Registry", Code = 0x00005208 + 16}


                //// Human Resource...
                //new NavigationMenu{Description = "Human Resource", IsArea = true, Code = 0x000055F0},
                //new NavigationMenu { AreaCode = 0x000055F0, IsArea = false, Description = "Setup", Icon = "fa fa-bars", Code = 0x000055F0 + 1 },
                //new NavigationMenu { AreaCode = 0x000055F0, IsArea = false, Description = "Operations", Icon = "fa fa-bars", Code = 0x000055F0 + 2 },

                //// Setup
                //new NavigationMenu{AreaCode = 0x000055F0 + 1 , IsArea = false, Description = "Departments", Icon="fa fa-briefcase", ControllerName="Employer",
                //    ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 3},
                //new NavigationMenu{AreaCode = 0x000055F0 + 1 , IsArea = false, Description = "Designations", Icon="fa fa-briefcase", ControllerName="Employer",
                //    ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 4},
                //new NavigationMenu{AreaCode = 0x000055F0 + 1 , IsArea = false, Description = "Holidays", Icon="fa fa-tree", ControllerName="Controller",
                //    ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 5},
                //new NavigationMenu{AreaCode = 0x000055F0 + 1 , IsArea = false, Description = "Employee Types", Icon="fa fa-users", ControllerName="Controller",
                //    ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 6},
                
                //// Operations
                //new NavigationMenu{AreaCode = 0x000055F0 + 2 , IsArea = false, Description = "Employees", Icon="fa fa-users", ControllerName="Controller",
                //    ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 7},
                //new NavigationMenu{AreaCode = 0x000055F0 + 2 , IsArea = false, Description = "Roster", Icon="fa fa-table", ControllerName="Controller",
                //    ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 8},
                // new NavigationMenu{AreaCode = 0x000055F0 + 2 , IsArea = false, Description = "Attendace", Icon="fa fa-clock-o", ControllerName="Controller",
                //    ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 9},
                // new NavigationMenu{AreaCode = 0x000055F0 + 2 , IsArea = false, Description = "Leave", Icon="fa fa-suitcase", ControllerName="Controller",
                //    ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 10},
                // new NavigationMenu{AreaCode = 0x000055F0 + 2 , IsArea = false, Description = "Salary", Icon="fa fa-money", ControllerName="Controller",
                //    ActionName="Index", AreaName = "HumanResource", Code = 0x000055F0 + 11},



                 //// Accounts...
                 //new NavigationMenu{Description = "Accounts", IsArea = true, Code = 0x000059D8},
                 //new NavigationMenu { AreaCode = 0x000059D8, IsArea = false, Description = "Setup", Icon = "fa fa-bars", Code = 0x000059D8 + 1 },
                 //new NavigationMenu { AreaCode = 0x000059D8, IsArea = false, Description = "Operations", Icon = "fa fa-bars", Code = 0x000059D8 + 2 },

                 //// Setup
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "G/L Accounts", Icon="fa fa-address-book", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 3},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Levies & Charges", Icon="fa fa-money", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 4},

                 //new NavigationMenu { AreaCode = 0x000059D8 + 1, IsArea = true, Description = "Financial Products", Icon = "", Code = 0x000059D8 + 5},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 5, IsArea = false, Description = "Savings", Icon="fa fa-hourglass-half", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 6},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 5, IsArea = false, Description = "Investments", Icon="fa fa-building", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 7},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 5, IsArea = false, Description = "Loans", Icon="fa fa-handshake-o", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 8},

                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Posting Periods", Icon="fa fa-calendar-check-o", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 9},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Treasuries", Icon="fa fa-users", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 10},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Tellers", Icon="fa fa-users", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 11},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Bank Linkages", Icon="fa fa-university", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 12},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Cheque Types", Icon="fa fa-envelope-open-o", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 13},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Direct Debits", Icon="fa fa-credit-card-alt", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 14},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Credit Types", Icon="fa fa-credit-card-alt", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 15},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Debit Types", Icon="fa fa-credit-card-alt", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 16},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Insurers", Icon="fa fa-building", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 17},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Unpay Reasons", Icon="fa fa-hand-paper-o", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 18},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Fixed Deposit Types", Icon="fa fa-money", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 19},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 1, IsArea = false, Description = "Wire Transfer Types", Icon="fa fa-money", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 20},

                 //// Operations
                 //new NavigationMenu{AreaCode = 0x000059D8 + 2, IsArea = false, Description = "Transactions Journal", Icon="fa fa-exchange", ControllerName="Controller",
                 //    ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 21},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 2, IsArea = false, Description = "Batch Procedures", Icon="fa fa-clock-o", ControllerName="Controller",
                 //    ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 22},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 2, IsArea = false, Description = "Recurring Procedures", Icon="fa fa-recycle", ControllerName="Controller",
                 //    ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 23},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 2, IsArea = false, Description = "Customer Accounts", Icon="fa fa-users", ControllerName="Controller",
                 //    ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 24},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 2, IsArea = false, Description = "Alternate Channels", Icon="fa fa-link", ControllerName="Controller",
                 //    ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 25},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 2, IsArea = false, Description = "Bank Reconciliation", Icon="fa fa-university", ControllerName="Controller",
                 //    ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 26},
                 //new NavigationMenu{AreaCode = 0x000059D8 + 2, IsArea = false, Description = "Budget Management", Icon="fa fa-money", ControllerName="Controller",
                 //    ActionName="Index", AreaName = "Accounts", Code = 0x000059D8 + 27},



                 ////// Front Office...
                 //new NavigationMenu{Description = "Front Office", IsArea = true, Code = 0x000061A8},
                 //new NavigationMenu { AreaCode = 0x000061A8, IsArea = false, Description = "Operations", Icon = "fa fa-bars", Code = 0x000061A8 + 1 },

                 //// Operations
                 //new NavigationMenu{AreaCode = 0x000061A8 + 1, IsArea = false, Description = "Treasury", Icon="fa fa-users", ControllerName="",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 2},
                 //new NavigationMenu { AreaCode = 0x000061A8 + 1, IsArea = true, Description = "Teller", Icon = "", Code = 0x000061A8 + 3},
                 //new NavigationMenu{AreaCode = 0x000061A8 + 3, IsArea = false, Description = "Savings Receipts/Payments", Icon="fa fa-money", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 4},
                 //new NavigationMenu{AreaCode = 0x000061A8 + 3, IsArea = false, Description = "Sundry Receipts/Payments", Icon="fa fa-money", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 5},
                 //new NavigationMenu{AreaCode = 0x000061A8 + 3, IsArea = false, Description = "Customer Receipts", Icon="fa fa-envelope-square", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 6},
                 //new NavigationMenu{AreaCode = 0x000061A8 + 3, IsArea = false, Description = "Cheques Transfer", Icon="fa fa-envelope-open-o", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 7},
                 //new NavigationMenu{AreaCode = 0x000061A8 + 3, IsArea = false, Description = "Cash Transfer", Icon="fa fa-money", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 8},
                 //new NavigationMenu{AreaCode = 0x000061A8 + 3, IsArea = false, Description = "End-Of-Day", Icon="fa fa-clock-o", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 9},

                 //new NavigationMenu{AreaCode = 0x000061A8 + 1, IsArea = false, Description = "Cheques", Icon="fa fa-money", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 10},
                 //new NavigationMenu{AreaCode = 0x000061A8 + 1, IsArea = false, Description = "Fixed Deposits", Icon="fa fa-money", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 11},
                 //new NavigationMenu{AreaCode = 0x000061A8 + 1, IsArea = false, Description = "Expense Payables", Icon="fa fa-money", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 12},
                 //new NavigationMenu{AreaCode = 0x000061A8 + 1, IsArea = false, Description = "Account Closure", Icon="fa fa-money", ControllerName="Controller",
                 //   ActionName="Index", AreaName = "FrontOffice", Code = 0x000061A8 + 13},


                 // Back Office...
                 //new NavigationMenu { Description = "Back Office", IsArea = true, Code = 0x00011170 },
                 //new NavigationMenu { AreaCode = 0x00011170, IsArea = false, Description = "Setup", Icon = "fa fa-bars", Code = 0x00011170 + 1 },
                 //new NavigationMenu { AreaCode = 0x00011170, IsArea = false, Description = "Operations", Icon = "fa fa-bars", Code = 0x00011170 + 2 },

                 //// Setup
                 //new NavigationMenu { AreaCode = 0x00011170 + 1, IsArea = false, Description = "Loan Purposes", Icon = "fa fa-question-circle", ControllerName = "LoanPurpose", ActionName = "Index",
                 //    AreaName = "Loaning", Code = 0x00011170 + 3 },
                 //new NavigationMenu { AreaCode = 0x00011170 + 1, IsArea = false, Description = "Loaning Remarks", Icon = "fa fa-check", ControllerName = "LoaningRemark", ActionName = "Index",
                 //    AreaName = "Loaning", Code = 0x00011170 + 4 },
                 //new NavigationMenu { AreaCode = 0x00011170 + 1, IsArea = false, Description = "Income Adjustments", Icon = "fa fa-bar-chart", ControllerName = "IncomeAdjustments", ActionName = "Index",
                 //    AreaName = "Loaning", Code = 0x00011170 + 5 },

                 //// Operations
                 //new NavigationMenu { AreaCode = 0x00011170 + 2, IsArea = false, Description = "Application", Icon = "fa fa-clipboard", ControllerName = "LoanRegistration",
                 //    ActionName = "Index", AreaName = "Loaning", Code = 0x00011170 + 6 },
                 //new NavigationMenu { AreaCode = 0x00011170 + 2, IsArea = false, Description = "Loaning", Icon = "fa fa-users", ControllerName = "LoanRestructuring",
                 //    ActionName = "Create", AreaName = "Loaning", Code = 0x00011170 + 7 },
                 //new NavigationMenu { AreaCode = 0x00011170 + 2, IsArea = false, Description = "Data Capture", Icon = "fa fa-clipboard", ControllerName = "DataCapture",
                 //    ActionName = "Index", AreaName = "Loaning", Code = 0x00011170 + 8 },
                 //new NavigationMenu { AreaCode = 0x00011170 + 2, IsArea = false, Description = "Back Office Transfer", Icon = "fa fa-exchange", ControllerName = "BackOfficeTransfer",
                 //    ActionName = "Create", AreaName = "Accounts", Code = 0x00011170 + 9 },




                 //// Micro-Credit...
                 //new NavigationMenu{Description = "Micro-Credit", IsArea = true, Code = 0x00007918},
                 //new NavigationMenu{AreaCode = 0x00007918, IsArea = false, Description = "Setup", Icon="fa fa-bars", Code = 0x00007918 + 1},
                 //new NavigationMenu{AreaCode = 0x00007918, IsArea = false, Description = "Operations", Icon="fa fa-bars", Code = 0x00007918 + 2},

                 //// Setup
                 //new NavigationMenu { AreaCode = 0x00007918 + 1, IsArea = false, Description = "Officers", Icon = "fa fa-users", ControllerName = "MicroCreditOfficers",
                 //    ActionName = "Index", AreaName = "MicroCredit", Code = 0x00007918 + 3 },

                 //// Operations
                 //new NavigationMenu { AreaCode = 0x00007918 + 2, IsArea = false, Description = "Groups", Icon = "fa fa-users", ControllerName = "Controller",
                 //    ActionName = "Index", AreaName = "MicroCredit", Code = 0x00007918 + 3 },
                 //new NavigationMenu { AreaCode = 0x00007918 + 2, IsArea = false, Description = "Apportionment", Icon = "fa fa-pie-chart", ControllerName = "Controller",
                 //    ActionName = "Index", AreaName = "MicroCredit", Code = 0x00007918 + 3 },




                 //// Dashboard Module...
                 //new NavigationMenu { Description = "Dashboard Module", IsArea = true, Code = 0x000124F8 }, 

                 //// Member Information Sub-Menus
                 //new NavigationMenu { AreaCode = 0x000124F8, IsArea = false, Description = "Setup", Icon = "fa fa-bars", Code = 0x000124F8 + 1 },
                 //new NavigationMenu { AreaCode = 0x000124F8, IsArea = false, Description = "Operations", Icon = "fa fa-bars", Code = 0x000124F8 + 2 }, 

                 //// Setup 
                 //new NavigationMenu { AreaCode = 0x000124F8 + 1, IsArea = false, Description = "Messaging Groups", Icon = "fa fa-comments-o", ControllerName = "MessagingGroups",
                 //    ActionName = "Index", AreaName = "Dashboard", Code = 0x000124F8 + 3 }, 

                 //// Operations 
                 //new NavigationMenu { AreaCode = 0x000124F8 + 2, IsArea = false, Description = "Loaning", Icon = "fa fa-users", ControllerName = "Applications", ActionName = "Index",
                 //   AreaName = "Dashboard", Code = 0x000124F8 + 4 },
                 //new NavigationMenu { AreaCode = 0x000124F8 + 2, IsArea = false, Description = "Messaging", Icon = "fa fa-comments-o", ControllerName = "TextAlerts", ActionName = "Index",
                 //   AreaName = "Dashboard", Code = 0x000124F8 + 5 },
                 //new NavigationMenu { AreaCode = 0x000124F8 + 2, IsArea = false, Description = "Utilities", Icon = "fa fa-bars", ControllerName = "FinancialPosition", ActionName = "Create",
                 //   AreaName = "Dashboard", Code = 0x000124F8 + 6 },




                 //// Control Module...
                 //new NavigationMenu{Description = "Control Module", IsArea = true, Code = 0x00007530},
                 //new NavigationMenu { AreaCode = 0x00007530, IsArea = false, Description = "Setup", Icon = "fa fa-bars", Code = 0x00007530 + 1 },
                 //new NavigationMenu { AreaCode = 0x00007530, IsArea = false, Description = "Operations", Icon = "fa fa-bars", Code = 0x00007530 + 2 }, 

                 //// Setup
                 //new NavigationMenu { AreaCode = 0x00007530 + 1, IsArea = false, Description = "Suppliers", Icon = "fa fa-users", ControllerName = "Controller", ActionName = "Create",
                 //   AreaName = "Control", Code = 0x00007530 + 3 },
                 //new NavigationMenu { AreaCode = 0x00007530 + 1, IsArea = false, Description = "Asset Types", Icon = "fa fa-car", ControllerName = "Controller", ActionName = "Create",
                 //   AreaName = "Control", Code = 0x00007530 + 4},
                 //new NavigationMenu { AreaCode = 0x00007530 + 1, IsArea = false, Description = "Inventory Categories", Icon = "fa fa-cart-arrow-down", ControllerName = "Controller", ActionName = "Create",
                 //   AreaName = "Control", Code = 0x00007530 + 5 },
                 //new NavigationMenu { AreaCode = 0x00007530 + 1, IsArea = false, Description = "Package Types", Icon = "fa fa-cart-plus", ControllerName = "Controller", ActionName = "Create",
                 //   AreaName = "Control", Code = 0x00007530 + 6 },
                 //new NavigationMenu { AreaCode = 0x00007530 + 1, IsArea = false, Description = "Unit Of Measure", Icon = "fa fa-area-chart", ControllerName = "Controller", ActionName = "Create",
                 //   AreaName = "Control", Code = 0x00007530 + 7 },

                 //// Operations
                 //new NavigationMenu { AreaCode = 0x00007530 + 2, IsArea = true, Description = "Assets", Icon = "", Code = 0x00007530 + 8},
                 //new NavigationMenu { AreaCode = 0x00007530 + 8, IsArea = false, Description = "Catalogue", Icon = "fa fa-table", ControllerName = "Controller", ActionName = "Create",
                 //   AreaName = "Control", Code = 0x00007530 + 9 },
                 //new NavigationMenu { AreaCode = 0x00007530 + 8, IsArea = false, Description = "Depreciation", Icon = "fa fa-arrow-down", ControllerName = "Controller", ActionName = "Create",
                 //   AreaName = "Control", Code = 0x00007530 + 10 },

                 //new NavigationMenu { AreaCode = 0x00007530 + 2, IsArea = true, Description = "Inventory", Icon = "", Code = 0x00007530 + 11},
                 //new NavigationMenu { AreaCode = 0x00007530 + 11, IsArea = false, Description = "Catalogue", Icon = "fa fa-table", ControllerName = "Controller", ActionName = "Create",
                 //   AreaName = "Control", Code = 0x00007530 + 12 },
                 


                 ////Analytics - area 27,000
                 //new NavigationMenu{Description = "Analytics", IsArea = true, Code = 0x00006978},
                 //new NavigationMenu{AreaCode = 0x00006978, IsArea = false, Description = "Analytics", Icon="fa fa-line-chart", ControllerName="Analytic",
                 //    ActionName="Index", AreaName = "Analytics", Code = 0x00006978 + 1}
             

                  /*
                //test - area 62,000
                new NavigationMenu{Description = "Grandfather", IsArea = true, Code = 0xF230},
                //test menu-items (children)
                new NavigationMenu{AreaCode = 0xF230, IsArea = false, Description = "Father1", Icon="fa fa-users", Code = 0xF230 + 1},
                new NavigationMenu{AreaCode = 0xF230, IsArea = false, Description = "Father2", Icon="fa fa-users", Code = 0xF230 + 2},

                new NavigationMenu{AreaCode = 0xF230 + 1, IsArea = false, Description = "Child1_F1", Icon="fa fa-users", ControllerName="Home",
                  ActionName="IndexOne", AreaName = "", Code = 0xF230 + 3},
                new NavigationMenu{AreaCode = 0xF230 + 1, IsArea = false, Description = "Child2_F1", Icon="fa fa-users", ControllerName="Home", 
                  ActionName="IndexTwo", AreaName = "", Code = 0xF230 + 4},

                new NavigationMenu{AreaCode = 0xF230 + 2, IsArea = false, Description = "Child1_F2", Icon="fa fa-users", Code = 0xF230 + 5},
                new NavigationMenu{AreaCode = 0xF230 + 2, IsArea = false, Description = "Child2_F2", Icon="fa fa-users", ControllerName="Home", 
                  ActionName="IndexThree", AreaName = "", Code = 0xF230 + 6},

                new NavigationMenu{AreaCode =  0xF230 + 5, IsArea = false, Description = "Child_Child1_F2", Icon="fa fa-users", Code = 0xF230 + 7},
                new NavigationMenu{AreaCode =  0xF230 + 5, IsArea = false, Description = "Child_Child2_F2", Icon="fa fa-users", ControllerName="Home", 
                  ActionName="IndexFour", AreaName = "", Code = 0xF230 + 8},

                new NavigationMenu{AreaCode =  0xF230 + 7, IsArea = false, Description = "Child_Child1_F2_2", Icon="fa fa-users", ControllerName="Home", 
                  ActionName="IndexFive", AreaName = "", Code = 0xF230 + 9},
            */
            };


            return menus;
        }
    }
}
