using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.MessagePack
{
    public static class Test
    {
        public static object TestObject () { return new object[] { 1, "a", new TestClass("test",42,0.23f) }; }
    }

    public class TestClass
    {
        public string MyString;
        public int MyInt;
        public float MyFloat;

        public Dictionary<string, int> MyDictionary;

        public TestClass(string myString, int myInt, float myFloat)
        {
            MyString = myString;
            MyInt = myInt;
            MyFloat = myFloat;

            MyDictionary = new Dictionary<string, int>();
            MyDictionary.Add(MyString, MyInt);
        }   
    }
}
