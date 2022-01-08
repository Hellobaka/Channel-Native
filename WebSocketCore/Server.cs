using Channel_Native.Enums;
using Channel_Native.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;
using static Channel_SDK.Channel;

namespace Channel_Native.WebSocketCore
{
    /// <summary>
    /// 作为插件管理器
    /// </summary>
    public class Server
    {
        public static Server Instance { get; private set; }
        public WebSocketServer InstanceServer { get; set; }
        private ushort port;
        public static List<MsgHandler> Clients;
        public Server(ushort Port)
        {
            Instance = this;
            port = Port;
            InstanceServer = new(port);
            InstanceServer.AddWebSocketService<MsgHandler>("/channel-native");
            Clients = new List<MsgHandler>();
            InstanceServer.Start();
        }
        private static Dictionary<int, MessageStateMachine> OrderedMessage = new();
        private static int msgSeq = 0;
        public static void Broadcast(PluginMessageType type, object msg)
        {
            msgSeq++;
            MessageStateMachine stateMachine = new(type, msg, Clients);
            Helper.OutLog("加入队列");
            OrderedMessage.Add(msgSeq, stateMachine);
            stateMachine.Next();
        }
        private static void RemoveStateMachine(int seq)
        {
            Helper.OutLog($"清除消息队列，序号: {seq}");
            OrderedMessage.Remove(seq);
        }
        public class MessageStateMachine
        {
            public int index = 0;

            readonly List<MsgHandler> clients;
            readonly PluginMessageType type;
            readonly object msg;
            public MessageStateMachine(PluginMessageType type, object msg, List<MsgHandler> clients)
            {
                this.clients = clients;
                this.type = type;
                this.msg = msg;
            }
            public void Next()
            {
                if (index < clients.Count)
                {
                    clients[index].Emit(type, msg);
                    index++;
                }
                else
                {
                    Helper.OutLog("溢出");
                    RemoveStateMachine(index);
                }
            }
            public void HandleResult(CallResult result)
            {
                switch (result)
                {
                    case CallResult.Pass:
                        Helper.OutLog($"投递消息，序号: {index}");
                        if (index == clients.Count)
                        {
                            RemoveStateMachine(index);
                        }
                        else
                        {
                            Next();
                        }
                        break;
                    case CallResult.Block:
                        Helper.OutLog($"阻止消息投递，序号: {index}");
                        RemoveStateMachine(index);
                        return;
                    default:
                        break;
                }
            }
        }
        public class MsgHandler : WebSocketBehavior
        {
            public int clientID = 0;
            public AppInfo PluginInfo { get; set; }
            public bool Loaded { get; set; } = false;
            protected override void OnMessage(MessageEventArgs e)
            {
                HandleMessage(this, e.Data);
            }
            protected override void OnOpen()
            {
                if (Clients.Count > 1) this.clientID = Clients.Last().clientID + 1;
                Clients.Add(this);
                Emit(PluginMessageType.PluginInfo, new { id = clientID });
                Helper.OutLog($"插件已连接, 数量: {Clients.Count}");
            }
            protected override void OnClose(CloseEventArgs e)
            {
                Clients.Remove(this);
                Helper.OutLog($"插件断开连接, 数量: {Clients.Count}");
            }
            protected override void OnError(ErrorEventArgs e)
            {
                Helper.OutLog($"插件连接出错: {e.Exception.Message}");
            }
            public void Emit(PluginMessageType type, object msg)
            {
                Send((new { type, seq = msgSeq, data = msg }).ToJson());
            }
            public void HandleMessage(MsgHandler socket, string Data)
            {
                JObject json = JObject.Parse(Data);
                int msgSeq = ((int)json["seq"]);
                switch ((PluginMessageType)(int)json["type"])
                {
                    case PluginMessageType.PluginInfo:
                        PluginInfo = JsonConvert.DeserializeObject<AppInfo>(json["data"]["msg"].ToString());
                        //Helper.OutLog($"获取插件信息: id: {clientID} {PluginInfo.Id}[{PluginInfo.Name}] {PluginInfo.Version} - {PluginInfo.Author}");
                        Emit(PluginMessageType.Enable, "");
                        break;
                    case PluginMessageType.Error:
                        Helper.OutError($"{PluginInfo.Name} 发生错误: {json["data"]["msg"]}");
                        break;
                    case PluginMessageType.FinMessage:
                        OrderedMessage[msgSeq].HandleResult((CallResult)((int)json["data"]["result"]));
                        break;
                    case PluginMessageType.Enable:
                        if(((int)json["data"]["msg"]) == 1)
                        {
                            Loaded = true;
                            Helper.OutLog($"{PluginInfo.Name} 载入成功");
                        }
                        break;
                    case PluginMessageType.Disable:
                        if (((int)json["data"]["msg"]) == 1)
                        {
                            Loaded = false;
                            Helper.OutLog($"{PluginInfo.Name} 卸载成功");
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
