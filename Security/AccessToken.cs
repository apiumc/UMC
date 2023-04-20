using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using UMC.Data;
using System.IO;
using UMC.Web;

namespace UMC.Security
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public class AccessToken : UMC.Data.IJSON
    {
        public AccessToken() : this(Guid.Empty)
        {

        }
        public AccessToken(Guid deviceId)
        {
            this.Device = deviceId;
            this.Items = new WebMeta();
            this.Roles = String.Empty;
            this.Organizes = String.Empty;
        }
        /// <summary>
        /// 过期时间，单位为秒，0为不过期
        /// </summary>
        public int Timeout
        {
            get;
            private set;
        }
        public string Username
        {
            get;
            private set;
        }
        /// <summary>
        /// 用户Id
        /// </summary>
        public Guid? UserId
        {
            get;
            private set;
        }
        /// <summary>
        /// 关联的ID
        /// </summary>
        public Guid? Device
        {
            get;
            private set;
        }
        /// <summary>
        /// 角色
        /// </summary>
        public string Roles
        {
            get;
            private set;
        }
        /// <summary>
        /// 最后一次活动时间
        /// </summary>
        public int? ActiveTime
        {
            get;
            protected set;
        }
        public String Organizes
        {

            get;
            private set;
        }
        public String Alias
        {

            get;
            private set;
        }
        [UMC.Data.JSON]
        public Web.WebMeta Items
        {
            get;
            private set;
        }
        /// <summary>
        /// 退出
        /// </summary>
        public AccessToken SignOut()
        {
            return Login(UMC.Security.Identity.Create(this.Device.Value, "?", String.Empty));
            //return this;
        }
        public AccessToken Put(string key, string value)
        {
            if (String.IsNullOrEmpty(key) == false)
            {
                if (String.IsNullOrEmpty(value))
                {
                    this.Items.Remove(key);
                }
                else
                {
                    this.Items[key] = value;
                }
            }
            return this;
        }

        public AccessToken Put(System.Collections.Specialized.NameValueCollection NameValue)
        {
            for (var i = 0; i < NameValue.Count; i++)
            {
                var key = NameValue.GetKey(i);
                if (String.IsNullOrEmpty(key) == false)
                {
                    var value = NameValue.Get(i);
                    if (String.IsNullOrEmpty(value))
                    {
                        this.Items.Remove(key);
                    }
                    else
                    {
                        this.Items[key] = value;
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// 提交修改访问票据
        /// </summary>
        public void Commit(String clientIP, String server)
        {
            this.Commit(null, clientIP, server);
        }
        public void Commit(string deviceType, String clientIP, String server)
        {
            this.Commit(deviceType, false, clientIP, server);
        }

        /// <summary>
        /// 管理员角色
        /// </summary>
        public const String AdminRole = "Administrators";
        /// <summary>
        /// 一般用户角色
        /// </summary>
        public const String UserRole = "Users";
        /// <summary>
        /// 来宾角色
        /// </summary>
        public const String GuestRole = "Guest";

        public virtual void Commit(string deviceType, bool unqiue, String clientIP, String server)
        {
            this.ActiveTime = UMC.Data.Utility.TimeSpan();

        }
        public UMC.Security.Identity Identity()
        {

            int cuttime = UMC.Data.Utility.TimeSpan();
            if (this.Timeout > 0 && ((this.ActiveTime ?? 0) + this.Timeout) <= cuttime)
            {
                this.UserId = this.Device;
                return UMC.Security.Identity.Create(this.Device.Value, "?", Alias);
            }
            if (String.IsNullOrEmpty(this.Username))
            {
                this.UserId = this.Device;

                return UMC.Security.Identity.Create(this.Device.Value, "?", Alias);
            }
            if (this.UserId == this.Device)
            {
                return UMC.Security.Identity.Create(this.UserId ?? this.Device.Value, "?", Alias);
            }
            switch (this.Username)
            {
                case "?":
                    return UMC.Security.Identity.Create(this.UserId ?? this.Device.Value, "?", Alias);
                case "#":
                    if (this.UserId.HasValue)
                    {
                        return UMC.Security.Identity.Create(this.UserId.Value, "#", Alias);
                    }
                    else
                    {
                        return UMC.Security.Identity.Create(this.Device.Value, "?", Alias);
                    }
                default:
                    if (this.UserId.HasValue)
                    {
                        if (String.IsNullOrEmpty(this.Roles))
                        {
                            if (String.IsNullOrEmpty(this.Organizes))
                            {

                                return UMC.Security.Identity.Create(this.UserId.Value, this.Username, Alias);
                            }
                            else
                            {

                                return UMC.Security.Identity.Create(this.UserId.Value, this.Username, Alias, new string[0], this.Organizes.Split(new String[] { "," }, StringSplitOptions.None));
                            }

                        }
                        else
                        {
                            if (String.IsNullOrEmpty(this.Organizes))
                            {

                                return UMC.Security.Identity.Create(this.UserId.Value, this.Username
                                    , Alias, this.Roles.Split(new String[] { "," }, StringSplitOptions.None));
                            }
                            else
                            {

                                return UMC.Security.Identity.Create(this.UserId.Value, this.Username
                                    , Alias, this.Roles.Split(new String[] { "," }, StringSplitOptions.None), this.Organizes.Split(new String[] { "," }, StringSplitOptions.None));
                            }

                        }
                    }
                    else
                    {
                        return UMC.Security.Identity.Create(this.Device.Value, "?", Alias);
                    }
            }
        }
        /// <summary>
        /// 登录，默认30分钟过期
        /// </summary>
        /// <param name="user"></param> 
        /// <returns></returns>
        public AccessToken Login(Identity user)
        {
            Login(user, 1800);
            return this;
        }
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="user">身份</param>
        /// <param name="timeout">过期时间</param>
        /// <returns></returns>
        public AccessToken Login(Identity user, int timeout)
        {
            var auth = this;
            auth.Timeout = timeout;
            auth.Username = user.Name;
            auth.UserId = user.Id;
            auth.ActiveTime = UMC.Data.Utility.TimeSpan();
            auth.Roles = null;
            auth.Organizes = null;
            auth.Alias = user.Alias;
            if (user.Roles != null)
            {
                auth.Roles = String.Join(",", user.Roles);
            }
            if (user.Organizes != null)
            {
                auth.Organizes = String.Join(",", user.Organizes);
            }
            return this;

        }

        public string Get(string key)
        {

            return this.Items[key] as string;

        }
        public bool IsInRole(string role)
        {
            if (String.IsNullOrEmpty(this.Roles))
            {
                return false;
            }
            var roles = $",{this.Roles},";

            if (roles.Contains($",{AdminRole},"))
            {
                return true;
            }
            else if (roles.Contains($",{role},"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void IJSON.Write(TextWriter writer)
        {
            writer.Write("{");

            JSON.Serialize("Device", writer);
            writer.Write(":");
            JSON.Serialize(Device, writer);
            if (Timeout > 0)
            {
                writer.Write(",");
                JSON.Serialize("Timeout", writer);
                writer.Write(":");
                JSON.Serialize(Timeout, writer);

            }
            if (String.IsNullOrEmpty(Username) == false)
            {
                writer.Write(",");
                JSON.Serialize("Username", writer);
                writer.Write(":");
                JSON.Serialize(Username, writer);
            }
            if (this.UserId.HasValue)
            {

                writer.Write(",");
                JSON.Serialize("UserId", writer);
                writer.Write(":");
                JSON.Serialize(UserId, writer);
            }



            if (Roles.Length > 0)
            {
                writer.Write(",");
                JSON.Serialize("Roles", writer);
                writer.Write(":");
                JSON.Serialize(Roles, writer);
            }
            if (ActiveTime > 0)
            {
                writer.Write(",");
                JSON.Serialize("ActiveTime", writer);
                writer.Write(":");
                JSON.Serialize(ActiveTime, writer);
            }
            if (this.Organizes.Length > 0)
            {
                writer.Write(",");
                JSON.Serialize("Organizes", writer);
                writer.Write(":");
                JSON.Serialize(Organizes, writer);
            }
            if (String.IsNullOrEmpty(Alias) == false)
            {
                writer.Write(",");
                JSON.Serialize("Alias", writer);
                writer.Write(":");
                JSON.Serialize(Alias, writer);
            }
            var em = this.Items.GetDictionary().GetEnumerator();
            while (em.MoveNext())
            {
                writer.Write(",");
                JSON.Serialize(em.Key, writer);
                writer.Write(":");
                JSON.Serialize(em.Value, writer);

            }
            writer.Write("}");
        }

        void IJSON.Read(string key, object value)
        {
            switch (key)
            {
                case "Username":
                    this.Username = (value as string) ?? String.Empty;
                    break;
                case "Alias":
                    this.Alias = (value as string) ?? String.Empty;
                    break;
                case "Organizes":
                    this.Organizes = (value as string) ?? String.Empty;
                    break;
                case "Roles":
                    this.Roles = (value as string) ?? String.Empty;
                    break;
                case "ActiveTime":
                    this.ActiveTime = UMC.Data.Utility.IntParse(value as string, 0);
                    break;
                case "Timeout":
                    this.Timeout = UMC.Data.Utility.IntParse(value as string, 0);
                    break;
                case "UserId":
                    this.UserId = UMC.Data.Utility.Guid(value as string);
                    break;
                case "Device":
                    this.Device = UMC.Data.Utility.Guid(value as string);
                    break;
                default:
                    this.Items[key] = value as string;
                    break;
            }
        }
    }
}
