using System;

namespace Channel_Native.Model
{
	/// <summary>
	/// 表示当前插件的一些基本信息的类
	/// </summary>
	[Serializable]
	public class AppInfo
	{
		#region --属性--
		/// <summary>
		/// 获取当前应用的 AppID
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// 获取当前应用的返回码
		/// </summary>
		public int ResultCode { get;  set; }

		/// <summary>
		/// 获取当前应用的 Api 版本
		/// </summary>
		public int ApiVersion { get;  set; }

		/// <summary>
		/// 获取当前应用的名称
		/// </summary>
		public string Name { get;  set; }

		/// <summary>
		/// 获取当前应用的版本号
		/// </summary>
		public string Version { get;  set; }

		/// <summary>
		/// 获取当前应用的顺序版本
		/// </summary>
		public int VersionId { get;  set; }

		/// <summary>
		/// 获取当前应用的作者名
		/// </summary>
		public string Author { get;  set; }

		/// <summary>
		/// 获取当前应用的说明文本
		/// </summary>
		public string Description { get;  set; }

		/// <summary>
		/// 获取当前应用的验证码
		/// </summary>
		public int AuthCode { get;  set; }
        #endregion

        #region --构造函数--
        public AppInfo()
        {

        }
		/// <summary>
		/// 初始化 <see cref="AppInfo"/> 类的新实例
		/// </summary>
		/// <param name="id">当前应用appid</param>
		/// <param name="resCode">返回码</param>
		/// <param name="apiVer">api版本</param>
		/// <param name="name">应用名称</param>
		/// <param name="version">版本号</param>
		/// <param name="versionId">版本id</param>
		/// <param name="author">应用作者</param>
		/// <param name="description">应用说明</param>
		/// <param name="authCode">应用授权码</param>
		public AppInfo(string id, int resCode, int apiVer, string name, string version, int versionId, string author, string description, int authCode)
		{
			this.Id = id;
			this.ResultCode = resCode;
			this.ApiVersion = apiVer;
			this.Name = name;
			this.Version = version;
			this.VersionId = versionId;
			this.Author = author;
			this.Description = description;
			this.AuthCode = authCode;
		}
		#endregion
	}
}
