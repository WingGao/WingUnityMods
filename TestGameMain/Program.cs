using System;
using TestGameMain.GameSingle;
using TestGameMainPatch;

namespace TestGameMain
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            PatchUMM.Patch();

            Run();
        }

        public static void Run()
        {
            // WarmSnowHelper.DecryptXMLs();
            var a = new ClassA();
            Console.WriteLine("hello {0}", a.GetName());
            Console.WriteLine("Call {0}", a.Print());
            Console.ReadLine();
        }
    }
}