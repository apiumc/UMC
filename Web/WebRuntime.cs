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
        static bool isScanning()
        {
            return WebRuntime.webFactorys.Count > 0 || WebRuntime.activities.Count > 0 || WebRuntime.flows.Count > 0;
        }
        static WebRuntime()
        {
            //var sb = new StringBuilder();
            //sb.AppendLine("                                                                     ");
            //sb.AppendLine("                                                                     ");
            //sb.AppendLine("    $$         $$           $$$$$$    $$$$$$             $$$$$$$$    ");
            //sb.AppendLine("    $$         $$         $$      $$$$      $$         $$            ");
            //sb.AppendLine("    $$         $$        $$        $$        $$       $$             ");
            //sb.AppendLine("    $$         $$        $$        $$        $$       $$             ");
            //sb.AppendLine("    $$         $$        $$        $$        $$       $$             ");
            //sb.AppendLine("    $$         $$        $$        $$        $$       $$             ");
            //sb.AppendLine("    $$         $$        $$        $$        $$       $$             ");
            //sb.AppendLine("     $$       $$         $$        $$        $$        $$            ");
            //sb.AppendLine("       $$$$$$$           $$        $$        $$          $$$$$$$$    ");
            //sb.AppendLine("                                                                     ");
            //sb.AppendLine("                                                                     ");
            //Console.Write(sb);
            var dics = new System.IO.DirectoryInfo(System.Environment.CurrentDirectory).GetFiles("*.dll", SearchOption.TopDirectoryOnly);

            var last = DateTime.Now;
            foreach (var f in dics)
            {

                if (last > f.LastWriteTime)
                {
                    last = f.LastWriteTime;
                }
            }

            String mapFile = Utility.MapPath("App_Data/register.net");
            var lastTime = Utility.TimeSpan(last);
            String m = Utility.Reader(mapFile);
            if (String.IsNullOrEmpty(m) == false)
            {
                Hashtable map = JSON.Deserialize(m) as Hashtable;
                if (map.ContainsKey("time"))
                {
                    if (Utility.IntParse(map["time"].ToString(), 0) == lastTime)
                    {
                        Array mapings = map["data"] as Array;
                        if (mapings != null)
                        {
                            int l = mapings.Length;
                            for (int i = 0; i < l; i++)
                            {
                                String v = mapings.GetValue(i) as string;
                                WebRuntime.Register(Type.GetType(v));
                            }
                        }
                    }
                }
            }
            if (isScanning() == false)
            {
                Reflection.Instance().ScanningClass();
                Utility.Writer(mapFile, JSON.Serialize(new WebMeta().Put("time", lastTime).Put("data", WebRuntime.RegisterCls())), false);
            }
            WebRuntime.webFactorys.Sort((x, y) => y.Weight.CompareTo(x.Weight));
            var em = WebRuntime.flows.GetEnumerator();
            while (em.MoveNext())
            {
                em.Current.Value.Sort((x, y) => y.Weight.CompareTo(x.Weight));

            }
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
                    metas.Add(t.FullName + "," + t.Assembly.FullName);
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
                        metas.Add(t.FullName + "," + t.Assembly.FullName);
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

                        metas.Add(t.FullName + "," + t.Assembly.FullName);
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
                        if (activities.ContainsKey(mp.Model) == false)
                        {
                            activities.Add(mp.Model, new Dictionary<string, Type>());
                        }
                        var cmd = String.Format("{0}.{1}", mp.Model, mp.Command);
                        var actDic = activities[mp.Model];


                        if (weightKeys.ContainsKey(cmd))
                        {

                            if (weightKeys[cmd] >= mp.Weight)
                            {
                                continue;
                            }
                        }
                        if (mp.Category > 0)
                        {
                            Categorys.Add(mp);
                        }
                        if (mp.Weight > 0)
                        {
                            weightKeys[cmd] = mp.Weight;
                        }

                        activities[mp.Model][mp.Command] = t;
                        authKeys[cmd] = mp.Auth;

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
                            list.Add(new WeightType { Type = t, Weight = mp.Weight });
                            authKeys[mp.Model] = mp.Auth;

                        }
                    }

                }
                else if (typeof(IWebFactory).IsAssignableFrom(t))
                {
                    if (webFactorys.Exists(e => e.Type.GUID == tUid) == false)
                    {

                        //if (typeof(WebFactory).IsAssignableFrom(t))
                        //{
                        //    webFactorys.Insert(0, new WeightType { Type = t, Weight = 10000 });
                        //}
                        //else
                        //{
                        webFactorys.Add(new WeightType { Type = t, Weight = mp.Weight });
                        //}
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
            public Type Type { get; set; }
            public int Weight
            {
                get; set;
            }
        }

        internal static List<MappingAttribute> Categorys = new List<MappingAttribute>();
        internal static Dictionary<String, WebAuthType> authKeys = new Dictionary<String, WebAuthType>();
        internal static Dictionary<String, int> weightKeys = new Dictionary<String, int>();
        internal static List<WeightType> webFactorys = new List<WeightType>();
        internal static Dictionary<String, List<WeightType>> flows = new Dictionary<string, List<WeightType>>();
        internal static Dictionary<String, Dictionary<String, Type>> activities = new Dictionary<String, Dictionary<string, Type>>();

        class MappingFLow : WebFlow
        {

            public override WebActivity GetFirstActivity()
            {
                var webRequest = this.Context.Request;
                var dic = activities[webRequest.Model];
                if (dic.ContainsKey(webRequest.Command))
                {
                    return Reflection.CreateInstance(dic[webRequest.Command]) as WebActivity;

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
                if (activities.ContainsKey(mode))
                {

                    return new MappingFLow();

                }
                return WebFlow.Empty;

            }

            void IWebFactory.OnInit(WebContext context)
            {

            }
        }
        class MappingFlowFactory : IWebFactory
        {
            int index = 0;
            public MappingFlowFactory(int i)
            {
                this.index = i;
            }
            WebFlow IWebFactory.GetFlowHandler(string mode)
            {
                if (flows.ContainsKey(mode))
                {
                    return Reflection.CreateInstance(flows[mode][index].Type) as WebFlow;
                }
                return WebFlow.Empty;

            }

            void IWebFactory.OnInit(WebContext context)
            {

            }
            public static IWebFactory[] GetFactory(String model)
            {
                if (flows.ContainsKey(model))
                {
                    var len = flows[model].Count;
                    var list = new List<IWebFactory>();

                    for (var i = 0; i < len; i++)
                    {
                        list.Add(new MappingFlowFactory(i));

                    }
                    return list.ToArray();
                }
                return new IWebFactory[0];

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

                    ProcessActivity(flow, flow.GetFirstActivity());
                }
            }
        }

        void Do()
        {
            var factorys = new List<IWebFactory>();



            foreach (var ftype in webFactorys)
            {
                var flowFactory = Reflection.CreateInstance(ftype.Type) as IWebFactory;
                if (flowFactory != null)
                {
                    flowFactory.OnInit(Context);
                    factorys.Add(flowFactory);
                }
            }


            factorys.Add(new MappingActivityFactory());

            factorys.AddRange(MappingFlowFactory.GetFactory(Context.Request.Model));

            
            try
            {
                DoFactory(factorys);

            }
            catch (UMC.Web.WebAbortException)
            {

            }

        }
        void ProcessActivity(WebFlow flow, WebActivity active)
        {
            active.Context = this.Context;
            if (active == WebActivity.Empty)
            {
            }
            else
            {
                active.Flow = flow;
                this.CurrentActivity = active;
                active.ProcessActivity(this.Request, this.Response);
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
