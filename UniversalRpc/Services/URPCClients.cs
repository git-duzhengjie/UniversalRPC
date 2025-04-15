using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using UniversalRPC.Contracts;
using UniversalRPC.Extensions;
using System.Net.Http;
using System.Threading.Tasks;
using UniversalRpc.Extensions;
using System.Collections.Concurrent;

namespace UniversalRPC.Services
{
    public class URPCClients
    {
        private string url;
        public static URPCClients Instance;

        public List<Type> Types=new List<Type>();
        public ConcurrentDictionary<string,TimeSpan> TimeSpanMap = new ConcurrentDictionary<string, TimeSpan>();
        public URPCClients(string url) { 
            this.url = url;
            Instance = this;
        }
        private List<IURPC> uRPCs;
        public IURPC[] GetOrCreate()
        {
            if (uRPCs != null)
            {
                return uRPCs.ToArray();
            }
            uRPCs = new List<IURPC>();
            var dataTypes = GetIURPCTypes();
            Types = dataTypes.ToList();
            foreach (var dataType in dataTypes) {
                var instance= (IURPC)CreateType(url, dataType);
                uRPCs.Add(instance);
            }
            return uRPCs.ToArray();
        }

        private static IEnumerable<Type> GetIURPCTypes()
        {
            var assemblies= AppDomain.CurrentDomain.GetAssemblies()
                 .Where(x => x.IsNotOut())
                 .ToArray();
            var types=new List<Type>();
            var exportTypes=new List<Type>();
            foreach (var assembly in assemblies) {
                try
                {
                    var etps = assembly.GetExportedTypes();
                    var tps = etps
                        .Where(x => typeof(IURPC).IsAssignableFrom(x))
                        .Where(x => x.IsInterface)
                        .Where(x => x != typeof(IURPC))
                        .ToArray();
                    types.AddRange(tps);
                    exportTypes.AddRange(etps);
                }
                catch
                {
                    continue;
                }
            }
            return types
                .Where(x=>!exportTypes.Where(e=>!e.IsAbstract).Any(e=>x.IsAssignableFrom(e)))
                .ToArray();
        }

        public static object CreateType(string url, Type type)
        {
            TypeBuilder typeBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("UniversalRPC"),
                    AssemblyBuilderAccess.Run)
                .DefineDynamicModule(type.GetTypeInfo().Module.Name)
                .DefineType(type.FullName, TypeAttributes.NotPublic);
            typeBuilder.AddInterfaceImplementation(type);
            MethodInfo[] methods = type.GetMethods();
            foreach (var m in methods)
            {
                ParameterInfo[] parameter = m.GetParameters();
                Type[] array = parameter.Select(p => p.ParameterType).ToArray();
                bool isVoid = m.ReturnType == typeof(void);

                MethodBuilder mbIm = typeBuilder.DefineMethod(m.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot | MethodAttributes.Virtual |
                    MethodAttributes.Final,
                    m.ReturnType,
                    array);

                ILGenerator il = mbIm.GetILGenerator();
                LocalBuilder localObjects = il.DeclareLocal(typeof(object[]));
                var parameterTypes = new List<string>();
                il.Emit(OpCodes.Ldc_I4, parameter.Length);
                il.Emit(OpCodes.Newarr, typeof(object));
                il.Emit(OpCodes.Stloc, localObjects);
                for (int i = 0; i < parameter.Length; i++)
                {
                    il.Emit(OpCodes.Ldloc, localObjects);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    Type t = array[i];
                    il.Emit(OpCodes.Box, t);
                    il.Emit(OpCodes.Stelem_Ref);
                    parameterTypes.Add(parameter[i].ParameterType.FullName);
                }
                il.Emit(OpCodes.Ldloc, localObjects);
                il.Emit(OpCodes.Ldstr, string.Join(",", parameterTypes));
                il.Emit(OpCodes.Ldstr, type.FullName);
                il.Emit(OpCodes.Ldstr, m.Name);
                il.Emit(OpCodes.Ldstr, url);
                bool isTask = m.ReturnType.IsTask(out var retType);
                isVoid = isVoid || (isTask && retType == null);
                var method = typeof(URPCMethod).GetMethod(GetMethodName(isTask, isVoid));
                if (!isVoid)
                {
                    if (isTask)
                    {
                        method= method.MakeGenericMethod(new Type[] { retType });
                    }
                    else
                    {
                        method= method.MakeGenericMethod(new Type[] { m.ReturnType });
                    }
                    var types = method.GetGenericArguments();
                }
                il.Emit(OpCodes.Call, method);
                il.Emit(OpCodes.Ret);
            }
            var typeInfo = typeBuilder.CreateTypeInfo();
            if (typeInfo != null)
            {
                var instance = Activator.CreateInstance(typeInfo);
                if (instance != null)
                {
                    return instance;
                }
            }
            return null;
        }

        private static string GetMethodName(bool isTask, bool isVoid)
        {
            if (isVoid)
            {
                if (isTask)
                {
                    return "SendVoidMessageAsync";
                }
                else
                {
                    return "SendVoidMessage";
                }
            }
            else
            {
                if (isTask)
                {
                    return "SendMessageAsync";
                }
                else
                {
                    return "SendMessage";
                }
            }
        }
    }
}
