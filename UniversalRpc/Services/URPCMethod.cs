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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objects">参数</param>
        /// <param name="typeName">服务名</param>
        /// <param name="methodName">方法名</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static T SendMessage<T>(object[] objects, string parameterTypes, string typeName, string methodName, string url)
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
                return SendMessageByHub<T>(request, realUrl + "/URPCHub");
            }
            else
            {
                return SendMessageByHttp<T>(request, realUrl + "/URPC");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objects">参数</param>
        /// <param name="typeName">服务名</param>
        /// <param name="methodName">方法名</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<T> SendMessageAsync<T>(object[] objects, string parameterTypes, string typeName, string methodName, string url)
        {
            var request = new Model.Request
            {
                ServiceName = typeName,
                MethodName = methodName,
                Parameters = objects,
                ParameterTypeNames = parameterTypes.Split(','),
            };
            var realUrl = GetUrl(url, typeName);
            request.Code = $"{GetEncryptString(typeName, methodName, request.ParameterTypeNames, realUrl)}";
            if (URPC.HubMap[url])
            {
                return await SendMessageByHubAsync<T>(request, realUrl + "/URPCHub");
            }
            else
            {
                return await SendMessageByHttpAsync<T>(request, realUrl + "/URPC");
            }
        }

        private static string GetUrl(string url, string typeName)
        {
            var type=URPCClients.Types.FirstOrDefault(x=>x.FullName==typeName);
            Debug.Assert(type!=null);
            string serviceName=type.GetServiceName();
            if (!url.EndsWith(serviceName))
            {
                return url.TrimEnd('/') + "/" + serviceName;
            }
            return url;
        }

        private static T SendMessageByHub<T>(Request request, string url)
        {
            return SendMessageByHubAsync<T>(request, url).Result;
        }

        private static async Task<T> SendMessageByHubAsync<T>(Request request, string url)
        {
            await InitHubAsync(url);
            var result = (string)(await _hubConnectionMap[url].InvokeCoreAsync("GetResultAsync",typeof(string),new object[] {URPC.GetSerialize().Serialize(request)}));
            return DeserializeObject<T>(result);
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

        private static T SendMessageByHttp<T>(Request request, string url)
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
            return GetResult<T>(httpClient, req);

        }
        private static Task<T> SendMessageByHttpAsync<T>(Request request, string url)
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
            return GetResultAsync<T>(httpClient, req);

        }

        private static T GetResult<T>(HttpClient httpClient, HttpRequestMessage req)
        {
            var response = httpClient.SendAsync(req).Result;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception(response.Content.ReadAsStringAsync().Result);
            }
            var result = response.Content.ReadAsStringAsync().Result;
            return DeserializeObject<T>(result);
        }

        private static async Task<T> GetResultAsync<T>(HttpClient httpClient, HttpRequestMessage req)
        {
            var response = await httpClient.SendAsync(req);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var message = await response.Content.ReadAsStringAsync();
                throw new Exception(message);
            }
            var result = await response.Content.ReadAsStringAsync();
            return DeserializeObject<T>(result);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objects">参数</param>
        /// <param name="typeName">服务名</param>
        /// <param name="methodName">方法名</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task SendVoidMessageAsync(object[] objects, string parameterTypes, string typeName, string methodName, string url)
        {
            var request = new Model.Request
            {
                ServiceName = typeName,
                MethodName = methodName,
                Parameters = objects,
                ParameterTypeNames = parameterTypes.Split(','),
            };
            var realUrl = GetUrl(url, typeName);
            request.Code = $"{GetEncryptString(typeName, methodName, request.ParameterTypeNames, realUrl)}";
            if (URPC.HubMap[url])
            {
                await SendVoidMessageByHubAsync(request, realUrl + "/URPCHub");
            }
            else
            {
                await SendVoidMessageByHttpAsync(request, realUrl + "/URPC");
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

        private static async Task SendVoidMessageByHttpAsync(Request request, string url)
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
            var response = await httpClient.SendAsync(req);
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

        private static T DeserializeObject<T>(string str)
        {
            return URPC.GetSerialize().Deserialize<T>(str);
        }
    }
}
