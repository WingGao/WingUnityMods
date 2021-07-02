using System;

namespace TestGameMain
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var a = new ClassA();
            Console.WriteLine("hello {0}", a.GetName());
            Console.ReadLine();
        }
    }
}