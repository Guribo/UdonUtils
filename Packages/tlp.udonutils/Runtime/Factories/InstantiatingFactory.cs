﻿using TLP.UdonUtils.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace TLP.UdonUtils.Factories
{
    /// <summary>
    /// Factory that creates new clones of a provided GameObject with <see cref="UdonSharpBehaviour"/> on it.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    public class InstantiatingFactory : TlpFactory
    {
        [Tooltip("What type of Component with GameObject to instantiate")]
        public UdonSharpBehaviour Prototype;

        #region Hook Implementations
        /// <returns>Type name of the product to be created.
        /// If the <see cref="FactoryKey"/> is set it is returned instead.
        /// Returns null if the prototype is not set</returns>
        protected override string GetProductTypeName() {
            if (!string.IsNullOrWhiteSpace(FactoryKey)) {
                return base.GetProductTypeName();
            }

            if (!Utilities.IsValid(Prototype)) {
                return null;
            }

            string udonTypeName = Prototype.GetUdonTypeName();
            FactoryKey = "";
            return UdonCommon.UdonTypeNameShort(udonTypeName);
        }

        /// <returns>a completely new instance, returns null if <see cref="Prototype"/> is not set</returns>
        protected override GameObject ProduceInstance() {
            if (Utilities.IsValid(Prototype)) {
                return Instantiate(Prototype.gameObject);
            }

            Error($"{nameof(Prototype)} not set");
            return null;
        }


        protected override bool SetupAndValidate() {
            if (Utilities.IsValid(Prototype)) {
                return base.SetupAndValidate();
            }

            Error($"{nameof(Prototype)} is not set");
            return false;
        }
        #endregion
    }
}