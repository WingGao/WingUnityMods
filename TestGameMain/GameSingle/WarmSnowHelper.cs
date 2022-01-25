using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TestGameMain.GameSingle
{
    public class WarmSnowHelper
    {
        public static string Decrypt(string content, string key)
        {
            if (string.IsNullOrEmpty(content))
                return (string) null;
            byte[] inputBuffer = Convert.FromBase64String(content);
            byte[] bytes = Encoding.UTF8.GetBytes(key);
            RijndaelManaged rijndaelManaged = new RijndaelManaged();
            rijndaelManaged.Key = bytes;
            rijndaelManaged.Mode = CipherMode.ECB;
            rijndaelManaged.Padding = PaddingMode.PKCS7;
            return Encoding.UTF8.GetString(rijndaelManaged.CreateDecryptor().TransformFinalBlock(inputBuffer, 0, inputBuffer.Length));
        }

        public static void DecryptXMLs()
        {
            var gameDir =
                "e:\\Program Files (x86)\\Steam\\steamapps\\common\\WarmSnow\\WarmSnow_Data\\StreamingAssets\\XML";
            var fileList = new string[] {"Config_Map.xml","Config_MonsterGroup.xml","Config_Monster.xml"};
            foreach (var f in fileList)
            {
                var fPath = Path.Combine(gameDir, f);
                var rawFile = Decrypt(Decrypt(File.ReadAllText(fPath), "lszcrlydfywtnmbz"), "xxjtdxqhbcbgzjpl");
                File.WriteAllText(fPath+"_raw.xml",rawFile);
            }
        }
    }
}