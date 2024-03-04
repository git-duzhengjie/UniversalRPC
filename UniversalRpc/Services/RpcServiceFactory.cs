using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPC.RPC.Contracts;

namespace UniversalRPC.RPC.Services
{
#if NET6_0_OR_GREATER
    public class RPCServiceFactory
    {
        private Dictionary<string, Type> _rpcServiceMap = new Dictionary<string, Type>();
        public RPCServiceFactory() {
            var assemblies= AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass&& typeof(IRPC).IsAssignableFrom(type))
                    {
                        var interfaces= type.GetInterfaces();
                        var inheritInterface = interfaces.Where(x => x != typeof(IRPC) && x.GetInterfaces().Any(i=>i==typeof(IRPC))).FirstOrDefault();
                        if (inheritInterface != null)
                        {
                            if (!_rpcServiceMap.ContainsKey(inheritInterface.FullName??""))
                            {
                                _rpcServiceMap.Add(inheritInterface.FullName??"", type);
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
            if(serviceName!=null&& _rpcServiceMap.TryGetValue(serviceName,out var type))
            {
                return type;
            }
            return null;
        }

        /// <summary>
        /// 获取所有实现了IRPC接口的服务
        /// </summary>
        /// <returns></returns>
        public Type[] GetRPCServiceTypes()
        {
            return _rpcServiceMap.Values.ToArray();
        }
    }
#endif
}
