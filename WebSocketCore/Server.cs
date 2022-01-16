using Channel_Native.Enums;
using Channel_Native.Model;
using Channel_SDK;
using Channel_SDK.Model;
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
        public static List<MsgHandler> Clients { get; set; }
        public static List<MsgHandler> CQPDll { get; set; }
        public Server(ushort Port)
        {
            Instance = this;
            port = Port;
            InstanceServer = new(port);
            InstanceServer.AddWebSocketService<MsgHandler>("/channel-native");
            Clients = new List<MsgHandler>();
            CQPDll = new List<MsgHandler>();
            InstanceServer.Start();
        }
        private static Dictionary<int, MessageStateMachine> OrderedMessage = new();
        private static int msgSeq = 0;

        private static Dictionary<int, Message> MessageStore = new();
        private static Dictionary<long, User> UserStore = new();
        public void Broadcast(PluginMessageType type, Message msg)
        {
            msgSeq++;
            MessageStore.Add(msgSeq, msg);
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
            public readonly PluginMessageType type;
            public readonly Message msg;

            public MessageStateMachine(PluginMessageType type, Message msg, List<MsgHandler> clients)
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
                    Send_CallResult(CallResult.Pass);
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
                            Send_CallResult(CallResult.Pass);
                        }
                        else
                        {
                            Next();
                        }
                        break;
                    case CallResult.Block:
                        Helper.OutLog($"阻止消息投递，序号: {index}");
                        RemoveStateMachine(index);
                        Send_CallResult(CallResult.Block);
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
            public bool IsPlugin { get; set; } = true;
            protected override void OnMessage(MessageEventArgs e)
            {
                HandleMessage(this, e.Data);
            }
            private object PluginLock = new();
            protected override void OnOpen()
            {
                lock (PluginLock)
                {
                    if (Clients.Count > 1) this.clientID = Clients.Last().clientID + 1;
                    Emit(PluginMessageType.PluginInfo, new { id = clientID });
                }
            }
            protected override void OnClose(CloseEventArgs e)
            {
                if(IsPlugin)
                {
                    Clients.Remove(this);
                    Helper.OutLog($"插件断开连接, 数量: {Clients.Count}");
                }
                else {
                    CQPDll.Remove(this);
                    //Helper.OutLog($"CQP断开连接, 数量: {CQPDll.Count}");
                }
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
                if (json.ContainsKey("seq"))
                { int msgSeq = (int)json["seq"]; }
                switch ((PluginMessageType)(int)json["type"])
                {
                    case PluginMessageType.PluginInfo:
                        PluginInfo = JsonConvert.DeserializeObject<AppInfo>(json["data"]["msg"].ToString());
                        if(PluginInfo.Id == "FakeCQP")
                        {
                            IsPlugin = false;
                            CQPDll.Add(this);
                            //Helper.OutLog($"CQP已连接, 数量: {CQPDll.Count}");
                        }
                        else
                        {
                            IsPlugin = true;
                            Emit(PluginMessageType.Enable, "");
                            Clients.Add(this);
                            Helper.OutLog($"插件已连接, 数量: {Clients.Count}");
                            //Helper.OutLog($"获取插件信息: id: {clientID} {PluginInfo.Id}[{PluginInfo.Name}] {PluginInfo.Version} - {PluginInfo.Author}");
                        }
                        break;
                    case PluginMessageType.Error:
                        Helper.OutError($"{PluginInfo.Name} 发生错误: {json["data"]["msg"]}");
                        break;
                    case PluginMessageType.FinMessage:
                        OrderedMessage[msgSeq].HandleResult((CallResult)(int)json["data"]["msg"]);
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
                    case PluginMessageType.SendMsg:
                        var sendMsg = json["data"]["msg"];
                        var lastMsg = OrderedMessage.Last().Value.msg;
                        Send_PlainMessage(lastMsg.channel_id, lastMsg.id, sendMsg["text"].ToString(), sendMsg["image"].ToString());
                        break;
                    case PluginMessageType.Log:
                        var log = json["data"]["msg"];
                        switch ((Enums.LogLevel)(int)log["level"])
                        {
                            case Enums.LogLevel.Debug:
                            case Enums.LogLevel.Info:
                            case Enums.LogLevel.InfoSuccess:
                            case Enums.LogLevel.InfoReceive:
                            case Enums.LogLevel.InfoSend:
                                Helper.OutLog(log["content"].ToString(), log["type"].ToString());
                                break;
                            case Enums.LogLevel.Warning:
                            case Enums.LogLevel.Error:
                            case Enums.LogLevel.Fatal:
                                Helper.OutError(log["content"].ToString(), log["type"].ToString());
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
