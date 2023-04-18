using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UMC.Data;
using UMC.Data.Sql;

namespace UMC.Web.Activity
{

    [Mapping("System", "Setup", Auth = WebAuthType.All, Desc = "UMC安装管理", Weight = 0)]
    public class SystemSetupActivity : WebActivity
    {
        string MySQL(WebMeta meta)
        {
            var str = "Server={0};Port={4};Database={1};Uid={2};Pwd={3};Charset=utf8";
            if (meta["Port"] == "3306")
            {
                str = "Server={0};Database={1};Uid={2};Pwd={3};Charset=utf8";
            }

            return String.Format(str, meta["Server"], meta["Database"], meta["User"], meta["Password"], meta["Port"]);

        }
        string MSSQL(WebMeta meta)
        {

            var str = "Data Source={0},{4};Initial Catalog={1};User ID={2};Password={3};";
            if (meta["Port"] == "1433")
            {
                str = "Data Source={0};Initial Catalog={1};User ID={2};Password={3};";
            }

            return String.Format(str, meta["Server"], meta["Database"], meta["User"], meta["Password"], meta["Port"]);

        }
        string Oracle(WebMeta meta)
        {
            if (meta.ContainsKey("Port") == false)
            {
                meta["Port"] = "1521";
            }
            var str = "User Id={2};Password={3};Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={4})))(CONNECT_DATA=(SERVICE_NAME={1})))";
            return String.Format(str, meta["Server"], meta["Database"], meta["User"], meta["Password"], meta["Port"]);

        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            // var Initializers = Data.Sql.Initializer.Initializers();
            var name = this.AsyncDialog("Name", g =>
            {
                var fm = new UISheetDialog() { Title = "选择安装组件" };
                foreach (var v in Data.Sql.Initializer.Initializers)
                {
                    fm.Put(new UIClick(v.Name) { Text = v.Caption }.Send(request.Model, request.Command));

                }
                return fm;
            });
            switch (name)
            {
                case "Command":
                    var Command = this.AsyncDialog("Command", g =>
                    {
                        var fm = new UIFormDialog() { Title = "调试指令" };

                        fm.AddText("模块", "Model").PlaceHolder("触发的模块");
                        fm.AddText("指令", "Command").PlaceHolder("触发的指令");
                        fm.AddText("参数", "Send").NotRequired().PlaceHolder("触发的参数");
                        fm.Submit("执行指令");
                        fm.AddUIIcon("\uf1b3", "扫描模块", "从新加载模块类型").Command(request.Model, request.Command, "Scanning");
                        return fm;
                    });
                    var send = Command["Send"];
                    if (String.IsNullOrEmpty(send) == false)
                    {
                        response.Redirect(Command["Model"], Command["Command"], Command["Send"]);
                    }
                    else
                    {

                        response.Redirect(Command["Model"], Command["Command"]);
                    }
                    break;
                case "Scanning":

                    Reflection.Instance().ScanningClass();
                    this.Prompt("已从新扫描类型", false);

                    this.Context.Send($"{request.Model}.{request.Command}", true);
                    break;
                case "Mapping":

                    var setup = Reflection.Configuration("setup") ?? new Data.ProviderConfiguration();

                    var data = new System.Data.DataTable();
                    data.Columns.Add("name");
                    data.Columns.Add("text");
                    data.Columns.Add("setup", typeof(bool));
                    foreach (var n in setup.Providers)
                    {
                        var initer3 = Initializer.Initializers.FirstOrDefault(r => String.Equals(r.Name, n.Name));
                        data.Rows.Add(n.Name, initer3?.Caption ?? n.Name, String.IsNullOrEmpty(n.Attributes["setup"]) == false);
                    }
                    response.Redirect(new WebMeta().Put("component", data).Put("data", WebServlet.Mapping()));
                    break;
                default:
                    break;
            }

            var Setup = Reflection.Configuration("setup") ?? new ProviderConfiguration();
            if (request.IsMaster == false && Setup.Count > 0)
            {
                this.Prompt("只有管理员才能检测升级");
            }

            var initer = Initializer.Initializers.FirstOrDefault(r => String.Equals(r.Name, name));
            if (initer == null)
            {
                this.Prompt("无此业务组件");
            }
            var database = Reflection.Configuration("database") ?? new UMC.Data.ProviderConfiguration();
            if (String.IsNullOrEmpty(initer.ProviderName) == false && database.ContainsKey(initer.ProviderName) == false)
            {
                var type = this.AsyncDialog("type", g =>
                {
                    var fm = new UISheetDialog() { Title = "安装数据库" };
                    fm.Put(new UIClick("Oracle") { Text = "Oracle数据库" }.Send(request.Model, request.Command))
                    .Put(new UIClick("MySql") { Text = "MySql数据库" }.Send(request.Model, request.Command))
                    .Put(new UIClick("MSSQL") { Text = "SQL Server数据库" }.Send(request.Model, request.Command));
                    return fm;
                });
                var Settings = this.AsyncDialog("Settings", g =>
                {
                    var fm = new UIFormDialog() { Title = "选择数据库" };

                    fm.AddText("服务地址", "Server");
                    fm.AddText("用户名", "User");
                    fm.AddText("密码", "Password");
                    fm.AddText("数据库名", "Database");
                    switch (type)
                    {
                        case "SQLite":
                            return this.DialogValue(new WebMeta().Put("File", initer.ProviderName));
                        case "Oracle":
                            fm.AddText("端口", "Port", "1521");
                            fm.AddText("表前缀", "Prefix").Put("tip", "分表设置");
                            fm.Title = "Oracle连接配置";
                            break;
                        case "MySql":
                            fm.AddText("端口", "Port", "3306");
                            fm.AddText("表前缀", "Prefix").Put("tip", "分表设置");
                            fm.Title = "MySql连接配置";
                            break;
                        case "MSSQL":
                            fm.AddText("端口", "Port", "1433");
                            fm.AddText("表前缀", "Prefix").Put("tip", "分表设置");
                            fm.Title = "SQL Server连接配置";
                            break;
                        default:
                            this.Prompt("数据类型错误");
                            break;
                    }
                    fm.Submit("确认安装", $"{request.Model}.{request.Command}");
                    return fm;
                });
                UMC.Data.Provider provder = null;


                switch (type)
                {
                    case "SQLite":
                        provder = UMC.Data.Provider.Create("Database", typeof(UMC.Data.Sql.SQLiteDbProvider).FullName);


                        var fname = Settings["File"] + ".sqlite"; ;


                        var path = UMC.Data.Reflection.AppDataPath(fname);

                        if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                        {
                            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                        }
                        provder.Attributes["db"] = fname;
                        break;
                    case "Oracle":
                        provder = UMC.Data.Provider.Create("Database", typeof(UMC.Data.Sql.OracleDbProvider).FullName);
                        provder.Attributes["conString"] = Oracle(Settings);
                        break;
                    case "MySql":
                        provder = UMC.Data.Provider.Create("Database", typeof(UMC.Data.Sql.MySqlDbProvider).FullName);
                        provder.Attributes["conString"] = MySQL(Settings);
                        break;
                    case "MSSQL":
                        provder = UMC.Data.Provider.Create("Database", typeof(UMC.Data.Sql.SqlDbProvider).FullName);
                        provder.Attributes["conString"] = MSSQL(Settings);
                        break;
                    default:
                        this.Prompt("数据类型错误");
                        break;
                }
                if (String.IsNullOrEmpty(Settings["Prefix"]) == false)
                {
                    provder.Attributes["delimiter"] = Settings["Delimiter"] ?? "_";
                    provder.Attributes["prefix"] = Settings["Prefix"];
                }
                DbProvider provider = Reflection.CreateObject(provder) as DbProvider;
                DbFactory factory = new DbFactory(provider);
                try
                {
                    factory.Open();
                    factory.Close();

                    var p = UMC.Data.Provider.Create(initer.ProviderName, provder.Type);
                    p.Attributes.Add(provder.Attributes);
                    database.Add(p);

                    Reflection.Configuration("database", database);
                }
                catch (Exception ex)
                {
                    this.Prompt(ex.Message);
                }
            }

            var Key = $"{request.Model}.{request.Command}.{initer.Name}";
            var log = new UMC.Data.CSV.Log(Key, "开始执行请求");


            try
            {
                var now = DateTime.Now;

                var provder = Setup[initer.Name] ?? UMC.Data.Provider.Create(initer.Name, initer.GetType().FullName);

                if (String.IsNullOrEmpty(provder.Attributes["setup"]))
                {
                    if (String.IsNullOrEmpty(initer.ProviderName))
                    {
                        log.Info("正在安装数据项", initer.Caption);
                        initer.Setup(log);
                    }

                    else
                    {
                        log.Info("正在安装数据库项", initer.Caption);
                        initer.Setup(log, Reflection.CreateObject(database[initer.ProviderName]) as DbProvider);
                        log.Info("正在安装数据项", initer.Caption);
                        initer.Setup(log);
                    }
                    provder.Attributes["setup"] = "true";

                    Setup.Add(provder);
                    Reflection.Configuration("setup", Setup);

                    log.End("安装完成", "请刷新界面");
                    log.Info(String.Format("用时{0}", DateTime.Now - now));

                }
                else
                {
                    if (String.IsNullOrEmpty(initer.ProviderName))
                    {
                        log.Info("正在检测升级数据项", initer.Caption);
                        initer.Upgrade(log);
                    }

                    else
                    {
                        log.Info("正在检测升级数据库", initer.Caption);
                        initer.Upgrade(log, Reflection.CreateObject(database[initer.ProviderName]) as DbProvider);
                        log.Info("正在检测升级数据项", initer.Caption);
                        initer.Upgrade(log);
                    }
                    log.End("检测完成", "请刷新界面");
                    log.Info(String.Format("用时{0}", DateTime.Now - now));
                }


                this.Prompt("检测完成", String.Format("用时{0},请刷新界面", DateTime.Now - now), false);

            }
            catch (Exception ex)
            {
                log.End("执行失败");
                log.Info(ex.Message);

                this.Prompt("执行失败", ex.Message);

            }
            finally
            {
                log.Close();
            }

            this.Context.Send($"{request.Model}.{request.Command}", true);

        }

    }
}
