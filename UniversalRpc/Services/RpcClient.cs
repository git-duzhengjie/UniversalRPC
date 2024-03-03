
using System.Reflection;
using System.Reflection.Emit;

namespace UniversalRpc.RPC.Services
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RpcClient<T> where T : class
    {
        /// <summary>
        /// 注入的rpc对象
        /// </summary>
        public T? Value;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public RpcClient(string url)
        {
            Value = CreateType(url+"/rpc");
        }
        private static T? CreateType(string url)
        {
            var type = typeof(T);
            TypeBuilder typeBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TestRpc"),
                    AssemblyBuilderAccess.Run)
                .DefineDynamicModule(type.GetTypeInfo().Module.Name)
                .DefineType(type.FullName ?? throw new InvalidOperationException(), TypeAttributes.NotPublic);
            typeBuilder.AddInterfaceImplementation(typeof(T));
            MethodInfo[] methods = type.GetMethods();
            lock (RpcMethod.ReturnTypeMap)
            {
                if (!RpcMethod.ReturnTypeMap.ContainsKey(type.FullName))
                {
                    RpcMethod.ReturnTypeMap.Add(type.FullName, new Dictionary<string, Type>());
                }
            }

            foreach (var m in methods)
            {
                ParameterInfo[] parameter = m.GetParameters();
                Type[] array = parameter.Select(p => p.ParameterType).ToArray();
                bool isVoid = m.ReturnType == typeof(void);
                lock (RpcMethod.ReturnTypeMap)
                {
                    if (!RpcMethod.ReturnTypeMap[type.FullName].ContainsKey(m.Name))
                    {
                        var returnType = m.ReturnType;

                        RpcMethod.ReturnTypeMap[type.FullName].Add(m.Name, returnType);
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
                var parameterNames = new List<string>();
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
                    parameterNames.Add(parameter[i].Name);
                }
                il.Emit(OpCodes.Ldloc, localObjects);
                il.Emit(OpCodes.Ldstr, type.FullName);
                il.Emit(OpCodes.Ldstr, m.Name);
                il.Emit(OpCodes.Ldstr, url);
                var method = typeof(RpcMethod).GetMethod(isVoid? "SendMessageViaHttpVoid": "SendMessageViaHttp",
                                          new Type[] { typeof(object[]), typeof(string), typeof(string), typeof(string) });
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
