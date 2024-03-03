using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using UniversalRpc.RPC.Model;
using Microsoft.Extensions.DependencyInjection;
using UniversalRpc.RPC.Services;
using Newtonsoft.Json;

namespace UniversalRpc.RPC.Extensions
{
    public class Result<T>
    {
        public static T GetValue(object obj)
        {
            var r = (Task<T>)obj;
            return r.Result;
        }
    }
    public static class WebApplicationExtensions
    {
        private async static Task ToExcuteRpc(HttpContext context, WebApplication app)
        {
            var request = await context.Request.ReadFromJsonAsync<Request>();
            if (request != null)
            {
                var serviceFactory = app.Services.GetService<RPCServiceFactory>();
                var serviceType = serviceFactory?.GetServiceType(request?.ServiceName);
                if (serviceType != null)
                {
                    var service = app.Services.GetService(serviceType);
                    if (request?.MethodName != null)
                    {
                        var method = serviceType.GetMethod(request.MethodName);
                        var result = method?.Invoke(service, request.Parameters);
                        if (result != null && result.GetType()?.BaseType?.Name == "Task")
                        {
                            var genericType = result.GetType().GetGenericArguments()[0];
                            var retType = typeof(Result<>).MakeGenericType(genericType);
                            result = retType?.GetMethod("GetValue")?.Invoke(null, new object[] { result });
                        }

                        context.Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                        if (result != null)
                        {
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(result, RPC.JsonSerializerSettings));
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
        public static WebApplication UseRpcService(this WebApplication app)
        {
            _ = app.MapPost("/rpc", async (context)=>await ToExcuteRpc(context,app));
            _ = app.MapGet("/rpc", async (context) => await ToExcuteRpc(context, app));
            return app;
        }

    }
}
