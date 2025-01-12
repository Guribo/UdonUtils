using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Experimental.Tasks;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
#else
using VRC.Udon;
#endif

namespace TLP.UdonUtils.Runtime.Pool
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(Pool), ExecutionOrder)]
    public class Pool : Task
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = ExampleEventListener.ExecutionOrder + 1;

        public bool AttachToParent = true;

        #region Configuration
        [FormerlySerializedAs("poolInstancePrefab")]
        public GameObject PoolInstancePrefab;

        [FormerlySerializedAs("activateInstances")]
        [Tooltip(
                "If true instances will be disabled upon returning to the pool and activated upon leaving the pool, if false the active state is not changed and must be maintained and checked manually by the user of the pool"
        )]
        [SerializeField]
        private bool ActivateInstances;

        [FormerlySerializedAs("limitToInitiallyCreated")]
        [Tooltip("Prevents creating additional instances by clearing the reference to the prefab during start")]
        [SerializeField]
        private bool LimitToInitiallyCreated;

        [FormerlySerializedAs("initialInstancesPrePooled")]
        [SerializeField]
        internal int InitialInstancesPrePooled = 100;

        [Tooltip("Number of instances to create per frame during the scene initialization phase")]
        [SerializeField]
        [Range(1, 50)]
        internal int InitializationsPerFrame = 10;

        /// <summary>
        /// In case a prefab is present the value is derived from the type of transform on that prefab
        /// </summary>
        [FormerlySerializedAs("usesRectTransform")]
        [SerializeField]
        private bool UsesRectTransform;

        [FormerlySerializedAs("disableAfterInitialization")]
        [SerializeField]
        private bool DisableAfterInitialization = true;

        private int _initiallyCreatedIndex;
        #endregion

        #region Monobehaviour
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            bool hasPrefab = Utilities.IsValid(PoolInstancePrefab);
            if (hasPrefab) {
                UsesRectTransform = Utilities.IsValid(PoolInstancePrefab.GetComponent<RectTransform>());
            }

            if (InitialInstancesPrePooled <= 0) {
            } else {
                _initiallyCreatedIndex = 0;

                if (!TaskScheduler.AddTaskToDefaultScheduler(this, this)) {
                    Error("Failed to add task to default scheduler");
                    return false;
                }
            }

            if (DisableAfterInitialization) {
                gameObject.SetActive(false);
            }

            return true;
        }
        #endregion

        #region Public API
        [PublicAPI]
        public int TotalCreated { get; internal set; }

        [PublicAPI]
        public int Pooled { get; internal set; }

        [PublicAPI]
        public int Index => transform.GetSiblingIndex();

        /// <summary>
        /// Gets an instance from the pool or creates a new one from the prefab
        ///
        /// Freshly created instances have their <see cref="Poolable.OnCreated"/> called,
        /// followed by <see cref="Poolable.OnReadyForUse"/>.
        ///
        /// Pooled objects simply have their <see cref="Poolable.OnReadyForUse"/> method called.
        ///
        /// Note: only calls the methods on the first UdonBehaviour found on the root gameobject of the instance
        /// </summary>
        /// <returns>null if no more instances can be created (no instance prefab provided),
        /// the unpooled/created instance otherwise</returns>
        [PublicAPI]
        public GameObject Get() {
#if TLP_DEBUG
            DebugLog(nameof(Get));
#endif
            int pooledCount = transform.childCount;

            if (pooledCount > 0) {
                return ReUseExistingEntry(pooledCount);
            }

            if (Utilities.IsValid(PoolInstancePrefab)) {
                return CreateNewEntry();
            }

            Warn("Pool is empty and no more instances can be created");
            return null;
        }


        /// <summary>
        /// Calls the method <see cref="Poolable.OnPrepareForReturnToPool"/> before returning the gameobject to the pool.
        ///
        /// Note: only calls the method on the first UdonBehaviour found on the root gameobject of the instance
        /// </summary>
        /// <param name="toReturn"></param>
        [PublicAPI]
        public void Return(GameObject toReturn) {
#if TLP_DEBUG
            DebugLog(nameof(Return));
#endif
            if (!Utilities.IsValid(toReturn)) {
                Error("Received invalid object to be returned to the pool");
                return;
            }

            if (ActivateInstances) {
                toReturn.SetActive(false);
            }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
            var instance = toReturn.gameObject.GetComponent<UdonSharpBehaviour>();
#else
            var instance = (UdonBehaviour)toReturn.gameObject.GetComponent(typeof(UdonBehaviour));
#endif

            if (!Utilities.IsValid(instance)) {
                Warn(
                        $"{toReturn.transform.GetPathInScene()} has no UdonSharpBehaviour attached and will thus not be de-initialized");
            } else {
                instance.SendCustomEvent(nameof(OnPrepareForReturnToPool));
            }

            if (UsesRectTransform) {
                ((RectTransform)toReturn.transform).SetParent(transform, false);
            } else {
                toReturn.transform.parent = transform;
            }

            if (Utilities.IsValid(instance)) {
                instance.SetProgramVariable(nameof(PoolableInUse), false);
            }

            Pooled++;
        }


        [PublicAPI]
        public bool IsEmpty() {
#if TLP_DEBUG
            DebugLog(nameof(IsEmpty));
#endif
            return transform.childCount == 0;
        }
        #endregion

        #region Internal
        public TaskResult CreateNextInitialInstance() {
#if TLP_DEBUG
            DebugLog($"{nameof(CreateNextInitialInstance)} {_initiallyCreatedIndex}");
#endif

            if (!Utilities.IsValid(PoolInstancePrefab)) {
                Error("Pool can not be prefilled: invalid prefab");
                return TaskResult.Failed;
            }

            for (int i = 0;
                 i < InitializationsPerFrame && _initiallyCreatedIndex < InitialInstancesPrePooled &&
                 TotalCreated < InitialInstancesPrePooled;
                 ++i) {
                var newInstance = Instantiate(PoolInstancePrefab);
                if (!Utilities.IsValid(newInstance)) {
                    Error("Failed to create a new instance");
                    return TaskResult.Failed;
                }

                var instances = newInstance.gameObject.GetComponents<TlpBaseBehaviour>();
                foreach (var instance in instances) {
                    if (!Utilities.IsValid(instance)) {
                        Warn($"{newInstance.name} has no UdonSharpBehaviour attached and will thus not be initialized");
                    } else {
                        instance.SetProgramVariable(nameof(Pool), this);
                        instance.SendCustomEvent(nameof(OnCreated));
                    }
                }

                if (ActivateInstances) {
                    newInstance.gameObject.SetActive(false);
                }

                if (UsesRectTransform) {
                    ((RectTransform)newInstance.transform).SetParent(transform, false);
                } else {
                    newInstance.transform.parent = transform;
                }

                TotalCreated++;
                Pooled++;
                _initiallyCreatedIndex++;
            }

            if (_initiallyCreatedIndex < InitialInstancesPrePooled && TotalCreated < InitialInstancesPrePooled) {
                if (InitialInstancesPrePooled > 0)
                    SetProgress((float)_initiallyCreatedIndex / InitialInstancesPrePooled);
                return TaskResult.Unknown;
            }


            DebugLog($"Initialized with {InitialInstancesPrePooled} instances");

            if (LimitToInitiallyCreated) {
                PoolInstancePrefab = null;
            }

            return TaskResult.Succeeded;
        }


        private GameObject CreateNewEntry() {
            var newInstance = Instantiate(PoolInstancePrefab, AttachToParent ? transform.parent : null, false);
            if (!Utilities.IsValid(newInstance)) {
                Error("Failed to create a new instance");
                return null;
            }

            var result = newInstance.gameObject;

            var instances = result.GetComponents<TlpBaseBehaviour>();
            foreach (var instance in instances) {
                if (!Utilities.IsValid(instance)) {
                    Warn($"{newInstance.name} has no UdonSharpBehaviour attached and will thus not be initialized");
                } else {
                    InitializeOnCreation(instance);
                }
            }

            if (ActivateInstances) {
                result.SetActive(true);
            }

            TotalCreated++;
            return result;
        }


        private GameObject ReUseExistingEntry(int pooledCount) {
            var toUnPool = transform.GetChild(pooledCount - 1);
            if (!Utilities.IsValid(toUnPool)) {
                Error("Failed to get an instance from the pool");
                return null;
            }

            if (AttachToParent) {
                if (UsesRectTransform) {
                    ((RectTransform)toUnPool.transform).SetParent(transform.parent, false);
                } else {
                    toUnPool.parent = transform.parent;
                }
            } else {
                if (UsesRectTransform) {
                    ((RectTransform)toUnPool.transform).SetParent(null, false);
                } else {
                    toUnPool.parent = null;
                }
            }

            var result = toUnPool.gameObject;

            var instances = toUnPool.gameObject.GetComponents<TlpBaseBehaviour>();
            foreach (var instance in instances) {
                if (!Utilities.IsValid(instance)) {
                    Warn(
                            $"{toUnPool.name} has no {nameof(TlpBaseBehaviour)} attached and will thus not be initialized"
                    );
                } else {
                    InitializeOnReUse(instance);
                }
            }

            if (ActivateInstances) {
                result.SetActive(true);
            }

            Pooled--;
            return toUnPool.gameObject;
        }

        private void InitializeOnCreation(TlpBaseBehaviour behaviour) {
            behaviour.Pool = this;
            behaviour.PoolableInUse = true;
            behaviour.OnCreated();
            behaviour.OnReadyForUse();
        }

        private void InitializeOnReUse(TlpBaseBehaviour behaviour) {
            behaviour.Pool = this;
            behaviour.PoolableInUse = true;
            behaviour.OnReadyForUse();
        }
        #endregion

        #region Event Handling
        public override void OnEvent(string eventName) {
            switch (eventName) {
                case "OnTaskFinished":
#if TLP_DEBUG
                    DebugLog_OnEvent(eventName);
#endif
                    Info("Pool pre-filling completed");
                    break;
                default:
                    base.OnEvent(eventName);
                    break;
            }
        }
        #endregion

        #region Task Implementation
        protected override TaskResult DoTask(float stepDeltaTime) {
            #region TLP_DEBUG
#if TLP_DEBUG
        DebugLog($"{nameof(DoTask)}: {nameof(stepDeltaTime)}={stepDeltaTime}");
#endif
            #endregion
            return CreateNextInitialInstance();
        }

        public override int GetNeededSteps() {
            return InitialInstancesPrePooled / Mathf.Max(1, InitializationsPerFrame);
        }

        protected override bool InitTask() {
            return true;
        }
        #endregion
    }
}