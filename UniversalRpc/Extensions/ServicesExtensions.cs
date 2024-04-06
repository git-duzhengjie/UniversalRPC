using UniversalRPC.Services;
using UniversalRPC.Serialization;

#if NET6_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
#endif
using System.Text.Json;
namespace UniversalRPC.Extensions
{
#if NET6_0_OR_GREATER
    public static class ServicesExtensions
    {

        public static void AddURPCService(this IServiceCollection services,ISerialize serialize)
        {
            URPC.Serialize=serialize;
            services.AddSingleton<URPCServiceFactory>();
            var serviceFactory=services.BuildServiceProvider().GetService<URPCServiceFactory>();
            var types = serviceFactory?.GetURPCServiceTypes();
            if (types != null)
            {
                foreach ( var type in types)
                {
                    services.AddScoped(type);
                }
            }
        }

        public static void AddURPCClient<T>(this IServiceCollection services,string url,ISerialize serialize=null) where T : class 
        {
            URPC.Serialize=serialize;
            var URPCClient = new URPCClient<T>(url);
            if (URPCClient.Value != null)
            {
                services.AddSingleton(URPCClient.Value);
            }
        }
    }
#endif
}
