using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace UMC.Web
{
    /// <summary>
    /// POS路线
    /// </summary>
    public abstract class WebFlow : WebHandler
    {
        class FinishFlow : WebFlow
        {
            public override WebActivity GetFirstActivity()
            {
                return WebActivity.Empty;
            }
        }
        public static readonly WebFlow Empty = new FinishFlow();

        /// <summary>
        /// 第一次获取Activity
        /// </summary>
        /// <returns></returns>
        public abstract WebActivity GetFirstActivity();

    }
}
