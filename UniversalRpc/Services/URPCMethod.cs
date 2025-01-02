using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UniversalRpc.Attibutes;
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
            };
            var realUrl = GetUrl(url,typeName);
            request.Code = $"{GetEncryptString(typeName, methodName, request.ParameterTypeNames, realUrl)}";
            if (URPC.HubMap[url])
            {
                return SendMessageByHub(request, realUrl + "/URPCHub");
            }
            else
            {
                return SendMessageByHttp(request, realUrl + "/URPC");
            }
        }

        private static string GetUrl(string url, string typeName)
        {
            var type=URPCClients.Instance.Types.FirstOrDefault(x=>x.FullName==typeName);
            Debug.Assert(type!=null);
            var serviceNameAttribute = type.GetCustomAttribute<ServiceNameAttribute>();
            string serviceName;
            if (serviceNameAttribute != null) {
                serviceName = serviceNameAttribute.Name;
            }
            else
            {
                serviceName = typeName.Split('.')[0];
            }
            if (!url.EndsWith(serviceName))
            {
                return url.TrimEnd('/') + "/" + serviceName;
            }
            return url;
        }

        private static object SendMessageByHub(Request request, string url)
        {
            Type returnType = ReturnTypeMap[request.ServiceName][request.MethodName];
            if(returnType.IsTask(out var retType))
            {
                return SendMessageByHubAsync(request, url);
            }
            else
            {
                return SendMessageByHubAsync(request, url).Result;
            }
            
        }

        private static async Task<object> SendMessageByHubAsync(Request request, string url)
        {
            await InitHubAsync(url);
            var result = (string)(await _hubConnectionMap[url].InvokeCoreAsync("GetResultAsync",typeof(string),new object[] {URPC.GetSerialize().Serialize(request)}));
            Type returnType = ReturnTypeMap[request.ServiceName][request.MethodName];
            if (returnType.IsTask(out var retType) && retType == null || retType.Name == "VoidTaskResult")
            {
                return null;
            }
            return DeserializeObject(result, retType);
        }

        private static Dictionary<string,HubConnection> _hubConnectionMap=new Dictionary<string, HubConnection>();
        private static async Task InitHubAsync(string url)
        {
            _hubConnectionMap.TryGetValue(url, out var hubConnection);
            if (hubConnection == null || hubConnection.State != HubConnectionState.Connected)
            {
                if (hubConnection != null)
                {
                    await hubConnection.StopAsync();
                    await hubConnection.DisposeAsync();
                }
                var builder = new HubConnectionBuilder();
                hubConnection = builder
                    .WithUrl(url)
                    .WithAutomaticReconnect()
                    .Build();
                await hubConnection.StartAsync();
                _hubConnectionMap[url] = hubConnection;
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
                Content = new StringContent(URPC.GetSerialize().Serialize(request), Encoding.UTF8, "application/json"),
            };
            Type returnType = ReturnTypeMap[request.ServiceName][request.MethodName];
            if (returnType.IsTask(out var retType))
            {
                return Task.Run(() =>
                {
                    return GetResult(httpClient, req, retType);
                });
            }
            else
            {
                return GetResult(httpClient,req,returnType);
            }
            
        }

        private static object GetResult(HttpClient httpClient, HttpRequestMessage req, Type returnType)
        {
            var response = httpClient.SendAsync(req).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.ToString());
            }

            if (returnType == null)
            {
                return null;
            }
            var result = response.Content.ReadAsStringAsync().Result;
            return DeserializeObject(result, returnType);
        }

        public static string GetEncryptString(string typeName, string methodName, object[] objects, string url)
        {
            var response= new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Get, $"{url}/URPC/time"))
                .Result.Content.ReadAsStringAsync().Result;
            var utcNow = DateTime.Parse(response);
            var str = $"{typeName}-{methodName}-{URPC.GetSerialize().Serialize(objects)}-{utcNow}";
            return Crypt.Encrypt(str);
        }

        public static string GetDecryptString(string code)
        {
            var str = Crypt.Decrypt(code);
            return str;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objects">参数</param>
        /// <param name="typeName">服务名</param>
        /// <param name="methodName">方法名</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static void SendVoidMessage(object[] objects, string parameterTypes,string typeName, string methodName, string url)
        {
            var request = new Model.Request
            {
                ServiceName = typeName,
                MethodName = methodName,
                Parameters = objects,
                ParameterTypeNames= parameterTypes.Split(','),
            };
            var realUrl = GetUrl(url, typeName);
            request.Code = $"{GetEncryptString(typeName, methodName, request.ParameterTypeNames, realUrl)}";
            if (URPC.HubMap[url])
            {
                SendVoidMessageByHub(request,realUrl+"/URPCHub");
            }
            else
            {
                SendVoidMessageByHttp(request,realUrl+"/URPC");
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
            await _hubConnectionMap[url].InvokeAsync("GetResultAsync", URPC.GetSerialize().Serialize(request));
        }

        private static object DeserializeObject(string str, Type returnType)
        {
            var retType = returnType;
            var type = typeof(JsonDeserializeObject<>).MakeGenericType(retType);
            var method = type.GetMethod("DeserializeObject");
            if (method == null)
            {
                return null;
            }
            var result = method.Invoke(null, new object[] { str });
            return result;
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
