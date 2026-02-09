using Application.MainBoundedContext.DTO.AccountsModule;
using Application.MainBoundedContext.DTO.RegistryModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace TestApis.Services
{
    public class CustomerComprehensiveService
    {
        private readonly string _connectionString;
        private readonly CustomerService _customerService;
        private readonly NextOfKinService _nextOfKinService;
        private readonly CustomerAccountService _customerAccountService;

        public CustomerComprehensiveService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SwiftFin_Dev"].ConnectionString;
            _customerService = new CustomerService();
            _nextOfKinService = new NextOfKinService();
            _customerAccountService = new CustomerAccountService();
        }

        public class CustomerFullDetailsDTO
        {
            public CustomerDTO Customer { get; set; }
            public List<NextOfKinDTO> NextOfKins { get; set; }
            public List<CustomerAccountDTO> Accounts { get; set; }
            public PercentageSummaryDTO PercentageSummary { get; set; }
        }

        // Get customer with all related data
        public CustomerFullDetailsDTO GetCustomerFullDetails(Guid customerId)
        {
            var customer = _customerService.GetById(customerId);
            if (customer == null)
                return null;

            var nextOfKins = _nextOfKinService.GetByCustomerId(customerId).ToList();
            var accounts = _customerAccountService.GetByCustomerId(customerId).ToList();
            var percentageSummary = _nextOfKinService.GetPercentageSummary(customerId);

            return new CustomerFullDetailsDTO
            {
                Customer = customer,
                NextOfKins = nextOfKins,
                Accounts = accounts,
                PercentageSummary = percentageSummary
            };
        }

        // Get all customers with their next of kin and accounts (paginated)
        public IEnumerable<CustomerFullDetailsDTO> GetAllCustomersWithDetails(int page = 1, int pageSize = 20)
        {
            var customers = _customerService.GetAll().ToList();
            var result = new List<CustomerFullDetailsDTO>();

            // Apply pagination
            var pagedCustomers = customers.Skip((page - 1) * pageSize).Take(pageSize);

            foreach (var customer in pagedCustomers)
            {
                var nextOfKins = _nextOfKinService.GetByCustomerId(customer.Id).ToList();
                var accounts = _customerAccountService.GetByCustomerId(customer.Id).ToList();
                var percentageSummary = _nextOfKinService.GetPercentageSummary(customer.Id);

                result.Add(new CustomerFullDetailsDTO
                {
                    Customer = customer,
                    NextOfKins = nextOfKins,
                    Accounts = accounts,
                    PercentageSummary = percentageSummary
                });
            }

            return result;
        }

        // Search customers with full details
        public IEnumerable<CustomerFullDetailsDTO> SearchCustomersWithDetails(string searchQuery, int page = 1, int pageSize = 20)
        {
            var customers = _customerService.Search(searchQuery).ToList();
            var result = new List<CustomerFullDetailsDTO>();

            // Apply pagination
            var pagedCustomers = customers.Skip((page - 1) * pageSize).Take(pageSize);

            foreach (var customer in pagedCustomers)
            {
                var nextOfKins = _nextOfKinService.GetByCustomerId(customer.Id).ToList();
                var accounts = _customerAccountService.GetByCustomerId(customer.Id).ToList();
                var percentageSummary = _nextOfKinService.GetPercentageSummary(customer.Id);

                result.Add(new CustomerFullDetailsDTO
                {
                    Customer = customer,
                    NextOfKins = nextOfKins,
                    Accounts = accounts,
                    PercentageSummary = percentageSummary
                });
            }

            return result;
        }

        // Get customers by type with full details
        public IEnumerable<CustomerFullDetailsDTO> GetCustomersByTypeWithDetails(int type, int page = 1, int pageSize = 20)
        {
            var customers = _customerService.GetByType(type).ToList();
            var result = new List<CustomerFullDetailsDTO>();

            // Apply pagination
            var pagedCustomers = customers.Skip((page - 1) * pageSize).Take(pageSize);

            foreach (var customer in pagedCustomers)
            {
                var nextOfKins = _nextOfKinService.GetByCustomerId(customer.Id).ToList();
                var accounts = _customerAccountService.GetByCustomerId(customer.Id).ToList();
                var percentageSummary = _nextOfKinService.GetPercentageSummary(customer.Id);

                result.Add(new CustomerFullDetailsDTO
                {
                    Customer = customer,
                    NextOfKins = nextOfKins,
                    Accounts = accounts,
                    PercentageSummary = percentageSummary
                });
            }

            return result;
        }

        // Get customers by station with full details
        public IEnumerable<CustomerFullDetailsDTO> GetCustomersByStationWithDetails(Guid stationId, int page = 1, int pageSize = 20)
        {
            var customers = _customerService.GetByStationId(stationId).ToList();
            var result = new List<CustomerFullDetailsDTO>();

            // Apply pagination
            var pagedCustomers = customers.Skip((page - 1) * pageSize).Take(pageSize);

            foreach (var customer in pagedCustomers)
            {
                var nextOfKins = _nextOfKinService.GetByCustomerId(customer.Id).ToList();
                var accounts = _customerAccountService.GetByCustomerId(customer.Id).ToList();
                var percentageSummary = _nextOfKinService.GetPercentageSummary(customer.Id);

                result.Add(new CustomerFullDetailsDTO
                {
                    Customer = customer,
                    NextOfKins = nextOfKins,
                    Accounts = accounts,
                    PercentageSummary = percentageSummary
                });
            }

            return result;
        }

        // Get customers by name with full details
        public IEnumerable<CustomerFullDetailsDTO> GetCustomersByNameWithDetails(string name, int page = 1, int pageSize = 20)
        {
            var customers = _customerService.GetByName(name).ToList();
            var result = new List<CustomerFullDetailsDTO>();

            // Apply pagination
            var pagedCustomers = customers.Skip((page - 1) * pageSize).Take(pageSize);

            foreach (var customer in pagedCustomers)
            {
                var nextOfKins = _nextOfKinService.GetByCustomerId(customer.Id).ToList();
                var accounts = _customerAccountService.GetByCustomerId(customer.Id).ToList();
                var percentageSummary = _nextOfKinService.GetPercentageSummary(customer.Id);

                result.Add(new CustomerFullDetailsDTO
                {
                    Customer = customer,
                    NextOfKins = nextOfKins,
                    Accounts = accounts,
                    PercentageSummary = percentageSummary
                });
            }

            return result;
        }

        // Get dashboard summary
        public CustomerDashboardSummaryDTO GetDashboardSummary()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = @"SELECT 
                                COUNT(*) as TotalCustomers,
                                COUNT(CASE WHEN Type = 1 THEN 1 END) as IndividualCustomers,
                                COUNT(CASE WHEN Type = 2 THEN 1 END) as PartnershipCustomers,
                                COUNT(CASE WHEN Type = 3 THEN 1 END) as CorporationCustomers,
                                COUNT(CASE WHEN Type = 4 THEN 1 END) as MicroCreditCustomers,
                                COUNT(CASE WHEN IsLocked = 1 THEN 1 END) as LockedCustomers,
                                COUNT(CASE WHEN IsDefaulter = 1 THEN 1 END) as DefaulterCustomers,
                                (SELECT COUNT(DISTINCT CustomerId) FROM [swiftFin_NextOfKin]) as CustomersWithNextOfKins,
                                (SELECT COUNT(DISTINCT CustomerId) FROM [swiftFin_CustomerAccounts]) as CustomersWithAccounts
                                FROM [swiftFin_Customers]";

                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new CustomerDashboardSummaryDTO
                            {
                                TotalCustomers = Convert.ToInt32(reader["TotalCustomers"]),
                                IndividualCustomers = Convert.ToInt32(reader["IndividualCustomers"]),
                                PartnershipCustomers = Convert.ToInt32(reader["PartnershipCustomers"]),
                                CorporationCustomers = Convert.ToInt32(reader["CorporationCustomers"]),
                                MicroCreditCustomers = Convert.ToInt32(reader["MicroCreditCustomers"]),
                                LockedCustomers = Convert.ToInt32(reader["LockedCustomers"]),
                                DefaulterCustomers = Convert.ToInt32(reader["DefaulterCustomers"]),
                                CustomersWithNextOfKins = Convert.ToInt32(reader["CustomersWithNextOfKins"]),
                                CustomersWithAccounts = Convert.ToInt32(reader["CustomersWithAccounts"])
                            };
                        }
                    }
                }
            }
            return new CustomerDashboardSummaryDTO();
        }
    }

    public class CustomerDashboardSummaryDTO
    {
        public int TotalCustomers { get; set; }
        public int IndividualCustomers { get; set; }
        public int PartnershipCustomers { get; set; }
        public int CorporationCustomers { get; set; }
        public int MicroCreditCustomers { get; set; }
        public int LockedCustomers { get; set; }
        public int DefaulterCustomers { get; set; }
        public int CustomersWithNextOfKins { get; set; }
        public int CustomersWithAccounts { get; set; }

        // Calculated properties
        public int CustomersWithoutNextOfKins => TotalCustomers - CustomersWithNextOfKins;
        public int CustomersWithoutAccounts => TotalCustomers - CustomersWithAccounts;
        public decimal NextOfKinCoveragePercentage => TotalCustomers > 0 ?
            (decimal)CustomersWithNextOfKins / TotalCustomers * 100 : 0;
        public decimal AccountCoveragePercentage => TotalCustomers > 0 ?
            (decimal)CustomersWithAccounts / TotalCustomers * 100 : 0;
    }
}