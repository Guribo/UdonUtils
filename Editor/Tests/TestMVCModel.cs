using NUnit.Framework;
using TLP.UdonUtils.EditorOnly;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using UnityEngine;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP/UdonUtils")]
    public class TestMvcModel : TestWithLogger
    {
        private MockModel _model;
        private GameObject _gameObject;
        private UdonEvent _changeEvent;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _gameObject = new GameObject("MVC");
            _changeEvent = _gameObject.AddComponent<UdonEvent>();
            _model = _gameObject.AddComponent<MockModel>();
        }

        [Test]

        // valid input/everything ok
        [TestCase(0, true, true, true, true, true, false)]

        // invalid input
        [TestCase(1, false, true, true, false, false, false, "changeEvent invalid")]

        // internal init fails but de-init succeeds
        [TestCase(2, true, false, true, false, false, false, "Initialization failed")]

        // internal init and de-init fails
        [TestCase(3, true, false, false, false, true, true, "Initialization failed", "De-Initialization failed")]

        // internal init succeeds but de-init would fail
        [TestCase(4, true, true, false, true, true, false)]
        public void TestInitialization(
            int test,
            bool hasEvent,
            bool initSuccess,
            bool deInitSuccess,
            bool initReturnValue,
            bool initState,
            bool errorState,
            params string[] error
        )
        {
            _model.InitResult = initSuccess;
            _model.DeInitResult = deInitSuccess;
            if (error != null)
            {
                foreach (string s in error)
                {
                    ExpectError(s);
                }
            }

            Assert.AreEqual(
                initReturnValue,
                _model.Initialize(hasEvent ? _changeEvent : null),
                test.ToString()
            );
            Assert.AreEqual(initState, _model.Initialized, test.ToString());
            Assert.AreEqual(errorState, _model.HasError, test.ToString());

            if (hasEvent && initSuccess && initState && initReturnValue)
            {
                Assert.True(ReferenceEquals(_model, _changeEvent.Listeners[0]));
            }
            else
            {
                Assert.AreEqual(0, _changeEvent.ListenerCount);
            }
        }

        [Test]
        public void InitFailsIfHasError()
        {
            _model.SetMockHasError(true);
            ExpectError("Can not initialize again due to previous critical error");
            Assert.False(_model.Initialize(_changeEvent));
        }

        [Test]
        public void InitReturnsFalseAfterInitialSuccess()
        {
            Assert.True(_model.Initialize(_changeEvent));
            Assert.False(_model.Initialize(_changeEvent));
        }

        [Test]
        public void InitSetsEventNameCorrectly()
        {
            Assert.True(_model.Initialize(_changeEvent));
            Assert.AreEqual("OnModelChanged", _changeEvent.ListenerMethod);
        }
    }
}