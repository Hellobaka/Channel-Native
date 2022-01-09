using Channel_Native.Enums;
using Channel_Native.Model;
using Channel_Native.WebSocketCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Channel_Native.FakeCQP
{
#if false
    public class CQP
    {
        static List<AppInfo> appInfos = new List<AppInfo>();
        static Encoding GB18030 = Encoding.GetEncoding("GB18030");

        [DllExport(ExportName = "CQ_sendGroupMsg", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_sendGroupMsg(int authcode, long groupid, IntPtr msg)
        {
            Stopwatch sw = new ();
            sw.Start();
            string text = msg.ToString(GB18030);
            string resultText = "", image = "";
            foreach(var item in CQCode.Parse(text))
            {
                switch (item.Function)
                {
                    case CQFunction.Face:
                        break;
                    case CQFunction.Image:
                        resultText = resultText.Replace(item.ToString(), "");
                        image = item.Items[""];
                        break;
                    case CQFunction.Record:
                        break;
                    case CQFunction.At:
                        resultText = resultText.Replace(item.ToString(), $"<@!{item.Items["qq"]}>");
                        break;
                    default:
                        break;
                }
            }
            Client.Instance.Emit(PluginMessageType.SendMsg, new { text = resultText, image, groupid });
            sw.Stop();
            return 1;
        }

        [DllExport(ExportName = "CQ_sendPrivateMsg", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_sendPrivateMsg(int authCode, long qqId, IntPtr msg)
        {
            Stopwatch sw = new ();
            sw.Start();
            string text = msg.ToString(GB18030);
            string resultText = "", image = "";
            foreach (var item in CQCode.Parse(text))
            {
                switch (item.Function)
                {
                    case CQFunction.Face:
                        break;
                    case CQFunction.Image:
                        resultText = resultText.Replace(item.ToString(), "");
                        image = item.Items[""];
                        break;
                    case CQFunction.Record:
                        break;
                    default:
                        break;
                }
            }
            Client.Instance.Emit(PluginMessageType.SendMsg, new { text = resultText, image, qqId });
            sw.Stop();
            return 1;
        }

        [DllExport(ExportName = "CQ_deleteMsg", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_deleteMsg(int authCode, long msgId) => -1;

        [DllExport(ExportName = "CQ_sendLikeV2", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_sendLikeV2(int authCode, long qqId, int count) => -1;

        [DllExport(ExportName = "CQ_getCookiesV2", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getCookiesV2(int authCode, IntPtr domain) => (IntPtr)0;

        [DllExport(ExportName = "CQ_getRecordV2", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getRecordV2(int authCode, IntPtr file, IntPtr format) => (IntPtr)0;

        [DllExport(ExportName = "CQ_getCsrfToken", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_getCsrfToken(int authCode) => -1;

        [DllExport(ExportName = "CQ_getAppDirectory", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getAppDirectory(int authCode)
        {
            string appid = appInfos.Find(x => x.AuthCode == authCode).Id;
            if (!Directory.Exists("data\\app\\" + appid))
                Directory.CreateDirectory("data\\app\\" + appid);
            string path = "data\\app\\" + appid;
            DirectoryInfo directoryInfo = new(path);
            return Marshal.StringToHGlobalAnsi(directoryInfo.FullName + "\\");
        }

        [DllExport(ExportName = "CQ_getLoginQQ", CallingConvention = CallingConvention.StdCall)]
        public static long CQ_getLoginQQ(int authCode) => MainSave.CurentQQ;

        [DllExport(ExportName = "CQ_getLoginNick", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getLoginNick(int authCode) => (IntPtr)0;

        [DllExport(ExportName = "CQ_setGroupKick", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setGroupKick(int authCode, long groupId, long qqId, bool refuses) => -1;

        [DllExport(ExportName = "CQ_setGroupBan", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setGroupBan(int authCode, long groupId, long qqId, long time) => -1;

        [DllExport(ExportName = "CQ_setGroupAdmin", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setGroupAdmin(int authCode, long groupId, long qqId, bool isSet) => -1;

        [DllExport(ExportName = "CQ_setGroupSpecialTitle", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setGroupSpecialTitle(int authCode, long groupId, long qqId, IntPtr title, long durationTime) => -1;

        [DllExport(ExportName = "CQ_setGroupWholeBan", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setGroupWholeBan(int authCode, long groupId, bool isOpen) => -1;

        [DllExport(ExportName = "CQ_setGroupAnonymousBan", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setGroupAnonymousBan(int authCode, long groupId, IntPtr anonymous, long banTime) => -1;

        [DllExport(ExportName = "CQ_setGroupAnonymous", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setGroupAnonymous(int authCode, long groupId, bool isOpen) => -1;

        [DllExport(ExportName = "CQ_setGroupCard", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setGroupCard(int authCode, long groupId, long qqId, IntPtr newCard) => -1;

        [DllExport(ExportName = "CQ_setGroupLeave", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setGroupLeave(int authCode, long groupId, bool isDisband) => -1;

        [DllExport(ExportName = "CQ_setDiscussLeave", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setDiscussLeave(int authCode, long disscussId) => -1;

        [DllExport(ExportName = "CQ_setFriendAddRequest", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setFriendAddRequest(int authCode, IntPtr identifying, int requestType, IntPtr appendMsg) => -1;

        [DllExport(ExportName = "CQ_setGroupAddRequestV2", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setGroupAddRequestV2(int authCode, IntPtr identifying, int requestType, int responseType, IntPtr appendMsg) => -1;

        [DllExport(ExportName = "CQ_addLog", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_addLog(int authCode, int priority, IntPtr type, IntPtr msg)
        {
            if (authCode == 0)
            {
                LogHelper.WriteLog(priority, "OPQBot框架", type.ToString(GB18030), msg.ToString(GB18030));
            }
            else
            {
                string pluginname = appInfos.Find(x => x.AuthCode == authCode).Name;
                LogHelper.WriteLog(priority, pluginname, type.ToString(GB18030), msg.ToString(GB18030));
            }
            return 0;
        }

        [DllExport(ExportName = "CQ_setFatal", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_setFatal(int authCode, IntPtr errorMsg)
        {
            string pluginname = appInfos.Find(x => x.AuthCode == authCode).Name;
            string msg = errorMsg.ToString(GB18030);
            LogHelper.WriteLog(LogLevel.Fatal, pluginname, "异常抛出", msg, "");
            Client.Instance.Emit(PluginMessageType.Error, msg);
            return 0;
        }

        [DllExport(ExportName = "CQ_getGroupMemberInfoV2", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getGroupMemberInfoV2(int authCode, long groupId, long qqId, bool isCache) => (IntPtr)0;

        [DllExport(ExportName = "CQ_getGroupMemberList", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getGroupMemberList(int authCode, long groupId) => (IntPtr)0;

        [DllExport(ExportName = "CQ_getGroupList", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getGroupList(int authCode) => (IntPtr)0;

        [DllExport(ExportName = "CQ_getStrangerInfo", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getStrangerInfo(int authCode, long qqId, bool notCache) => (IntPtr)0;

        [DllExport(ExportName = "CQ_canSendImage", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_canSendImage(int authCode) => 1;

        [DllExport(ExportName = "CQ_canSendRecord", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_canSendRecord(int authCode) => 0;

        [DllExport(ExportName = "CQ_getImage", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getImage(int authCode, IntPtr file) => (IntPtr)0;

        [DllExport(ExportName = "CQ_getGroupInfo", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getGroupInfo(int authCode, long groupId, bool notCache) => (IntPtr)0;

        [DllExport(ExportName = "CQ_getFriendList", CallingConvention = CallingConvention.StdCall)]
        public static IntPtr CQ_getFriendList(int authCode, bool reserved) => (IntPtr)0;
        [DllExport(ExportName = "cq_start", CallingConvention = CallingConvention.StdCall)]
        public static bool cq_start(IntPtr msg, int authcode)
        {
            try
            {
                JObject json = JObject.Parse(msg.ToString(GB18030));

                appInfos.Add(JsonConvert.DeserializeObject<AppInfo>(json["appinfo"].ToString()));
                if (Client.Instance == null)
                {
                    MainSave.Role = Role.FakeCQP;
                    Client client = new(json["wsurl"].ToString());
                    client.Connect();
                }
                return true;
            }
            catch (Exception ex)
            {
                File.WriteAllText("e.txt", ex.Message);
                return false;
            }
        }
        public static string SendRequest(string url, string data)
        {
            return "";
        }
    }
#endif
}
