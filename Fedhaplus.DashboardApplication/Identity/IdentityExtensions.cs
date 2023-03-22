using DistributedServices.MainBoundedContext.Identity;
using System;
using System.Security.Claims;
using System.Security.Principal;

namespace Fedhaplus.DashboardApplication.Identity
{
    public static class IdentityExtensions
    {
        public static Guid GetCompanyId(this IIdentity identity)
        {
            var claim = ((ClaimsIdentity)identity).FindFirst(ApplicationUserProperties.CompanyId);

            if (claim != null)
            {
                var CompanyId = claim.Value;

                if (!string.IsNullOrWhiteSpace(CompanyId))
                {
                    return new Guid(CompanyId);
                }

                return Guid.Empty;
            }

            return Guid.Empty;
        }

        public static Guid GetCustomerId(this IIdentity identity)
        {
            var claim = ((ClaimsIdentity)identity).FindFirst(ApplicationUserProperties.CustomerId);

            if (claim != null)
            {
                var customerId = claim.Value;

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    return new Guid(customerId);
                }

                return Guid.Empty;
            }

            return Guid.Empty;
        }

        public static string GetFullName(this IIdentity identity)
        {
            var claim = ((ClaimsIdentity)identity).FindFirst(ApplicationUserProperties.FullName);

            if (claim != null)
            {
                var fullName = claim.Value;

                return fullName;
            }

            return string.Empty;
        }
    }
}