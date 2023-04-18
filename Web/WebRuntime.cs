using System;
using System.Collections.Generic;
using System.Text;
using UMC.Data;
using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Specialized;

namespace UMC.Web
{
    class WebRuntime : IDisposable
    {
        public Hashtable Items
        {
            get
            {
                return Client.Items;
            }
        }
        public WebRuntime(WebClient client, String model, String cmd, System.Collections.IDictionary header)
        {
            client._runtime = this;
            this.Client = client;
            this.Request = new WebRequest();
            this.Request.OnInit(client, model, cmd, header);
            this.Response = new WebResponse();
            this.Response.OnInit(client);


            this.Context = new WebContext();
            this.Context.Init(this);
            this.Context.OnInit();

        }
        ~WebRuntime()
        {
            GC.SuppressFinalize(this);
        }
        public WebClient Client
        {
            get;
            set;
        }
        public static bool isScanning()
        {
            return WebRuntime.webFactorys.Count > 0 || WebRuntime.activities.Count > 0 || WebRuntime.flows.Count > 0;
        }
        public static WebContext ProcessRequest(String model, String cmd, System.Collections.IDictionary header, WebClient client)
        {
            WebRuntime runtime = new WebRuntime(client, model, cmd, header);
            runtime.Do();
            return runtime.Context;

        }

        public static List<String> RegisterCls()
        {
            List<String> metas = new List<string>();

            if (WebRuntime.webFactorys.Count > 0)
            {
                foreach (WeightType wt in WebRuntime.webFactorys)
                {
                    var t = wt.Type;
                    metas.Add(t.FullName);
                }
            }
            if (WebRuntime.flows.Count > 0)
            {
                var em = WebRuntime.flows.GetEnumerator();
                while (em.MoveNext())
                {
                    var fs = em.Current.Value;
                    foreach (var wt in fs)
                    {

                        var t = wt.Type;
                        metas.Add(t.FullName);
                    }
                }
            }
            if (WebRuntime.activities.Count > 0)
            {
                var em = WebRuntime.activities.GetEnumerator();
                while (em.MoveNext())
                {
                    var mv = em.Current.Value.GetEnumerator();
                    while (mv.MoveNext())
                    {
                        var t = mv.Current.Value;

                        metas.Add(t.Type.FullName);
                    }
                }
            }


            return metas;

        }


        internal WebFlow CurrentFlow
        {
            get;
            set;
        }
        internal IWebFactory CurrentFlowFactory
        {
            get;
            set;
        }

        public WebContext Context
        {
            get;
            private set;
        }

        public WebActivity CurrentActivity
        {
            get;
            set;
        }
        public static void Register(Func<IWebFactory> t)
        {
            var weight = new WeightFactory(t);

            var tUid = weight.Type.GUID;
            var mpps = weight.Type.GetCustomAttributes(typeof(MappingAttribute), false);
            foreach (var m in mpps)
            {
                var mp = m as MappingAttribute;

                if (webFactorys.Exists(e => e.Type.GUID == tUid) == false)
                {
                    webFactorys.Add(weight);
                }
            }
        }
        public static void Register(Func<WebFlow> t)
        {
            var weight = new WeightWebFlow(t);

            var tUid = weight.Type.GUID;
            var mpps = weight.Type.GetCustomAttributes(typeof(MappingAttribute), false);
            foreach (var m in mpps)
            {
                var mp = m as MappingAttribute;
                if (flows.ContainsKey(mp.Model) == false)
                {
                    flows.Add(mp.Model, new List<WeightType>()); ;
                }
                var list = flows[mp.Model];
                if (list.Exists(e => e.Type.GUID == tUid) == false)
                {
                    list.Add(weight);
                    authKeys[mp.Model] = mp.Auth;

                }
            }

        }
        public static void Register(Func<WebActivity> t)
        {
            var weight = new WeightActivity(t);

            var mpps = weight.Type.GetCustomAttributes(typeof(MappingAttribute), false);
            foreach (var m in mpps)
            {
                var mp = m as MappingAttribute;
                if (String.IsNullOrEmpty(mp.Command) == false && String.IsNullOrEmpty(mp.Model) == false)
                {

                    if (activities.TryGetValue(mp.Model, out var actDic) == false)
                    {
                        actDic = new Dictionary<string, WeightType>();
                        activities.Add(mp.Model, actDic);
                    }
                    if (actDic.TryGetValue(mp.Command, out var cmp))
                    {

                        if (cmp.Weight >= mp.Weight)
                        {
                            continue;
                        }
                    }

                    authKeys[$"{mp.Model}.{mp.Command}"] = mp.Auth;
                    weight.Weight = mp.Weight;
                    activities[mp.Model][mp.Command] = weight;




                }


            }
        }
        public static void Register(Type t)
        {
            if (t == null)
            {
                return;
            }
            var tUid = t.GUID;
            var mpps = t.GetCustomAttributes(typeof(MappingAttribute), false);
            foreach (var m in mpps)
            {
                var mp = m as MappingAttribute;
                if (String.IsNullOrEmpty(mp.Command) == false && String.IsNullOrEmpty(mp.Model) == false)
                {
                    if (typeof(WebActivity).IsAssignableFrom(t))
                    {

                        if (activities.TryGetValue(mp.Model, out var actDic) == false)
                        {
                            actDic = new Dictionary<string, WeightType>();
                            activities.Add(mp.Model, actDic);
                        }
                        if (actDic.TryGetValue(mp.Command, out var cmp))
                        {

                            if (cmp.Weight >= mp.Weight)
                            {
                                continue;
                            }
                        }

                        authKeys[$"{mp.Model}.{mp.Command}"] = mp.Auth;
                        activities[mp.Model][mp.Command] = new WeightType(t);

                    }
                }
                else if (String.IsNullOrEmpty(mp.Model) == false)
                {
                    if (typeof(WebFlow).IsAssignableFrom(t))
                    {
                        if (flows.ContainsKey(mp.Model) == false)
                        {
                            flows.Add(mp.Model, new List<WeightType>()); ;
                        }
                        var list = flows[mp.Model];
                        if (list.Exists(e => e.Type.GUID == tUid) == false)
                        {
                            list.Add(new WeightType(t) { Weight = mp.Weight });
                            authKeys[mp.Model] = mp.Auth;

                        }
                    }

                }
                else if (typeof(IWebFactory).IsAssignableFrom(t))
                {
                    if (webFactorys.Exists(e => e.Type.GUID == tUid) == false)
                    {
                        webFactorys.Add(new WeightType(t) { Weight = mp.Weight });
                    }
                }


            }
        }

        public static void Register(System.Reflection.Assembly assembly)
        {

            var types = assembly.GetTypes();
            foreach (var t in types)
            {
                Register(t);
            }
        }
        public class WeightType
        {

            public WeightType(Type type)
            {
                Type = type;
            }
            public Type Type { get; protected set; }
            public int Weight
            {
                get; set;
            }
            public virtual Object Instance()
            {
                return Reflection.CreateInstance(Type);
            }
        }
        class WeightFactory : WeightType
        {
            Func<IWebFactory> _func;
            public WeightFactory(Func<IWebFactory> type) : base(type().GetType())
            {
                _func = type;
            }
            public override Object Instance()
            {
                return _func();
            }
        }

        class WeightActivity : WeightType
        {
            Func<WebActivity> func;
            public WeightActivity(Func<WebActivity> type) : base(type().GetType())
            {
                func = type;
            }
            public override Object Instance()
            {
                return func();
            }
        }
        class WeightWebFlow : WeightType
        {
            Func<WebFlow> func;
            public WeightWebFlow(Func<WebFlow> type) : base(type().GetType())
            {
                func = type;
            }
            public override Object Instance()
            {
                return func();
            }
        }

        internal static Dictionary<String, WebAuthType> authKeys = new Dictionary<String, WebAuthType>();
        internal static List<WeightType> webFactorys = new List<WeightType>();
        internal static Dictionary<String, List<WeightType>> flows = new Dictionary<string, List<WeightType>>();
        internal static Dictionary<String, Dictionary<String, WeightType>> activities = new Dictionary<String, Dictionary<string, WeightType>>();

        class MappingFLow : WebFlow
        {
            Dictionary<String, WeightType> _actives;
            public MappingFLow(Dictionary<String, WeightType> actives)
            {
                _actives = actives;
            }
            public override WebActivity GetFirstActivity()
            {

                if (_actives.TryGetValue(this.Context.Request.Command, out var t))
                {
                    return t.Instance() as WebActivity;
                }
                else
                {
                    return WebActivity.Empty;
                }
            }
        }
        class MappingActivityFactory : IWebFactory
        {
            WebFlow IWebFactory.GetFlowHandler(string mode)
            {
                if (activities.TryGetValue(mode, out var a))
                {

                    return new MappingFLow(a);

                }
                return WebFlow.Empty;

            }

            void IWebFactory.OnInit(WebContext context)
            {

            }
        }
        class MappingFlowFactory : IWebFactory
        {
            WeightType weightType;
            public MappingFlowFactory(WeightType weightType)
            {
                this.weightType = weightType;
            }
            WebFlow IWebFactory.GetFlowHandler(string mode)
            {
                return weightType.Instance() as WebFlow;
            }

            void IWebFactory.OnInit(WebContext context)
            {

            }
        }

        void DoFactory(List<IWebFactory> factorys)
        {
            foreach (var factory in factorys)
            {
                this.CurrentFlowFactory = factory;

                var flow = factory.GetFlowHandler(this.Request.Model);

                flow.Context = this.Context;

                if (flow != WebFlow.Empty)
                {
                    this.CurrentFlow = flow;

                    if (ProcessActivity(flow, flow.GetFirstActivity()))
                    {
                        break;
                    }
                }
            }
        }

        void Do()
        {
            var factorys = new List<IWebFactory>();

            foreach (var ftype in webFactorys)
            {
                var flowFactory = ftype.Instance() as IWebFactory;
                if (flowFactory != null)
                {
                    flowFactory.OnInit(Context);
                    factorys.Add(flowFactory);
                }
            }


            factorys.Add(new MappingActivityFactory());

            if (flows.TryGetValue(Context.Request.Model, out var _fs))
            {
                foreach (var w in _fs)
                {
                    factorys.Add(new MappingFlowFactory(w));
                }
            }


            try
            {
                DoFactory(factorys);

            }
            catch (UMC.Web.WebAbortException)
            {

            }

        }
        bool ProcessActivity(WebFlow flow, WebActivity active)
        {
            active.Context = this.Context;
            if (active == WebActivity.Empty)
            {
                return false;
            }
            else
            {
                active.Flow = flow;
                this.CurrentActivity = active;
                active.ProcessActivity(this.Request, this.Response);
                return true;
            }
        }
        public WebRequest Request
        {
            get;
            private set;
        }

        public WebResponse Response
        {
            get;
            private set;
        }


        #region IDisposable Members

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
