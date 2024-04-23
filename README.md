# UniversalRPC介绍
## 0 背景
通常的GRpc是针对多语言的，在实现上通常需要通过protobuf进行通信，protobuf往往只有单纯的基础值类型，但实际环境中往往会出现
更多的情况，更多的类，在使用上就势必会进行二次转换。UniversalRPC就是要解决这种使用上的问题。
## 1 使用说明
### 1.1 契约定义
契约与普通接口定义一样，只需要继承IURPC接口
### 1.2 服务端实现接口
服务端引用定义的契约接口，并实现该接口
### 1.3 服务端添加RPC服务
- IServiceCollection 执行AddURPCService()扩展方法
- WebApplication 执行UseURPCService()扩展方法
- EndPoint 执行UseURPCService()扩展方法
### 1.4 添加RPC客户端
#### 1.4.1 服务端添加
- IServiceCollection 执行AddURPCClient(url)方法
- 注入契约即可使用
#### 1.4.2 客户端添加
- new URPCClient<契约>()
- URPCClient中的Value即当前契约
## 2 序列化
UniversalRPC默认使用系统自带json序列化，但你也可以指定序列化方式，传入你定义的序列化实例即可。
