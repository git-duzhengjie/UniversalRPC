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

        public static void AddURPCService(this IServiceCollection services,ISerialize serialize=null)
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
            services.AddSignalR();
        }

        public static void AddURPCClient<T>(this IServiceCollection services,string url,ISerialize serialize=null,bool isHub=false) where T : class 
        {
            URPC.Serialize=serialize;
            URPC.HubMap[url] = isHub;
            var URPCClient = new URPCClient<T>(url);
            if (URPCClient.Value != null)
            {
                services.AddSingleton(URPCClient.Value);
            }
        }

        public static void AddURPCClients(this IServiceCollection services, string url, ISerialize serialize = null, bool isHub = false)
        {
            URPC.Serialize = serialize;
            URPC.HubMap[url] = isHub;
            var URPCClient = new URPCClients(url);
            var rpcs = URPCClient.GetOrCreate();
            if (rpcs != null)
            {
                foreach( var rpc in rpcs)
                {
                    services.AddSingleton(rpc);
                }
            }
        }
    }
#endif
}
