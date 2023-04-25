using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using iFActionGame2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WingUtil.Harmony;

namespace WingMod
{
    public static class Helper
    {
        public static string GamePath = "d:\\Program Files (x86)\\Steam\\steamapps\\common\\JiangCity\\";

        public static bool IRWFileAssertMs(IRWFile f, string s)
        {
            return f.ReadMs(s.Length) == s;
        }

        /// <summary>
        /// 将资源导出
        /// </summary>
        public static void UnPack()
        {
            IVal.BasePath = GamePath;
            var dpackClassName = "ypYelOSLmnAUSe2WuA.nqUuKJcbtj5XBCQC65";
            var dpackType = AccessTools.TypeByName(dpackClassName);
            var pack = AccessTools.GetDeclaredConstructors(dpackType).First().Invoke(new object[] {"iFCon"});
            // -> foreach (var pf in pack.fileList.list)
            var files = WingAccessTools.GetFieldValue<object>(pack, "mbNP99AiDA");
            foreach (var pf in WingAccessTools.GetFieldValue<IDictionary>(files, "t9vPuSRns9").Keys)
            {
                var pfs = pf as string;
                Debug.WriteLine(pfs);
                var fullPath = Path.Combine(GamePath, "unpack", pfs);
                var dir = Path.GetDirectoryName(fullPath);
                if (!File.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // -> File.WriteAllBytes(fullPath, pack.getFile(pf.Key));
                File.WriteAllBytes(fullPath, WingAccessTools.InvokeMethod<object>(pack, "VEZPxV3Xs4", new object[] {pfs}) as byte[]);
            }
        }

        public static void MakePluginFile()
        {
            IWriteData w = new IWriteData();
            w.aString("iFPlug");
            w.aInt(1);
            // mod iFActionGame2.data.DMods.Mod.Mod
            //  DModProject
            w.aMString("iFMOD");
            w.aInt(101); //版本
            w.aString("WingMod"); // this.projectKey = rd.ReadString();
            w.aString("WingMod"); // this.name = rd.ReadString();
            w.aString("0.0.1"); // this.ver = rd.ReadString();
            w.aInt(1); // this.type = rd.ReadInt();
            w.aString("WingMod"); // this.key = rd.ReadString();
            //dll 目前好像没用到
            w.aInt(0);
            // w.aString("WingMod.dll"); // this.file = rd.ReadString();
            // w.aInt(0); // this.index = rd.ReadInt();
            // w.aBool(true); // this.isCheck = rd.ReadBool();

            w.aBool(true); // this.pPc = rd.ReadBool();
            w.aBool(true); // this.pWeb = rd.ReadBool();
            w.aBool(true); // this.pAndroid = rd.ReadBool();
            w.aBool(true); // this.pIOS = rd.ReadBool();
            w.aBool(true); // this.pWexin = rd.ReadBool();
            w.aString("true"); // this.msg = rd.ReadString();
            w.aBool(true); // this.isSelfWin = rd.ReadBool();
            w.aInt(0); // this.dllIndex = rd.ReadInt();
            w.aString("WingMod"); // this.dllNameSpace = rd.ReadString();
            //data
            w.aInt(0);
            // triggers
            w.aInt(0);
            //js
            w.aInt(0);
            w.aString("");
            //res
            w.aInt(0);
            w.aString(""); // this.resFile = rd.ReadString();
            w.aBool(false); // this.isSelfDir = rd.ReadBool();
            w.aBool(false); // this.isAutoInput = rd.ReadBool();
            //modui
            w.aInt(0);
            w.aInt(0);
            //DScript
            w.aInt(1);
            w.aString("WingMod"); // this.name = rd.ReadString();
            //language=js
            w.aString(@"
RF.log('WingMod Start');
(function(){
    if2d_dll.loadDll('WingMod.dll');
    var id = if2d_dll.loadDll('WingMod.dll');
    if2d_dll.doStaticFunction(id,'WingMod.MyPlugin','Hook')
})();
RF.log('WingMod End');
"); // this.script = rd.ReadString();

            w.aString(""); // this.path = rd.ReadString();
            w.aString("WingMod"); // this.key = rd.ReadString();
            w.aInt(0); //pos

            File.WriteAllBytes(Path.Combine(GamePath, "iFMods"), w.getByts());
        }


        /// <summary>
        /// 给deps文件打补丁
        /// </summary>
        public static void PatchDeps()
        {
            var oDepPath = GamePath + "iFActionGame.deps0.json"; //优先读取
            var depPath = GamePath + "iFActionGame.deps.json";
            var j = File.ReadAllText(File.Exists(oDepPath) ? oDepPath : depPath);
            var coreDeps = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(j);
            var libraries = coreDeps["libraries"];
            if (libraries.ContainsKey("WingMod/1.0.0.0")) return; //已经运行过了
            var myDeps = new Dictionary<string, string>()
            {
                {"Lib.Harmony", "2.2.2.0"},
                {"WingMod", "1.0.0.0"},
                {"WingUtil.Harmony", "1.0.0.0"},
            };
            foreach (var (key, value) in myDeps)
            {
                var coreApp = coreDeps["targets"][".NETCoreApp,Version=v3.1"];
                coreApp["iFActionGame/1.0.0"]["dependencies"][key] = value;
                var dllFile = key + ".dll";
                switch (key)
                {
                    case "Lib.Harmony":
                        dllFile = "0Harmony.dll";
                        break;
                    default:
                        break;
                }
                coreApp[key + "/" + value] = new JObject(
                    new JProperty("runtime", new JObject(
                        new JProperty(dllFile, new JObject(
                            new JProperty("assemblyVersion", "1.0.0.0"),
                            new JProperty("fileVersion", "1.0.0.0")
                        ))
                    )));
                coreDeps["libraries"][key + "/" + value] = new JObject(
                    new JProperty("type", "reference"),
                    new JProperty("serviceable", false),
                    new JProperty("sha512", "")
                );
                // coreDeps.targets.Net31["iFActionGame/1.0.0"].dependencies[key] = value;
                // var dep = new CoreDeps.Dep()
                // {
                //     runtime = new Dictionary<string, Dictionary<string, string>>()
                //     {
                //         {
                //             key + ".dll", new Dictionary<string, string>()
                //             {
                //                 {"assemblyVersion", value},
                //                 {"fileVersion", value},
                //             }
                //         }
                //     }
                // };
                // coreDeps.targets.Net31.Add(key + "/" + value, dep);
                // coreDeps.libraries.Add(key + "/" + value, new CoreDeps.Lib() {type = "reference", serviceable = false, sha512 = ""});
            }

            //回写
            var jj = JsonConvert.SerializeObject(coreDeps);
            File.WriteAllText(depPath, jj);
        }
    }
}