using Channel_Native.Enums;
using Channel_Native.Model;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Channel_Native
{
    public static class LogHelper
    {
        public static string GetLogFileName()
        {
            var fileinfo = new DirectoryInfo($@"logs\{MainSave.CurentQQ}").GetFiles("*.db");
            string filename = "";
            foreach (var item in fileinfo)
            {
                if (item.Name.StartsWith($"logv2_{DateTime.Now:yyMM}"))
                {
                    filename = item.Name;
                    break;
                }
            }
            return string.IsNullOrWhiteSpace(filename) ? $"logv2_{DateTime.Now:yyMMdd}.db" : filename;
        }
        public static string GetLogFilePath()
        {
            if (Directory.Exists($@"logs\{MainSave.CurentQQ}") is false)
                Directory.CreateDirectory($@"logs\{MainSave.CurentQQ}");
            return Path.Combine(Environment.CurrentDirectory, $@"logs\{MainSave.CurentQQ}", GetLogFileName());
        }
        private static SqlSugarClient GetInstance()
        {
            SqlSugarClient db = new(new ConnectionConfig()
            {
                ConnectionString = $"data source={GetLogFilePath()}",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
            });
            return db;
        }
        public static void CreateDB()
        {
            using (var db = GetInstance())
            {
                string DBPath = GetLogFilePath();
                db.DbMaintenance.CreateDatabase(DBPath);
                db.CodeFirst.InitTables(typeof(Log));
            }
            WriteLog(LogLevel.InfoSuccess, "运行日志", $"日志数据库初始化完毕{DateTime.Now:yyMMdd}。");
        }
        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }
        public static DateTime TimeStamp2DateTime(long Timestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Timestamp);
        }
        public static string GetTimeStampString(long Timestamp)
        {
            DateTime time = TimeStamp2DateTime(Timestamp);
            StringBuilder sb = new StringBuilder();
            //sb.Append($"{(time.AddDays(1).Day == DateTime.Now.Day ? "昨天" : "今天")} ");
            sb.Append($"{time:MM/dd HH:mm:ss}");
            return sb.ToString();
        }
        public static int WriteLog(LogLevel level, string logOrigin, string type, string messages, string status = "")
        {
            Log model = new Log
            {
                detail = messages,
                id = 0,
                source = logOrigin,
                priority = (int)level,
                name = type,
                time = GetTimeStamp(),
                status = status
            };
            return WriteLog(model);
        }
        public static int WriteLog(Log model)
        {
            if (File.Exists(GetLogFilePath()) is false)
            {
                CreateDB();
            }
            if (!string.IsNullOrWhiteSpace(model.detail) && string.IsNullOrWhiteSpace(model.name))
            {
                model.name = "异常捕获";
                model.detail = model.name;
            }
            using (var db = GetInstance())
            {
                var result = db.Ado.UseTran(() =>
                {
                    db.Insertable(model).ExecuteCommand();
                });
                if (result.IsSuccess is false)
                {
                    throw new Exception("执行错误，发生位置 WriteLog " + result.ErrorMessage);
                }
            }
            int logid = GetLastLog().id;
            SendBroadcast(GetLogBroadcastContent(logid));
            return logid;
        }
        private static string GetLogBroadcastContent(int logid)
        {
            JObject json = new JObject
            {
                new JProperty("Type","Log"),
                new JProperty("QQ",MainSave.CurentQQ),
                new JProperty("LogID",logid),
            };
            return json.ToString();
        }
        private static string GetUpdateBroadcastContent(int logid, string msg)
        {
            JObject json = new JObject
            {
                new JProperty("Type","Update"),
                new JProperty("QQ",MainSave.CurentQQ),
                new JProperty("LogID",logid),
                new JProperty("Msg",msg)
            };
            return json.ToString();
        }
        private static void SendBroadcast(string msg)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
               ProtocolType.Udp);
            IPEndPoint iep1 = new IPEndPoint(IPAddress.Broadcast, MainSave.BoardCastPort);//255.255.255.255
            byte[] data = Encoding.UTF8.GetBytes(msg);
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            sock.SendTo(data, iep1);
            sock.Close();
        }
        public static int WriteLog(int level, string logOrigin, string type, string messages, string status = "")
        {
            LogLevel loglevel = (LogLevel)Enum.Parse(typeof(LogLevel), Enum.GetName(typeof(LogLevel), level));
            return WriteLog(loglevel, logOrigin, type, messages, status);
        }
        public static int WriteLog(LogLevel level, string type, string message, string status = "")
        {
            return WriteLog(level, "OPQBot框架", type, message, status);
        }
        /// <summary>
        /// 以info为等级，"OPQBot框架"为来源，"提示"为类型写出一条日志
        /// </summary>
        /// <param name="messages">日志内容</param>
        public static int WriteLog(string messages, string status = "")
        {
            return WriteLog(LogLevel.Info, "OPQBot框架", "提示", messages, status);
        }

        public static Log GetLogByID(int id)
        {
            using (var db = GetInstance())
            {
                var result = db.Ado.UseTran(() =>
                {
                    return db.Queryable<Log>().First(x => x.id == id);
                });
                if (result.IsSuccess)
                {
                    return result.Data;
                }
                else
                {
                    throw new Exception("执行错误，发生位置 GetLogByID " + result.ErrorMessage);
                }
            }
        }
        public static void UpdateLogStatus(int id, string status)
        {
            using (var db = GetInstance())
            {
                var result = db.Ado.UseTran(() =>
                {
                    db.Updateable<Log>().SetColumns(x => x.status == status).Where(x => x.id == id)
                      .ExecuteCommand();
                });
                if (result.IsSuccess)
                {
                    SendBroadcast(GetUpdateBroadcastContent(id, status));
                }
                else
                {
                    throw new Exception("执行错误，发生位置 UpdateLogStatus " + result.ErrorMessage);
                }
            }
        }
        public static List<Log> GetDisplayLogs(int priority)
        {
            using (var db = GetInstance())
            {
                var result = db.Ado.UseTran(() =>
                {
                    var c = db.SqlQueryable<Log>($"select * from log where priority>= {priority} order by id desc limit {MainSave.LogerMaxCount}").ToList();
                    c.Reverse();
                    return c;
                });
                if (result.IsSuccess)
                {
                    return result.Data;
                }
                else
                {
                    throw new Exception("执行错误，发生位置 UpdateLogStatus " + result.ErrorMessage);
                }
            }
        }
        public static Log GetLastLog()
        {
            using (var db = GetInstance())
            {
                var result = db.Ado.UseTran(() =>
                {
                    return db.SqlQueryable<Log>("select * from log order by id desc limit 1").First();
                });
                if (result.IsSuccess)
                {
                    return result.Data;
                }
                else
                {
                    throw new Exception("执行错误，发生位置 GetLastLog " + result.ErrorMessage);
                }
            }
        }
    }
}

