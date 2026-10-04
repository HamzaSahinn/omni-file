using Microsoft.Extensions.DependencyInjection;

namespace OmniFile.Physical
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPhysicalStorage(this IServiceCollection services, Action<PhysicalStorageOptions> configure)
        {
            var options = new PhysicalStorageOptions();

            configure(options);

            services.AddSingleton(new PhysicalStorage(options));

            return services;
        }
    }
}
