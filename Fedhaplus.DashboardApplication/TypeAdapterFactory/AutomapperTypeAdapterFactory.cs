using AutoMapper;
using Infrastructure.Crosscutting.Framework.Adapter;

namespace Fedhaplus.DashboardApplication.TypeAdapterFactory
{
    public class AutomapperTypeAdapterFactory : ITypeAdapterFactory
    {
        public AutomapperTypeAdapterFactory()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<ApplicationProfile>();
            });
        }

        public ITypeAdapter Create()
        {
            return new AutomapperTypeAdapter();
        }
    }
}