using TLP.UdonUtils.DesignPatterns.MVC;

namespace TLP.UdonUtils.EditorOnly
{
    public class MockView : View
    {
        public bool InitResult = true;
        public bool DeInitResult = true;
        public int ModelChangedInvocations;

        protected override bool InitializeInternal() {
            return InitResult;
        }

        protected override bool DeInitializeInternal() {
            return DeInitResult;
        }

        public override void OnModelChanged() {
            ++ModelChangedInvocations;
        }

        public void SetMockHasError(bool error) {
            HasError = error;
        }
    }
}