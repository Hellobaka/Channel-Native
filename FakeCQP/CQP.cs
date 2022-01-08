using Channel_Native.Enums;
using Channel_Native.Model;
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
            JObject data;
            List<CQCode> cqCodeList = CQCode.Parse(text);

            CQCodeHelper.Progeress(cqCodeList, ref data, ref text);
            string pluginname = appInfos.Find(x => x.AuthCode == authcode).Name;
            int logid = LogHelper.WriteLog(LogLevel.InfoSend, pluginname, "[↑]发送消息", $"群号:{groupid} 消息:{msg.ToString(GB18030)}", "处理中...");
            WebAPI.SendRequest(url, data.ToString());
            sw.Stop();
            LogHelper.UpdateLogStatus(logid, $"√ {sw.ElapsedMilliseconds / (double)1000:f2} s");
            return Save.MsgList.Count + 1;
        }

        [DllExport(ExportName = "CQ_sendPrivateMsg", CallingConvention = CallingConvention.StdCall)]
        public static int CQ_sendPrivateMsg(int authCode, long qqId, IntPtr msg)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string text = msg.ToString(GB18030);
            string url = $@"{Save.url}v1/LuaApiCaller?qq={Save.curentQQ}&funcname=SendMsg";
            JObject data;
            List<CQCode> cqCodeList = CQCode.Parse(text);
            if (text.Contains("[CQ:image"))
            {
                data = new JObject
                {
                    {"toUser",qqId},
                    {"sendToType",1},
                    {"groupid",0},
                    {"fileMd5","" },
                    {"atUser",0 }
                };
            }
            else
            {
                url += "V2";
                data = new JObject
                {
                    {"ToUserUid",qqId},
                    {"SendToType",1},
                    {"GroupID",0 }
                };
            }
            switch (Helper.GetMsgType(qqId))
            {
                case -1:
                    LogHelper.WriteLog(LogLevel.Warning, "消息无法投递", $"此账号未与 {qqId} 建立任何关系");
                    return 0;
                case 1:
                    break;
                case 3:
                    if (data.ContainsKey("sendToType"))
                    {
                        data["sendToType"] = 3;
                        data["groupid"] = Helper.GetIDFirstInGroup(qqId);
                    }
                    else
                    {
                        data["SendToType"] = 3;
                        data["GroupID"] = Helper.GetIDFirstInGroup(qqId);
                    }
                    break;
                default:
                    break;
            }
            CQCodeHelper.Progeress(cqCodeList, ref data, ref text);
            string pluginname = appInfos.Find(x => x.AuthCode == authCode).Name;
            if (WebAPI.SendRequest(url, data.ToString()).Contains("-103"))
            {
                LogHelper.WriteLog(LogLevel.Warning, "消息投递失败", "此群禁止临时会话");
                return 0;
            }
            int logid = LogHelper.WriteLog(LogLevel.InfoSend, pluginname, "[↑]发送好友消息", $"QQ:{qqId} 消息:{msg.ToString(GB18030)}", "处理中...");
            sw.Stop();
            LogHelper.UpdateLogStatus(logid, $"√ {sw.ElapsedMilliseconds / (double)1000:f2} s");
            return 0;
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
            LogHelper.WriteLog(LogLevel.Fatal, pluginname, "异常抛出", errorMsg.ToString(GB18030), "");
            //待找到实现方法
            throw new Exception(errorMsg.ToString(GB18030));
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
            string json = msg.ToString(GB18030);
            appInfos.Add(JsonConvert.DeserializeObject<AppInfo>(json));
            return true;
        }
        public static string SendRequest(string url, string data)
        {
            return "";
        }
    }
}
