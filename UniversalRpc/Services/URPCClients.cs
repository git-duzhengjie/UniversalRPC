using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using UniversalRPC.Contracts;

namespace UniversalRPC.Services
{
    class URPCClients
    {
        private string url;
        public URPCClients(string url) { 
            this.url = url;
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
            foreach (var dataType in dataTypes) {
                var instance= (IURPC)CreateType(url, dataType);
                uRPCs.Add(instance);
            }
            return uRPCs.ToArray();
        }

        private IEnumerable<Type> GetIURPCTypes()
        {
            var assemblies= AppDomain.CurrentDomain.GetAssemblies();
            var types=new List<Type>();
            foreach (var assembly in assemblies) { 
                var tps=assembly.GetTypes().Where(x=>typeof(IURPC).IsAssignableFrom(x));
                types.AddRange(tps);
            }
            return types;
        }

        public static object CreateType(string url, Type type)
        {
            TypeBuilder typeBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("UniversalRPC"),
                    AssemblyBuilderAccess.Run)
                .DefineDynamicModule(type.GetTypeInfo().Module.Name)
                .DefineType(type.FullName ?? throw new InvalidOperationException(), TypeAttributes.NotPublic);
            typeBuilder.AddInterfaceImplementation(type);
            MethodInfo[] methods = type.GetMethods();
            lock (URPCMethod.ReturnTypeMap)
            {
                if (!URPCMethod.ReturnTypeMap.ContainsKey(type.FullName))
                {
                    URPCMethod.ReturnTypeMap.Add(type.FullName, new Dictionary<string, Type>());
                }
            }

            foreach (var m in methods)
            {
                ParameterInfo[] parameter = m.GetParameters();
                Type[] array = parameter.Select(p => p.ParameterType).ToArray();
                bool isVoid = m.ReturnType == typeof(void);
                lock (URPCMethod.ReturnTypeMap)
                {
                    if (!URPCMethod.ReturnTypeMap[type.FullName].ContainsKey(m.Name))
                    {
                        var returnType = m.ReturnType;

                        URPCMethod.ReturnTypeMap[type.FullName].Add(m.Name, returnType);
                    }
                }

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
                var method = typeof(URPCMethod).GetMethod(isVoid ? "SendVoidMessage" : "SendMessage",
                                          new Type[] { typeof(object[]), typeof(string), typeof(string), typeof(string), typeof(string) });
                il.Emit(OpCodes.Call, method
                                      ?? throw new InvalidOperationException());
                il.Emit(OpCodes.Ret);
                typeBuilder.DefineMethodOverride(mbIm, m);
            }
            var typeInfo = typeBuilder.CreateTypeInfo();
            if (typeInfo != null)
            {
                var instance = Activator.CreateInstance(typeInfo);
                if (instance != null)
                {
                    return (T)instance;
                }
            }
            return null;
        }
    }
}
