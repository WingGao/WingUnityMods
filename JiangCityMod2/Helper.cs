using System;
using System.Diagnostics;
using System.IO;
using iFActionGame2;
using iFActionGame2.data;

namespace WingMod
{
    public static class Helper
    {
        public static string GamePath = "d:\\Program Files (x86)\\Steam\\steamapps\\common\\JiangCity\\";

        /// <summary>
        /// 将资源导出
        /// </summary>
        public static void UnPack()
        {
            IVal.BasePath = GamePath;
            var pack = new DPack("iFCon");
            foreach (var pf in pack.fileList.list)
            {
                Debug.WriteLine(pf.Key);
                var fullPath = Path.Combine(GamePath, "unpack", pf.Key);
                var dir = Path.GetDirectoryName(fullPath);
                if (!File.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllBytes(fullPath, pack.getFile(pf.Key));
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
    }
}