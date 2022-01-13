using Channel_Native.Enums;
using Channel_Native.Model;
using Channel_SDK.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WebSocket4Net;

namespace Channel_Native.WebSocketCore
{
    /// <summary>
    /// 作为插件本体
    /// </summary>
    public class Client
    {
        public static Client Instance { get; private set; }
        private string WebSocketURL { get; set; } = "";
        public WebSocket Websocket { get; set; }
        public static bool Connected { get; set; } = false;
        private bool HeartBeatStop { get; set; } = false;

        private static Dictionary<int, Message> MessageStore = new();
        public Client(string url)
        {
            WebSocketURL = url;
            WebSocketInit();
            Instance = this;
        }
        private void WebSocketInit()
        {
            Websocket = new WebSocket(WebSocketURL);
            Websocket.Opened += Websocket_Opened;
            Websocket.Error += Websocket_Error;
            Websocket.Closed += Websocket_Closed;
            Websocket.MessageReceived += Websocket_MessageReceived;
        }
        public void Connect()
        {
            Websocket.Open();
        }
        private void Websocket_Opened(object sender, EventArgs e)
        {
            Connected = true;
            Helper.OutLog("服务端连接成功");
            new Thread(() =>
            {
                while (true)
                {
                    if (HeartBeatStop is false)
                    {
                        Emit(PluginMessageType.HeartBeat, "");
                    }
                    Thread.Sleep(3 * 1000);
                }
            }).Start();
        }
        private void Websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Helper.OutError($"WebSocket连接出错:{e.Exception.Message}");
        }

        private void Websocket_Closed(object sender, EventArgs e)
        {
            Connected = false;
            HeartBeatStop = true;
            Helper.OutError("与服务器断开连接，阻止重新连接请按Ctrl+C");
            Thread.Sleep(1000 * 3);
            Helper.OutLog("尝试重新连接...");
            Websocket.Dispose();
            WebSocketInit();
            Connect();
        }
        static Encoding GB18030 = Encoding.GetEncoding("GB18030");
        private void Websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Helper.OutLog($"来自服务器的消息:{e.Message}");
            JObject json = JObject.Parse(e.Message);
            switch ((PluginMessageType)(int)json["type"])
            {
                case PluginMessageType.PluginInfo:
                    if(MainSave.Role == Role.Plugin)
                    {
                        MainSave.PluginID = (int)json["data"]["id"];
                        Emit(PluginMessageType.PluginInfo, PluginManagment.Instance?.InstancePlugin.appinfo);
                    }
                    else
                    {
                        Emit(PluginMessageType.PluginInfo, new AppInfo() { Id ="FakeCQP" });
                    }
                    break;
                case PluginMessageType.Diconnect:
                    Environment.Exit(1);
                    break;
                case PluginMessageType.Enable:
                    PluginManagment.Instance.CallFunction(FunctionName.Enable);
                    PluginManagment.Instance.CallFunction(FunctionName.StartUp);
                    Emit(PluginMessageType.Enable, 1);
                    break;
                case PluginMessageType.Disable:
                    PluginManagment.Instance.CallFunction(FunctionName.Disable);
                    PluginManagment.Instance.CallFunction(FunctionName.Exit);
                    Emit(PluginMessageType.Disable, 1);
                    break;
                case PluginMessageType.ReceiveMessage:
                    int msgSeq = (int)json["seq"];
                    Message message = json["data"].ToObject<Message>();
                    MessageStore.Add(msgSeq, message);
                    message.content = message.nonATMsg;
                    if(message.attachments != null)
                    {
                        foreach (var item in message.attachments)
                        {
                            if (item.content_type.StartsWith("image"))
                            {
                                if (Directory.Exists("data/image") is false)
                                    Directory.CreateDirectory("data/image");
                                string filename = item.filename[..32];
                                StringBuilder sb = new();
                                sb.AppendLine("[image]");
                                sb.AppendLine($"md5={filename}");
                                sb.AppendLine($"size={item.size}");
                                sb.AppendLine($"url={item.url}");
                                File.WriteAllText($"{filename.ToUpper()}.cqimg", sb.ToString());
                                message.content += $"[CQ:image,file:{item.filename}]";
                            }
                        }
                    }                    
                    message.content = Regex.Replace(message.content, "<@!(\\d*)>", $"[CQ:at,qq=$1]");
                    message.content = Regex.Replace(message.content, "<emoji:(\\d*)>", "[CQ:face,id=$1]");
                    Helper.OutLog($"处理消息: {message.content}");

                    var b = Encoding.UTF8.GetBytes(message.content);
                    string messageParse = GB18030.GetString(Encoding.Convert(Encoding.UTF8, GB18030, b));
                    byte[] messageBytes = GB18030.GetBytes(messageParse + "\0");
                    var messageIntptr = Marshal.AllocHGlobal(messageBytes.Length);
                    Marshal.Copy(messageBytes, 0, messageIntptr, messageBytes.Length);

                    int result = PluginManagment.Instance.CallFunction(FunctionName.GroupMsg, 2, msgSeq, message.channel_id, Convert.ToInt64(message.author.id),
                      "", messageIntptr, 0);
                    Thread.Sleep(1000);
                    Emit(PluginMessageType.FinMessage, result);
                    break;
                default:
                    break;
            }
        }
        public void Emit(PluginMessageType type, object msg)
        {
            Websocket.Send((new { type, data = new {msg, id=MainSave.PluginID } }).ToJson());
        }
    }
}
