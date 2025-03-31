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
using System.Linq;
using System;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using UniversalRPC.Serialization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Runtime.CompilerServices;
using UniversalRpc.Abstracts;
using System.Text.Encodings.Web;


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

    class JElementConvert<T>
    {
        public T GetValue1(JsonElement obj)
        {
            return obj.Deserialize<T>();
        }

        public T GetValue2(JsonDocument jValue)
        {
            return jValue.Deserialize<T>();
        }
    }


#if NET6_0_OR_GREATER
    public static class WebApplicationExtensions
    {
        public static Dictionary<string, Type?> ObjectTypeMap = null;
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
                    objects2[i] = GetValue(objects2[i], objects1[i]);
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
        static List<string> GetJsonElementKeys(JsonElement element,bool first=false)
        {
            List<string> keys = new List<string>();
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    keys.Add(property.Name);
                    if (first)
                    {
                        break;
                    }
                }
            }
            if (element.ValueKind == JsonValueKind.Array)
            {
                var array = element.EnumerateArray();
                if (array.Any())
                {
                    return GetJsonElementKeys(array.First());
                }
            }
            return keys;
        }

        private static object? JsonElementToValue(JsonElement value, Type propertyType)
        {
            if (value.ValueKind == JsonValueKind.String && propertyType != typeof(string))
            {
                if (propertyType.IsEnum)
                {
                    return Enum.Parse(propertyType, value.GetString());
                }
                else if (propertyType == typeof(int) || propertyType == typeof(int?))
                {
                    return int.Parse(value.GetString());
                }
                else if (propertyType == typeof(float) || propertyType == typeof(float?))
                {
                    return float.Parse(value.GetString());
                }
                else if (propertyType == typeof(double) || propertyType == typeof(double?))
                {
                    return double.Parse(value.GetString());
                }
            }
            if (propertyType == typeof(string))
            {
                return value.GetString();
            }
            var keys = GetJsonElementKeys(value,true);
            if (keys.Count > 0 && char.IsLower(keys[0][0]))
            {
                return value.Deserialize(propertyType,new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            return value.Deserialize(propertyType);
            
        }
        private static object GetValue(object v, Type type1)
        {
            try
            {
                if (type1.IsEnum)
                {
                    return Enum.ToObject(type1, v);
                }
                if (type1.IsAssignableFrom(v.GetType()))
                {
                    return v;
                }
                return Convert.ChangeType(v, type1);
            }
            catch
            {
                if (type1.IsAbstract)
                {
                    type1= GetObjectType(v);
                }
                var vType=v.GetType();
                if (vType.Assembly.FullName.Contains("NewtonsoftJson"))
                {
                    var type = typeof(JObjectConvert<>).MakeGenericType(type1);
                    var instance = Activator.CreateInstance(type);
                    var method = v.GetType() == typeof(JObject) ? type.GetMethod("GetValue1") : type.GetMethod("GetValue2");
                    return method.Invoke(instance, new object[] { v });
                }
                else if (v is JsonElement je)
                {
                    var r= JsonElementToValue(je, type1);
                    return r;
                }
                throw new Exception($"不支持类型{type1}");
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
            if(v is JsonElement je)
            {
                if(!je.TryGetProperty("ObjectName",out var value))
                {
                    je.TryGetProperty("objectName",out value);
                }
                var objectName = value.ToString();
                return ObjectTypeMap[objectName];
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
                var logger = serviceProvider.GetService<ILogger>();
                bool verify=VerifyRequest(request,logger);
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
                                await context.Response.WriteAsync(URPC.GetSerialize().Serialize(result));
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

        private static bool VerifyRequest(Request request, ILogger logger)
        {
            try
            {
                var str = URPCMethod.GetDecryptString(request.Code);
                var array = str.Split("-");
                var time = DateTime.Parse(array[3]);
                if ((DateTime.UtcNow - time).TotalSeconds > 10)
                {
                    throw new ArgumentException("请求失效");
                }
                if (array[0] != request.ServiceName)
                {
                    return false;
                }
                if (array[1] != request.MethodName)
                {
                    return false;
                }
                if (array[2]!= URPC.GetSerialize().Serialize(request.ParameterTypeNames))
                {
                    return false;
                }
                return true;
            }
            catch(ArgumentException)
            {
                throw;
            }
            catch(Exception ex)
            {
                logger.LogError(ex.ToString());
                return false;
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <returns></returns>
        public static WebApplication UseURPCService(this WebApplication app, string serviceName = "",ISerialize serialize=null)
        {
            URPC.Serialize=serialize;
            serviceName = serviceName.Replace("/", "");
            var prefix = string.IsNullOrEmpty(serviceName) ? "" : $"/{serviceName}";
            app.MapPost($"{prefix}/URPC", async (context) => await ToExcuteURPC(context, app.Services));
            app.MapHub<URPCHub>($"/URPCHub");
            app.MapGet($"{prefix}/URPC/time", async context => await GetTime(context));
            GenerateTypeMap();
            return app;
        }
        /// <summary>
        /// 获取时间
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static async Task GetTime(HttpContext context)
        {
            context.Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
            var str= DateTime.UtcNow.ToString();
            await context.Response.WriteAsync(str);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IEndpointRouteBuilder UseURPCService(this IEndpointRouteBuilder app, string serviceName = "", ISerialize serialize = null)
        {
            URPC.Serialize = serialize;
            serviceName = serviceName.Replace("/", "");
            var prefix = string.IsNullOrEmpty(serviceName) ? "" : $"/{serviceName}";
            app.MapPost($"{prefix}/URPC", async (context) => await ToExcuteURPC(context, app.ServiceProvider));
            app.MapHub<URPCHub>($"/URPCHub");
            app.MapGet($"{prefix}/URPC/time", async context => await GetTime(context));
            GenerateTypeMap();
            return app;
        }
        private static bool IsNotAbstractClass(this Type type, bool publicOnly)
        {
            if (type.IsSpecialName)
                return false;

            if (type.IsClass && !type.IsAbstract)
            {
                if (type.IsDefined(typeof(CompilerGeneratedAttribute),true))
                    return false;

                if (publicOnly)
                    return type.IsPublic || type.IsNestedPublic;

                return true;
            }
            return false;
        }
        public static void GenerateTypeMap()
        {
            if (ObjectTypeMap == null)
            {
                ObjectTypeMap = new Dictionary<string, Type?>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var types = assembly.GetExportedTypes()
                        .Where(x => x.IsNotAbstractClass(true))
                        .ToArray();
                    foreach (var type in types)
                    {
                        var interfaces = type.GetInterfaces();
                        if (interfaces.Contains(typeof(IObject)))
                        {
                            IObject instance;
                            if (type.IsGenericType)
                            {
                                var newType = type.MakeGenericType(typeof(int));
                                instance = Activator.CreateInstance(newType) as IObject;
                            }
                            else
                            {
                                instance = Activator.CreateInstance(type) as IObject;
                            }

                            if (!ObjectTypeMap.ContainsKey(instance.ObjectName))
                            {
                                ObjectTypeMap.Add(instance.ObjectName, type);
                            }
                            else
                            {
                                throw new Exception($"{instance.ObjectName}该对象名已经存在");
                            }
                        }
                    }
                }

            }
        }

    }
#endif
}
