using UniversalRpc.RPC.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace UniversalRpc.RPC.Extensions
{
    public static class ServicesExtensions
    {
        public static void AddRpcService(this IServiceCollection services,JsonSerializerSettings? jsonSerializerSettings=null)
        {
            RPC.JsonSerializerSettings = jsonSerializerSettings;
            services.AddSingleton<RpcServiceFactory>();
            var serviceFactory=services.BuildServiceProvider().GetService<RpcServiceFactory>();
            var types = serviceFactory?.GetRpcServiceTypes();
            if (types != null)
            {
                foreach ( var type in types)
                {
                    services.AddSingleton(type);
                }
            }
        }

        public static void AddRpcClient<T>(this IServiceCollection services,string url,JsonSerializerSettings? jsonSerializerSettings =null) where T : class 
        {
            RPC.JsonSerializerSettings=jsonSerializerSettings;
            var rpcClient = new RpcClient<T>(url);
            if (rpcClient.Value != null)
            {
                services.AddSingleton(rpcClient.Value);
            }
        }
    }
}
