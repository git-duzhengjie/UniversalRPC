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
    public class ArraySet<T>
    {
        private T[] array;

        public ArraySet(int length)
        {
            array = new T[length];
        }

        public void SetValue(int index, T value)
        {
            array[index] = value;
        }

        public T[] GetValue()
        {
            return array;
        }
    }
#if NET6_0_OR_GREATER
    public static class WebApplicationExtensions
    {

        private static bool Same(Type[] objects1, object[] objects2,string[] objects3)
        {
            for (var i = 0; i < objects1.Length; i++)
            {
                if (objects1[i].IsArray && objects2[i] is not System.Collections.IList)
                {
                    return false;
                }
                if (objects1[i].IsArray && objects2[i] is System.Collections.IList list)
                {
                    var type1 = objects1[i].GetElementType();
                    if (list.Count > 0)
                    {
                        var type = typeof(ArraySet<>).MakeGenericType(type1);
                        var instance = Activator.CreateInstance(type, list.Count);
                        for (var j = 0; j < list.Count; j++)
                        {
                            try
                            {
                                var method = type.GetMethod("SetValue");
                                method?.Invoke(instance, new object[] { j, Convert.ChangeType(list[j], type1) });
                            }
                            catch
                            {
                                return false;
                            }
                        }
                        var method2 = type.GetMethod("GetValue");
                        objects2[i] = method2.Invoke(instance, new object[] { });
                    }
                    return true;
                }
                if (objects1[i].FullName != objects3[i])
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
            var request = URPC.GetSerialize().Deserialize<Request>(await read.ReadToEndAsync());
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
                            var method = serviceType.GetMethods()
                                .FirstOrDefault(x => x.Name == request.MethodName && x.GetParameters().Length == request.Parameters.Length && Same(x.GetParameters().Select(x => x.ParameterType).ToArray(), request.Parameters,request.ParameterTypeNames));
                            var result = method?.Invoke(service, request.Parameters);
                            Type retType = null;
                            if (result != null && result.GetType().IsTask(out retType))
                            {
                                var task = (Task)result;
                                await task.ConfigureAwait(false);
                                var resultProperty = task.GetType().GetProperty("Result");
                                result = resultProperty.GetValue(task);
                            }

                            context.Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                            if (result != null && retType != null && retType.Name != "VoidTaskResult")
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
