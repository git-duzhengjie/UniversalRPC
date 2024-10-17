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
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;


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

    class JObjectConvert<T>
    {
        public T GetValue1(JObject obj)
        {
            return obj.ToObject<T>();
        }

        public T GetValue2(JValue jValue)
        {
            return jValue.ToObject<T>();
        }
    }

    
#if NET6_0_OR_GREATER
    public static class WebApplicationExtensions
    {

        public static bool Same(Type[] objects1, object[] objects2,string[] objects3)
        {
            for (var i = 0; i < objects1.Length; i++)
            {
                if (objects1[i].FullName != objects3[i])
                {
                    return false;
                }
                if (objects1[i].IsArray && objects2[i] is System.Collections.IList list)
                {
                    var type1 = objects1[i].GetElementType();
                    var type = typeof(ArraySet<>).MakeGenericType(type1);
                    var instance = Activator.CreateInstance(type, list.Count);
                    for (var j = 0; j < list.Count; j++)
                    {
                        try
                        {
                            var method = type.GetMethod("SetValue");
                            method?.Invoke(instance, new object[] { j, GetValue(list[j], type1) });
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    var method2 = type.GetMethod("GetValue");
                    objects2[i] = method2.Invoke(instance, new object[] { });
                    continue;
                }
                if (objects2[i]!=null&&objects2[i].GetType().FullName != objects1[i].FullName)
                {
                    if (objects1[i].IsEnum)
                    {
                        objects2[i] = Enum.ToObject(objects1[i], objects2[i]);
                    }
                    else
                    {
                        objects2[i] = Convert.ChangeType(objects2[i], objects1[i]);
                    }
                    
                }
            }
            return true;
        }

        class EmpytArrayConverter<T>
        {
            public T[] GetEmpty()
            {
                return Array.Empty<T>();
            }
        }

        private static object GetEmptyArray(Type type1)
        {
            var type = typeof(EmpytArrayConverter<>).MakeGenericType(type1);
            var instance=Activator.CreateInstance(type);
            var method = type.GetMethod("GetEmpty");
            return method.Invoke(instance, new object[] { });
        }

        private static object GetValue(object v, Type type1)
        {
            try
            {
                return Convert.ChangeType(v, type1);
            }
            catch
            {
                if (type1.IsAbstract)
                {
                    type1= GetObjectType(v);
                }
                var type = typeof(JObjectConvert<>).MakeGenericType(type1);
                var instance = Activator.CreateInstance(type);
                var method = v.GetType() == typeof(JObject) ? type.GetMethod("GetValue1") : type.GetMethod("GetValue2");
                return method.Invoke(instance, new object[] { v });
            }
            
        }

        private static Type GetObjectType(object v)
        {
            if(v is JObject jbj)
            {
                return Type.GetType(jbj["$type"].ToString());
            }
            if(v is JValue jValue)
            {
                return Type.GetType(jValue["$type"].ToString());
            }
            return null;
        }

        private async static Task ToExcuteURPC(HttpContext context, IServiceProvider serviceProvider)
        {
            var body = context.Request.Body;
            var read = new StreamReader(body);
            var str = await read.ReadToEndAsync();
            var request = URPC.GetSerialize().Deserialize<Request>(str);
            if (request != null)
            {
                bool verify=VerifyRequest(request,context.Request.Headers);
                if (!verify)
                {
                    throw new Exception("校验失败");
                }
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

        private static bool VerifyRequest(Request request, IHeaderDictionary headers)
        {
            var code = URPCMethod.GetMd5String(request.ServiceName,request.MethodName,request.ParameterTypeNames);
            return code == request.Code&&headers.TryGetValue("Code",out var key)&&key==URPC.Key;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <returns></returns>
        public static WebApplication UseURPCService(this WebApplication app, string serviceName = "")
        {
            serviceName = serviceName.Replace("/", "");
            var prefix = string.IsNullOrEmpty(serviceName) ? "" : $"/{serviceName}";
            app.MapPost($"{prefix}/URPC", async (context) => await ToExcuteURPC(context, app.Services));
            app.MapHub<URPCHub>($"/URPCHub");
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
            app.MapPost($"{prefix}/URPC", async (context) => await ToExcuteURPC(context, app.ServiceProvider));
            app.MapHub<URPCHub>($"/URPCHub");
            return app;
        }

    }
#endif
}
