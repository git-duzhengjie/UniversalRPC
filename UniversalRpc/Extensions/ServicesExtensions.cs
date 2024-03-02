using UniversalRpc.Rpc.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace UniversalRpc.Rpc.Extensions
{
    public static class ServicesExtensions
    {
        public static void AddRpcService(this IServiceCollection services,JsonSerializerSettings jsonSerializerSettings)
        {
            Rpc.JsonSerializerSettings = jsonSerializerSettings;
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
            Rpc.JsonSerializerSettings=jsonSerializerSettings;
            var rpcClient = new RpcClient<T>(url);
            if (rpcClient.Value != null)
            {
                services.AddSingleton(rpcClient.Value);
            }
        }
    }
}
