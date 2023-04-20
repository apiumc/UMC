using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace UMC.Security
{


    /// <summary>
    /// 用户
    /// </summary>
    public abstract class Identity
    {
        public static Identity Create(string name)
        {
            return new Identition(null, null, name, new string[0]);
        }
        public static Identity Create(string name, string alias)
        {
            return new Identition(null, alias, name, new string[0]);
        }
        public static Identity Create(Guid sn, string name, string alias)
        {

            return new Identition(sn, alias, name, new string[0]);
        }

        public static Identity Create(Identity user, params string[] roles)
        {
            return new Identition(user.Id.Value, user.Alias, user.Name, roles, user.Organizes);
        }
        public static Identity Create(Guid sn, string name, string alias, params string[] roles)
        {
            return new Identition(sn, alias, name, roles);
        }

        public static Identity Create(Guid sn, string name, string alias, string[] roles, string[] organizes)
        {
            return new Identition(sn, alias, name, roles, organizes);
        }
        class Identition : Identity
        {
            public Identition(Guid? sn, string alias, string name, string[] roles, params string[] organizes)
            {
                this.Id = sn;
                this._Alias = alias;
                this._Name = name;
                this._Roles = roles;
                this._Organizes = organizes;
            }
            string _Alias;
            public override string Alias
            {
                get
                {
                    return _Alias;
                }
            }

            string _Name;
            public override string Name
            {
                get { return this._Name; }
            }
            string[] _Organizes;
            string[] _Roles;
            public override string[] Organizes => _Organizes;
            public override string[] Roles => _Roles;

        }

        public virtual bool IsInRole(string role)
        {
            var Roles = this.Roles;
            if (Roles == null)
            {
                return false;
            }
            else
            {
                foreach (var r in Roles)
                {
                    if (String.Equals(r, AccessToken.AdminRole, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        if (String.Equals(role, r, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        public virtual bool IsOrganizeMember(string organizeName)
        {
            var Roles = this.Organizes;
            if (Roles == null)
            {
                return false;
            }
            else
            {
                foreach (var r in Organizes)
                {
                    if (String.Equals(organizeName, r, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        /// <summary>
        /// 用户ID
        /// </summary>
        public virtual Guid? Id
        {
            get;
            protected set;
        }
        /// <summary>
        /// 别名全名
        /// </summary>
        public abstract string Alias
        {
            get;
        }
        /// <summary>
        /// 用户名
        /// </summary>
        public abstract string Name
        {
            get;
        }

        public abstract string[] Roles
        {
            get;
        }

        public abstract string[] Organizes
        {
            get;
        }

        public virtual string AuthenticationType
        {
            get { return "UMC"; }
        }

        public virtual bool IsAuthenticated
        {
            get
            {
                if (String.IsNullOrEmpty(this.Name) == false && String.Equals(this.Name, "?") == false)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

    }

    public class Guest : Identity
    {
        public Guest(Guid? sn)
        {
            this.Id = sn;
        }
        public override string Alias => String.Empty;
        public override string[] Organizes => new string[0];
        public override string[] Roles => new string[0];
        public override string Name => "?";
    }
}
