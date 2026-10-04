using Microsoft.Extensions.DependencyInjection;

namespace OmniFile.Memory
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddMemoryStorage(this IServiceCollection services)
        {
            services.AddSingleton<MemoryStorage>();
            
            return services;
        }
    }
}
