using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPC.Contracts;

namespace UniversalRPC.Services
{

    public class URPCServiceFactory
    {
#if NET6_0_OR_GREATER
        private readonly Dictionary<string, (Type,Type)> uRPCServiceMap = new();
        public URPCServiceFactory()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetExportedTypes())
                {
                    if (type.IsClass && typeof(IURPC).IsAssignableFrom(type))
                    {
                        var interfaces = type.GetInterfaces();
                        var inheritInterface = interfaces.Where(x => x != typeof(IURPC) && x.GetInterfaces().Any(i => i == typeof(IURPC))).FirstOrDefault();
                        if (inheritInterface != null)
                        {
                            if (!uRPCServiceMap.ContainsKey(inheritInterface.FullName ?? ""))
                            {
                                uRPCServiceMap.Add(inheritInterface.FullName ?? "", (type,inheritInterface));
                            }
                        }

                    }
                }
            }
        }

        /// <summary>
        /// 根据服务名获取类型
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public Type? GetServiceType(string? serviceName)
        {
            if (serviceName != null && uRPCServiceMap.TryGetValue(serviceName, out var value))
            {
                return value.Item1;
            }
            return null;
        }

        /// <summary>
        /// 获取所有实现了IURPC接口的服务
        /// </summary>
        /// <returns></returns>
        public Type[] GetURPCServiceTypes()
        {
            return uRPCServiceMap.Values.Select(x=>x.Item1).ToArray();
        }

        /// <summary>
        /// 获取所有实现了IURPC接口的接口
        /// </summary>
        /// <returns></returns>
        public Type[] GetURPCServiceITypes()
        {
            return uRPCServiceMap.Values.Select(x=>x.Item2).ToArray();
        }
#endif


    }

}
