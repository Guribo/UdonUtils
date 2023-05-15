using NUnit.Framework;
using TLP.UdonUtils.EditorOnly;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using UnityEngine;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP/UdonUtils")]
    public class TestMvcView : TestWithLogger
    {
        private MockController _controller;
        private MockModel _model;
        private MockView _view;
        private GameObject _gameObject;
        private UdonEvent _changeEvent;

        [SetUp]
        public override void Setup()
        {
            base.Setup();
            _gameObject = new GameObject("MVC");
            _controller = _gameObject.AddComponent<MockController>();
            _model = _gameObject.AddComponent<MockModel>();
            _view = _gameObject.AddComponent<MockView>();
            _changeEvent = _gameObject.AddComponent<UdonEvent>();
        }

        [Test]

        // valid input/everything ok
        [TestCase(0, true, true, true, true, true, true, false)]
        [TestCase(1, false, true, true, true, true, true, false)]

        // invalid input
        [TestCase(2, true, false, false, true, false, false, false, "model invalid")]
        [TestCase(3, false, false, false, true, false, false, false, "model invalid")]

        // internal init fails but de-init succeeds
        [TestCase(4, true, true, false, true, false, false, false, "Initialization failed")]

        // internal init and de-init fails
        [TestCase(5, true, true, false, false, false, true, true, "Initialization failed", "De-Initialization failed")]

        // internal init succeeds but de-init would fail
        [TestCase(6, true, true, true, false, true, true, false)]
        public void TestInitialization(
            int test,
            bool hasController,
            bool hasModel,
            bool initSuccess,
            bool deInitSuccess,
            bool initReturnValue,
            bool initState,
            bool errorState,
            params string[] error
        )
        {
            Assert.True(_model.Initialize(_changeEvent));
            Assert.True(_controller.Initialize(_model, _view));

            _view.InitResult = initSuccess;
            _view.DeInitResult = deInitSuccess;

            if (error != null)
            {
                foreach (string s in error)
                {
                    ExpectError(s);
                }
            }

            Assert.AreEqual(
                initReturnValue,
                _view.Initialize(hasController ? _controller : null, hasModel ? _model : null),
                test.ToString()
            );
            Assert.AreEqual(initState, _view.Initialized, test.ToString());
            Assert.AreEqual(errorState, _view.HasError, test.ToString());
        }

        [Test]
        public void InitFailsIfModelIsNotInitialized()
        {
            ExpectError("model is not initialized");
            Assert.False(_view.Initialize(null, _model));
        }

        [Test]
        public void InitFailsIfModelHasError()
        {
            Assert.False(_model.Initialized);
            _model.SetMockHasError(true);
            ExpectError("model has critical error");
            Assert.False(_view.Initialize(_controller, _model));
        }

        [Test]
        public void InitFailsIfHasError()
        {
            Assert.True(_model.Initialize(_changeEvent));

            _view.SetMockHasError(true);
            ExpectError("Can not initialize again due to previous critical error");
            Assert.False(_view.Initialize(_controller, _model));
        }

        [Test]
        public void InitFailsIfControllerHasError()
        {
            Assert.True(_model.Initialize(_changeEvent));

            _controller.SetMockHasError(true);
            ExpectError("optionalController has critical error");
            Assert.False(_view.Initialize(_controller, _model));
        }

        [Test]
        public void InitFailsIfControllerIsNotInitialized()
        {
            Assert.True(_model.Initialize(_changeEvent));

            ExpectError("optionalController is not initialized");
            Assert.False(_view.Initialize(_controller, _model));
        }


        [Test]
        public void InitReturnsFalseAfterInitialSuccess()
        {
            Assert.True(_model.Initialize(_changeEvent));
            Assert.True(_controller.Initialize(_model, _view));

            Assert.True(_view.Initialize(_controller, _model));
            Assert.False(_view.Initialize(_controller, _model));
        }

        [Test]
        public void DeInitDoesNotDeInitializesModelOrController()
        {
            // setup
            Assert.True(_model.Initialize(_changeEvent));
            Assert.True(_controller.Initialize(_model, _view));
            Assert.True(_view.Initialize(_controller, _model));

            Assert.True(_controller.Initialized);
            Assert.True(_view.Initialized);
            Assert.True(_model.Initialized);

            // test
            Assert.True(_view.DeInitialize());
            Assert.False(_view.Initialized);

            Assert.True(_controller.Initialized);
            Assert.True(_model.Initialized);

            Assert.False(_controller.HasError);
            Assert.False(_view.HasError);
            Assert.False(_model.HasError);
        }

        [Test]
        public void ListensToModelChangedAfterInit()
        {
            _model.Initialize(_changeEvent);
            _view.Initialize(null, _model);
            Assert.True(ReferenceEquals(_model, _changeEvent.Listeners[0]));
            Assert.True(ReferenceEquals(_view, _changeEvent.Listeners[1]));
        }

        [Test]
        public void NoLongerListensToModelChangedAfterDeInit()
        {
            Assert.AreEqual(0, _changeEvent.ListenerCount);
            Assert.True(_model.Initialize(_changeEvent));
            Assert.True(_view.Initialize(null, _model));
            Assert.True(_view.DeInitialize());
            Assert.AreEqual(1, _changeEvent.ListenerCount);
        }

        [Test]
        public void ReceivesOnModelChangedWhenModelNotifies()
        {
            Assert.True(_model.Initialize(_changeEvent));
            Assert.True(_view.Initialize(null, _model));
            _model.Dirty = true;
            _model.NotifyIfDirty();
            Assert.AreEqual(1, _view.ModelChangedInvocations);
        }
    }
}