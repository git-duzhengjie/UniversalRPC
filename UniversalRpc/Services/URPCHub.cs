#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalRPC.Extensions;
using UniversalRPC.Model;

namespace UniversalRPC.Services
{

    public class URPCHub:Hub
    {

        private readonly URPCServiceFactory serviceFactory;
        private readonly IServiceProvider serviceProvider;
        public URPCHub(URPCServiceFactory uRPCServiceFactory,IServiceProvider serviceProvider)
        {
            serviceFactory = uRPCServiceFactory;
            this.serviceProvider = serviceProvider;
        }
        public async Task<string> GetResultAsync(string requestStr)
        {
            var request = URPC.GetSerialize().Deserialize<Request>(requestStr);
            var serviceType = serviceFactory.GetServiceType(request.ServiceName);
            if (serviceType != null)
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var service = scope.ServiceProvider.GetService(serviceType);
                    if (request?.MethodName != null)
                    {
                        var method = serviceType.GetMethods()
                            .FirstOrDefault(x => x.Name == request.MethodName && x.GetParameters().Length == request.Parameters.Length && WebApplicationExtensions.Same(x.GetParameters().Select(x => x.ParameterType).ToArray(), request.Parameters, request.ParameterTypeNames));
                        var result = method?.Invoke(service, request.Parameters);
                        Type retType = null;
                        if (result != null && result.GetType().IsTask(out retType))
                        {
                            var task = (Task)result;
                            await task.ConfigureAwait(false);
                            var resultProperty = task.GetType().GetProperty("Result");
                            result = resultProperty.GetValue(task);
                        }
                        if (result != null && retType != null && retType.Name != "VoidTaskResult")
                        {
                            return URPC.Serialize.Serialize(result);
                        }
                        return null;
                    }
                    else
                    {
                        throw new Exception("请求服务不存在");
                    }
                }

            }
            else
            {
                throw new Exception("请求服务不存在");
            }
        }
    }

}
#endif
