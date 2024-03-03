using UniversalRpc.RPC.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace UniversalRpc.RPC.Extensions
{
    public static class ServicesExtensions
    {
        public static void AddRPCService(this IServiceCollection services,JsonSerializerSettings? jsonSerializerSettings=null)
        {
            RPC.JsonSerializerSettings = jsonSerializerSettings;
            services.AddSingleton<RPCServiceFactory>();
            var serviceFactory=services.BuildServiceProvider().GetService<RPCServiceFactory>();
            var types = serviceFactory?.GetRpcServiceTypes();
            if (types != null)
            {
                foreach ( var type in types)
                {
                    services.AddSingleton(type);
                }
            }
        }

        public static void AddRPCClient<T>(this IServiceCollection services,string url,JsonSerializerSettings? jsonSerializerSettings =null) where T : class 
        {
            RPC.JsonSerializerSettings=jsonSerializerSettings;
            var rpcClient = new RPCClient<T>(url);
            if (rpcClient.Value != null)
            {
                services.AddSingleton(rpcClient.Value);
            }
        }
    }
}
