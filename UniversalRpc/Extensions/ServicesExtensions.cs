﻿using UniversalRPC.Services;
#if NET6_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
#endif
using Newtonsoft.Json;

namespace UniversalRPC.Extensions
{
#if NET6_0_OR_GREATER
    public static class ServicesExtensions
    {

        public static void AddURPCService(this IServiceCollection services,JsonSerializerSettings? jsonSerializerSettings=null)
        {
            URPC.JsonSerializerSettings = jsonSerializerSettings;
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

        public static void AddURPCClient<T>(this IServiceCollection services,string url,JsonSerializerSettings? jsonSerializerSettings =null) where T : class 
        {
            URPC.JsonSerializerSettings=jsonSerializerSettings;
            var URPCClient = new URPCClient<T>(url);
            if (URPCClient.Value != null)
            {
                services.AddSingleton(URPCClient.Value);
            }
        }
    }
#endif
}
