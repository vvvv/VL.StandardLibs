using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.MessagePack
{
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
