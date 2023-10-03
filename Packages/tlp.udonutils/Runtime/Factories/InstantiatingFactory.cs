using TLP.UdonUtils.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Factories
{
    [DefaultExecutionOrder(ExecutionOrder)]
    public class InstantiatingFactory : TlpFactory
    {
        public UdonSharpBehaviour Prototype;

        protected override string GetProductTypeName()
        {
            if (!string.IsNullOrWhiteSpace(FactoryKey))
            {
                return base.GetProductTypeName();
            }

            if (Utilities.IsValid(Prototype))
            {
                string udonTypeName = Prototype.GetUdonTypeName();
                FactoryKey = "";
                return UdonCommon.UdonTypeNameShort(udonTypeName);
            }

            return null;
        }


        protected override GameObject ProduceInstance()
        {
            if (Utilities.IsValid(Prototype))
            {
                return Instantiate(Prototype.gameObject);
            }

            Error($"{nameof(Prototype)} not set");
            return null;
        }
    }
}