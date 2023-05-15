using System;
using NUnit.Framework;
using TLP.UdonUtils.Runtime;
using TLP.UdonUtils.Runtime.Rendering;
using TLP.UdonUtils.Runtime.Sync.SyncedVariables;
using UdonSharp;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP")]
    public class TestSyncedBool
    {
#pragma warning disable URA0023
        private class TestDummy : UdonSharpBehaviour
        {
            public bool targetBool;
        }
#pragma warning restore URA0023

        private SyncedBool m_SyncedBool;
        private TestDummy m_TestDummy;

        [SetUp]
        public void Prepare()
        {
            m_SyncedBool = new GameObject().AddComponent<SyncedBool>();
            m_SyncedBool.targetFieldNames = new string[]
            {
                nameof(TestDummy.targetBool)
            };

            m_TestDummy = m_SyncedBool.gameObject.AddComponent<TestDummy>();
            m_SyncedBool.listeners = new UdonSharpBehaviour[]
            {
                m_TestDummy
            };
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(m_SyncedBool);
        }

        #region Set

        [Test]
        public void Set_DoesNothingWhenValueUnchanged()
        {
            m_SyncedBool.SyncedValue = false;
            Assert.DoesNotThrow(() => m_SyncedBool.BoolValueProperty = false);
            Assert.False(m_SyncedBool.SyncedValue);

            m_SyncedBool.SyncedValue = true;
            Assert.DoesNotThrow(() => m_SyncedBool.BoolValueProperty = true);
            Assert.True(m_SyncedBool.SyncedValue);
        }

        #endregion

        #region ListenerSetupValid

        [Test]
        public void ListenerSetupValid_FalseWhenListenersNull()
        {
            m_SyncedBool.listeners = null;
            m_SyncedBool.targetFieldNames = new[] { "" };

            Assert.False(m_SyncedBool.ListenerSetupValid());
        }

        [Test]
        public void ListenerSetupValid_FalseWhenTargetFieldNamesNull()
        {
            m_SyncedBool.listeners = new UdonSharpBehaviour[1];
            m_SyncedBool.targetFieldNames = null;

            Assert.False(m_SyncedBool.ListenerSetupValid());
        }

        [Test]
        public void ListenerSetupValid_FalseWhenLengthsDontMatch()
        {
            m_SyncedBool.listeners = new UdonSharpBehaviour[2];
            m_SyncedBool.targetFieldNames = new[] { "" };

            Assert.False(m_SyncedBool.ListenerSetupValid());
        }

        [Test]
        public void ListenerSetupValid_TrueWhenLengthsMatch()
        {
            m_SyncedBool.listeners = new UdonSharpBehaviour[1];
            m_SyncedBool.targetFieldNames = new[] { "" };

            Assert.True(m_SyncedBool.ListenerSetupValid());
        }

        #endregion

        #region NotifyListeners

        [Test]
        public void NotifyListeners_ReturnsFalseWhenListenerSetupInvalid()
        {
            m_SyncedBool.listeners = null;
            LogAssert.Expect(LogType.Error, "Invalid listener setup");
            Assert.DoesNotThrow(() => m_SyncedBool.NotifyListeners());
        }


        [Test]
        public void NotifyListeners_DoesNothingWhenListsEmpty()
        {
            m_SyncedBool.listeners = Array.Empty<UdonSharpBehaviour>();
            m_SyncedBool.targetFieldNames = Array.Empty<string>();
            ReflectionUtils.PatchMethod(
                typeof(UdonSharpBehaviour),
                nameof(SyncedBool.SetProgramVariable),
                typeof(TestSyncedBool),
                nameof(SetProgramVariablePrefix),
                (harmony) =>
                {
                    LogAssert.NoUnexpectedReceived();
                    Assert.DoesNotThrow(() => m_SyncedBool.NotifyListeners());
                }
            );
        }

        [Test]
        public void NotifyListeners_DoesNothingForNullEntries()
        {
            m_SyncedBool.listeners = new UdonSharpBehaviour[] { null, null };
            m_SyncedBool.targetFieldNames = new[] { "", "" };
            ReflectionUtils.PatchMethod(
                typeof(UdonSharpBehaviour),
                nameof(SyncedBool.SetProgramVariable),
                typeof(TestSyncedBool),
                nameof(SetProgramVariablePrefix),
                (harmony) =>
                {
                    LogAssert.NoUnexpectedReceived();
                    Assert.DoesNotThrow(() => m_SyncedBool.NotifyListeners());
                }
            );
        }

        [Test]
        public void NotifyListeners_InvokesSetProgramVariableForValidEntryPair()
        {
            m_SyncedBool.listeners = new UdonSharpBehaviour[] { m_SyncedBool, m_SyncedBool };
            m_SyncedBool.targetFieldNames = new[] { "Test", "Test2" };
            ReflectionUtils.PatchMethod(
                typeof(UdonSharpBehaviour),
                nameof(SyncedBool.SetProgramVariable),
                typeof(TestSyncedBool),
                nameof(SetProgramVariablePrefix),
                (harmony) =>
                {
                    m_SyncedBool.SyncedValue = false;
                    LogAssert.Expect(LogType.Log, $"Test False");
                    LogAssert.Expect(LogType.Log, $"Test2 False");
                    Assert.DoesNotThrow(() => m_SyncedBool.NotifyListeners());

                    m_SyncedBool.SyncedValue = true;
                    LogAssert.Expect(LogType.Log, "Test True");
                    LogAssert.Expect(LogType.Log, "Test2 True");
                    Assert.DoesNotThrow(() => m_SyncedBool.NotifyListeners());
                }
            );
        }

        public static bool SetProgramVariablePrefix(string name, object value)
        {
            Debug.Log($"{name} {value}");
            return false; // use the patched method over the original method
        }

        #endregion
    }
}