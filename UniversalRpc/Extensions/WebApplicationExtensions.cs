#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
#endif
using UniversalRPC.Model;
using System.IO;
using UniversalRPC.Services;
using System.Threading.Tasks;
using UniversalRPC.Extensions;
using System.Linq;
using System;
using System.Text.Json;


namespace UniversalRPC.Extensions
{
#if NET6_0_OR_GREATER
    public static class WebApplicationExtensions
    {

        private static bool Same(System.Type[] objects1, object[] objects2)
        {
            for (var i = 0; i < objects1.Length; i++)
            {
                if (objects1[i] != objects2[i].GetType())
                {
                    return false;
                }
            }
            return true;
        }

        private async static Task ToExcuteURPC(HttpContext context, IServiceProvider serviceProvider)
        {
            var body = context.Request.Body;
            var read = new StreamReader(body);
            var request = URPC.Serialize.Deserialize<Request>(await read.ReadToEndAsync());
            if (request != null)
            {
                var serviceFactory = serviceProvider.GetService<URPCServiceFactory>();
                var serviceType = serviceFactory.GetServiceType(request.ServiceName);
                if (serviceType != null)
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var service = scope.ServiceProvider.GetService(serviceType);
                        if (request?.MethodName != null)
                        {
                            var method = serviceType.GetMethods().FirstOrDefault(x => x.Name == request.MethodName && x.GetParameters().Length == request.Parameters.Length && Same(x.GetParameters().Select(x=>x.ParameterType).ToArray(), request.Parameters));
                            var result = method?.Invoke(service, request.Parameters);
                            Type retType=null;
                            if (result != null && result.GetType().IsTask(out retType))
                            {
                                var task = (Task)result;
                                await task.ConfigureAwait(false);
                                var resultProperty = task.GetType().GetProperty("Result");
                                result = resultProperty.GetValue(task);
                            }

                            context.Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                            if (result != null&&retType!=null&&retType.Name!="VoidTaskResult")
                            {
                                await context.Response.WriteAsync(URPC.Serialize.Serialize(result));
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                        }
                    }

                }
                else
                {
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                }
            }
            else
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <returns></returns>
        public static WebApplication UseURPCService(this WebApplication app, string serviceName = "")
        {
            var prefix = string.IsNullOrEmpty(serviceName) ? "" : $"/{serviceName}";
            _ = app.MapPost($"{prefix}/URPC", async (context) => await ToExcuteURPC(context, app.Services));
            return app;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IEndpointRouteBuilder UseURPCService(this IEndpointRouteBuilder app, string serviceName = "")
        {
            serviceName = serviceName.Replace("/", "");
            var prefix = string.IsNullOrEmpty(serviceName) ? "" : $"/{serviceName}";
            _ = app.MapPost($"{prefix}/URPC", async (context) => await ToExcuteURPC(context, app.ServiceProvider));
            return app;
        }

    }
#endif
}
