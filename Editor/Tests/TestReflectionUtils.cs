using NUnit.Framework;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Rendering;
using UnityEngine;
using UnityEngine.TestTools;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP")]
    public class TestReflectionUtils
    {
        public static int GetFixedInteger(int seed)
        {
            return 42;
        }

        public static bool GetFixedIntegerReplacement(int seed, ref int __result)
        {
            __result = 10; // set return value
            return false; // don't run original method
        }

        public static bool GetFixedIntegerAddition(int seed, ref int __result)
        {
            Debug.Log("Hello World");
            return true; // run original method
        }

        #region PatchMethod

        [Test]
        public void PatchMethod_ReplacesReturnValue()
        {
            ReflectionUtils.PatchMethod(
                typeof(TestReflectionUtils),
                nameof(GetFixedInteger),
                typeof(TestReflectionUtils),
                nameof(GetFixedIntegerReplacement),
                (harmony) =>
                {
                    LogAssert.NoUnexpectedReceived();
                    Assert.AreEqual(10, GetFixedInteger(1));
                }
            );
        }

        [Test]
        public void PatchMethod_AddsFunctionality()
        {
            ReflectionUtils.PatchMethod(
                typeof(TestReflectionUtils),
                nameof(GetFixedInteger),
                typeof(TestReflectionUtils),
                nameof(GetFixedIntegerAddition),
                (harmony) =>
                {
                    LogAssert.Expect(LogType.Log, "Hello World");
                    Assert.AreEqual(42, GetFixedInteger(1));
                }
            );
        }

        #endregion
    }
}