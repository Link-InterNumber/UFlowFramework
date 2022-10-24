using System;
using LinkFrameWork.DesignPatterns;
using UnityEngine;

namespace Test.SingletonTest
{
    public class TestSingleton: SingletonBase<TestSingleton>
    {
        public int TestIntP = 6;
    }

    public class TestModule : TestSingleton
    {
        public int TestInt = 5;
    }

    public class TestSingleUsing : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log($"{TestModule.Instance.TestIntP}");
        }
    }
}