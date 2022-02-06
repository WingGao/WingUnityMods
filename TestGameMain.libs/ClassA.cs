using System;
using TestGameMain.Other;

namespace TestGameMain
{
    public class ClassA
    {
        public ClassB FieldClass1 = new ClassB();

        private String GetNamePrivate()
        {
            return "GetNamePrivate from ClassA";
        }

        public String GetName()
        {
            return GetNamePrivate();
        }

        public String Method1()
        {
            return "Method1";
        }

        public String Method2()
        {
            return "Method2";
        }

        static public void MethodStatic1()
        {
            Console.WriteLine("MethodStatic1 Run");
        }

        public String Print()
        {
            if (FieldClass1.FieldInt1 == 1)
            {
                var a = Method1();
                Console.WriteLine("FieldClass1.FieldInt1 " + a);
            }

            var floatA = 0.5f; //operand == 0.5f
            if (floatA < 0.4f)
            {
                Console.WriteLine($"floatA={floatA}");
            }

            ClassC.Method1();
            MethodStatic1();

            return "Print";
        }
    }
}