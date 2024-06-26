using TLP.UdonUtils.Runtime.DesignPatterns.MVC;

namespace TLP.UdonUtils.Runtime.EditorOnly
{
    public class MockController : Controller
    {
        public bool InitResult = true;
        public bool DeInitResult = true;


        protected override bool InitializeInternal() {
            return InitResult;
        }

        protected override bool DeInitializeInternal() {
            return DeInitResult;
        }

        public void SetMockHasError(bool error) {
            HasError = error;
        }
    }
}