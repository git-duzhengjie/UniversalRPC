using UniversalRpc.RPC.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalRpc.RPC.Services
{
    public class RpcServiceFactory
    {
        private Dictionary<string, Type> _rpcServiceMap = new Dictionary<string, Type>();
        public RpcServiceFactory() {
            var assemblies= AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass&& typeof(IRpc).IsAssignableFrom(type))
                    {
                        var interfaces= type.GetInterfaces();
                        var inheritInterface = interfaces.Where(x => x != typeof(IRpc) && x.GetInterfaces().Any(i=>i==typeof(IRpc))).FirstOrDefault();
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
        /// 获取所有实现了IRpc接口的服务
        /// </summary>
        /// <returns></returns>
        public Type[] GetRpcServiceTypes()
        {
            return _rpcServiceMap.Values.ToArray();
        }
    }
}
