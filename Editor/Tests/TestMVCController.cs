using NUnit.Framework;
using TLP.UdonUtils.EditorOnly;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using UnityEngine;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP/UdonUtils")]
    public class TestMvcController : TestWithLogger
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

        // invalid input
        [TestCase(1, false, true, true, true, false, false, false, "model invalid")]
        [TestCase(2, true, false, true, true, false, false, false, "view invalid")]
        [TestCase(3, false, false, true, true, false, false, false, "view invalid")]

        // internal init fails but de-init succeeds
        [TestCase(4, true, true, false, true, false, false, false, "Initialization failed")]

        // internal init and de-init fails
        [TestCase(5, true, true, false, false, false, true, true, "Initialization failed", "De-Initialization failed")]

        // internal init succeeds but de-init would fail
        [TestCase(6, true, true, true, false, true, true, false)]
        public void TestInitialization(
            int test,
            bool model,
            bool view,
            bool initSuccess,
            bool deInitSuccess,
            bool initReturnValue,
            bool initState,
            bool errorState,
            params string[] error
        )
        {
            Assert.True(_model.Initialize(_changeEvent));

            _controller.InitResult = initSuccess;
            _controller.DeInitResult = deInitSuccess;
            if (error != null)
            {
                foreach (string s in error)
                {
                    ExpectError(s);
                }
            }

            Assert.AreEqual(
                initReturnValue,
                _controller.Initialize(model ? _model : null, view ? _view : null),
                test.ToString()
            );
            Assert.AreEqual(initState, _controller.Initialized, test.ToString());
            Assert.AreEqual(errorState, _controller.HasError, test.ToString());
        }

        [Test]
        public void InitFailsIfHasError()
        {
            _controller.SetMockHasError(true);
            ExpectError("Can not initialize again due to previous critical error");
            Assert.False(_controller.Initialize(_model, _view));
        }

        [Test]
        public void InitReturnsFalseAfterInitialSuccess()
        {
            Assert.True(_model.Initialize(_changeEvent));
            Assert.True(_controller.Initialize(_model, _view));
            ExpectWarning("Already initialized");
            Assert.False(_controller.Initialize(_model, _view));
        }

        [Test]
        public void InitFailsIfModelIsNotInitialized()
        {
            _model.DeInitialize();
            Assert.False(_model.Initialized);
            ExpectError("model is not initialized");
            Assert.False(_controller.Initialize(_model, _view));
        }

        [Test]
        public void InitFailsIfModelHasError()
        {
            _model.SetMockHasError(true);
            ExpectError("model has critical error");
            Assert.False(_controller.Initialize(_model, _view));
        }

        [Test]
        public void InitFailsIfViewIsAlreadyInitialized()
        {
            Assert.True(_model.Initialize(_changeEvent));

            _view.Initialize(null, _model);
            Assert.True(_view.Initialized);

            ExpectError("view is already initialized");
            Assert.False(_controller.Initialize(_model, _view));
        }

        [Test]
        public void InitFailsIfViewHasError()
        {
            _view.SetMockHasError(true);
            ExpectError("view has critical error");
            Assert.False(_controller.Initialize(_model, _view));
        }

        [Test]
        public void CompleteSetup()
        {
            _model.DeInitialize();

            Assert.True(_model.Initialize(_changeEvent));
            Assert.True(_controller.Initialize(_model, _view));
            Assert.True(_view.Initialize(_controller, _model));
        }

        [Test]
        public void CompleteSetupDifferentOrder()
        {
            _model.DeInitialize();

            Assert.True(_model.Initialize(_changeEvent));
            ExpectError("optionalController is not initialized");
            Assert.False(_view.Initialize(_controller, _model));
            Assert.True(_controller.Initialize(_model, _view));
        }

        [Test]
        public void DeInitDeInitializesAlsoModelAndView()
        {
            // setup
            Assert.True(_model.Initialize(_changeEvent));
            Assert.True(_controller.Initialize(_model, _view));
            Assert.True(_view.Initialize(_controller, _model));

            Assert.True(_controller.Initialized);
            Assert.True(_view.Initialized);
            Assert.True(_model.Initialized);

            // test
            Assert.True(_controller.DeInitialize());

            Assert.False(_controller.Initialized);
            Assert.False(_view.Initialized);
            Assert.False(_model.Initialized);

            Assert.False(_controller.HasError);
            Assert.False(_view.HasError);
            Assert.False(_model.HasError);
        }
    }
}