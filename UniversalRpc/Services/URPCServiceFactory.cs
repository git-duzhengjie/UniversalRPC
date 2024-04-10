using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPC.Contracts;

namespace UniversalRPC.Services
{

    public class URPCServiceFactory
    {
#if NET6_0_OR_GREATER
        private readonly Dictionary<string, Type> _URPCServiceMap = new();
        public URPCServiceFactory()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && typeof(IURPC).IsAssignableFrom(type))
                    {
                        var interfaces = type.GetInterfaces();
                        var inheritInterface = interfaces.Where(x => x != typeof(IURPC) && x.GetInterfaces().Any(i => i == typeof(IURPC))).FirstOrDefault();
                        if (inheritInterface != null)
                        {
                            if (!_URPCServiceMap.ContainsKey(inheritInterface.FullName ?? ""))
                            {
                                _URPCServiceMap.Add(inheritInterface.FullName ?? "", type);
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
            if (serviceName != null && _URPCServiceMap.TryGetValue(serviceName, out var type))
            {
                return type;
            }
            return null;
        }

        /// <summary>
        /// 获取所有实现了IURPC接口的服务
        /// </summary>
        /// <returns></returns>
        public Type[] GetURPCServiceTypes()
        {
            return _URPCServiceMap.Values.ToArray();
        }
#endif


    }

}
