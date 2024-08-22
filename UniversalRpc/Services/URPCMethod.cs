using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UniversalRPC.Extensions;
using UniversalRPC.Model;

namespace UniversalRPC.Services
{
    /// <summary>
    /// URPCMethod 的摘要说明
    /// </summary>
    public class URPCMethod
    {
        public static Dictionary<string, Dictionary<string, Type>> ReturnTypeMap = new Dictionary<string, Dictionary<string, Type>>();


        private static string GetMd5String(string str)
        {
            var md5 = MD5.Create();
            var bytes=Encoding.UTF8.GetBytes(str);
            var result=md5.ComputeHash(bytes);
            return BitConverter.ToString(result);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objects">参数</param>
        /// <param name="typeName">服务名</param>
        /// <param name="methodName">方法名</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static object SendMessage(object[] objects, string parameterTypes, string typeName, string methodName, string url)
        {
            var request = new Model.Request
            {
                ServiceName = typeName,
                MethodName = methodName,
                Parameters = objects,
                ParameterTypeNames = parameterTypes.Split(','),
                Code = $"{GetMd5String(typeName,methodName,objects)}"
            };
            if (URPC.HubMap[url])
            {
                return SendMessageByHub(request,url+"/URPCHub");
            }
            else
            {
                return SendMessageByHttp(request,url+"/URPC");
            }
        }

        private static object SendMessageByHub(Request request, string url)
        {
            return SendMessageByHubAsync(request,url).Result;
        }

        private static async Task<object> SendMessageByHubAsync(Request request, string url)
        {
            await InitHubAsync(url);
            var result = (string)(await _hubConnection.InvokeCoreAsync("GetResultAsync",typeof(string),new object[] {URPC.GetSerialize().Serialize(request)}));
            Type returnType = ReturnTypeMap[request.ServiceName][request.MethodName];
            if (returnType.IsTask(out var retType) && retType == null || retType.Name == "VoidTaskResult")
            {
                return Task.CompletedTask;
            }
            return DeserializeObject(result, returnType);
        }

        private static HubConnection _hubConnection;
        private static async Task InitHubAsync(string url)
        {
            if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
                var builder=new HubConnectionBuilder();
                _hubConnection = builder
                    .WithUrl(url)
                    .Build();
                await _hubConnection.StartAsync();
            }
        }

        private static object SendMessageByHttp(Request request, string url)
        {
            HttpClient httpClient = new HttpClient();
            int version = 2;
#if NET6_0_OR_GREATER
            version=2;
#else
            version = 1;
#endif
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Version = new Version(version, 0),
                Content = new StringContent(URPC.GetSerialize().Serialize(request), Encoding.UTF8, "application/json")
            };
            var response = httpClient.SendAsync(req).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.ToString());
            }
            Type returnType = ReturnTypeMap[request.ServiceName][request.MethodName];
            if (returnType.IsTask(out var retType) && retType == null || retType.Name == "VoidTaskResult")
            {
                return Task.CompletedTask;
            }
            var result = response.Content.ReadAsStringAsync().Result;
            return DeserializeObject(result, returnType);
        }

        public static string GetMd5String(string typeName, string methodName, object[] objects)
        {
            var str = $"{typeName}-{methodName}-{URPC.GetSerialize().Serialize(objects)}-{URPC.Key}";
            return GetMd5String(str);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objects">参数</param>
        /// <param name="typeName">服务名</param>
        /// <param name="methodName">方法名</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static void SendVoidMessage(object[] objects, string typeName, string methodName, string url)
        {
            var request = new Model.Request
            {
                ServiceName = typeName,
                MethodName = methodName,
                Parameters = objects,
            };
            if (URPC.HubMap[url])
            {
                SendVoidMessageByHub(request,url+"/URPCHub");
            }
            else
            {
                SendVoidMessageByHttp(request,url+"/URPC");
            }
            
        }

        private static void SendVoidMessageByHttp(Request request, string url)
        {
            HttpClient httpClient = new HttpClient();
            int version = 2;
#if NET6_0_OR_GREATER
            version = 2;
#else
            version = 1;
#endif
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Version = new Version(version, 0),
                Content = new StringContent(URPC.GetSerialize().Serialize(request), Encoding.UTF8, "application/json")
            };
            var response = httpClient.SendAsync(req).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.ToString());
            }
            return;
        }

        private static void SendVoidMessageByHub(Request request, string url)
        {
            SendVoidMessageByHubAsync(request, url).Wait();
        }

        private static async Task SendVoidMessageByHubAsync(Request request, string url)
        {
            await InitHubAsync(url);
            await _hubConnection.InvokeAsync("GetResultAsync", URPC.GetSerialize().Serialize(request));
        }

        private static object DeserializeObject(string str, Type returnType)
        {
            var retType = returnType;
            bool isTask = false;
            if (returnType.BaseType?.Name == "Task")
            {
                retType = returnType.GetGenericArguments().First();
                isTask = true;
            }
            var type = typeof(JsonDeserializeObject<>).MakeGenericType(retType);
            var method = type.GetMethod("DeserializeObject");
            if (method == null)
            {
                return null;
            }
            var result = method.Invoke(null, new object[] { str });
            if (isTask)
            {
                return Task.FromResult(result);
            }
            else
            {
                return result;
            }
        }
    }
    class JsonDeserializeObject<T>
    {
        public static T DeserializeObject(string str)
        {
            return URPC.GetSerialize().Deserialize<T>(str);
        }
    }
}
