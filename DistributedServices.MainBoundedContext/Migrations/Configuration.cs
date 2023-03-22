namespace DistributedServices.MainBoundedContext.Migrations
{
    using DistributedServices.MainBoundedContext.Identity;
    using Infrastructure.Crosscutting.Framework.Utils;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System.Data.Entity.Migrations;

    internal sealed class Configuration : DbMigrationsConfiguration<DistributedServices.MainBoundedContext.Identity.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            CommandTimeout = 3600;
        }

        protected override void Seed(DistributedServices.MainBoundedContext.Identity.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //

            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));

            if (userManager.FindByName(DefaultSettings.Instance.RootEmail) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = DefaultSettings.Instance.RootEmail,
                    Email = DefaultSettings.Instance.RootEmail,
                    EmailConfirmed = true,
                };

                var identityResult = userManager.Create(user, DefaultSettings.Instance.RootPassword);
            }
        }
    }
}
