
using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace PowerCellStudio
{
    public class RunTestMono : MonoBehaviour
    {
        protected void RunTest(string testName, Action testAction)
        {
            try
            {
                testAction();
                UnityEngine.Debug.Log($"<color=green>[PASS]</color> {testName}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"<color=red>[FAIL]</color> {testName}\nException: {e.Message}\n{e.StackTrace}");
            }
        }

        protected void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }

        protected void RunPerformanceTest(string testName, System.Action testAction)
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                testAction();
                stopwatch.Stop();
                UnityEngine.Debug.Log($"[PASS] {testName}: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");
            }
            catch (System.Exception e)
            {
                stopwatch.Stop();
                UnityEngine.Debug.LogError($"[FAIL] {testName} crashed after {stopwatch.Elapsed.TotalMilliseconds:F2} ms. \nException: {e.Message} \n{e.StackTrace}");
            }
        }

        protected static void ExecuteEnumerator(IEnumerator routine)
        {
            if (routine == null)
            {
                return;
            }

            while (routine.MoveNext())
            {
                if (routine.Current is IEnumerator nested)
                {
                    ExecuteEnumerator(nested);
                }
            }
        }
    }
}
