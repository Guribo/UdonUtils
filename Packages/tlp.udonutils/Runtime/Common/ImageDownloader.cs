using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.DesignPatterns.MVC;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDK3.Image;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonUtils.Runtime.Common
{
    [DisallowMultipleComponent]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(ImageDownloader), ExecutionOrder)]
    public class ImageDownloader : Controller
    {
        #region Executionorder
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = Controller.ExecutionOrder + 1;
        #endregion

        [SerializeField]
        private VRCUrl[] ImageUrls;

        [SerializeField]
        private Material ImageMaterial;

        [SerializeField]
        private TextureInfo Settings;

        [SerializeField]
        public Button NextButton;

        [SerializeField]
        public Button PreviousButton;

        #region Renderer
        [Header("Optional Renderer")]
        [SerializeField]
        private Renderer Renderer;

        [SerializeField]
        private bool RendererAutoAspectRatio = true;

        [Tooltip(
                "Adjusts the X scale (width) and the Y scale (height) of the renderer GameObject depending on the here defined limits, but only if RendererAutoAspectRatio is set to true"
        )]
        [SerializeField]
        private Vector2 RendererAspectRatioScaleLimits = Vector2.one;

        [SerializeField]
        private string RendererMaterialProperty = "_MainTex";

        [SerializeField]
        private Texture2D RendererFallback;
        #endregion

        #region State
        private int _currentImageIndex;

        private VRCImageDownloader _imageDownloader;
        private IVRCImageDownload _imageDownload;

        // map of URLs to Texture2Ds
        private readonly DataDictionary _downloadedImages = new DataDictionary();
        #endregion

        #region Udon Lifecycle
        internal void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (string.IsNullOrWhiteSpace(ImageUrls.ToString())) {
                _Warn($"{nameof(ImageUrls)} is empty, no image will be loaded");
                return;
            }

            if (Settings == null) {
                _Error($"{nameof(Settings)} invalid");
                return;
            }

            if (ImageMaterial == null) {
                _Error($"{nameof(ImageMaterial)} invalid");
                return;
            }

            if (!PreviousButton) {
                _Error($"{nameof(PreviousButton)} not set");
                return;
            }

            if (!NextButton) {
                _Error($"{nameof(NextButton)} not set");
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"Starting download of image: {ImageUrls}");
#endif
            #endregion

            if (Utilities.IsValid(_imageDownloader)) {
                _imageDownloader.Dispose();
            }

            _imageDownloader = new VRCImageDownloader();
            _StartDownload();
            _SetFallbackTexture();

            PreviousButton.gameObject.SetActive(_currentImageIndex != 0);
            NextButton.gameObject.SetActive(_currentImageIndex < ImageUrls.Length - 1);
        }
        
        internal void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDisable));
#endif
            #endregion

            if (_imageDownloader == null) {
                return;
            }

            _imageDownloader.Dispose();
            _imageDownloader = null;
            _imageDownload = null; // already implicitly invalidated by Dispose()
            _downloadedImages.Clear();
        }

        protected override void OnDestroy() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(OnDestroy));
#endif
            #endregion

            OnDisable();
        }
        #endregion

        #region Callbacks
        /// <summary>
        /// Call from Unity UI Button
        /// </summary>
        [PublicAPI]
        public void _ShowNext() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_ShowNext));
#endif
            #endregion

            if (ImageUrls._LengthSafe() < 1) {
                _Error("No URLs defined");
                return;
            }

            _currentImageIndex = Mathf.Clamp(_currentImageIndex + 1, 0, ImageUrls.Length - 1);

            _DownloadOrDisplayImageForIndex();
            PreviousButton.gameObject.SetActive(_currentImageIndex != 0);
            NextButton.gameObject.SetActive(_currentImageIndex < ImageUrls.Length - 1);
        }

        /// <summary>
        /// Call from Unity UI Button
        /// </summary>
        [PublicAPI]
        public void _ShowPrevious() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_ShowPrevious));
#endif
            #endregion

            if (ImageUrls._LengthSafe() < 1) {
                _Error("No URLs defined");
                return;
            }

            _currentImageIndex = Mathf.Clamp(_currentImageIndex - 1, 0, ImageUrls.Length - 1);

            _DownloadOrDisplayImageForIndex();
            PreviousButton.gameObject.SetActive(_currentImageIndex != 0);
            NextButton.gameObject.SetActive(_currentImageIndex < ImageUrls.Length - 1);
        }

        private void _DownloadOrDisplayImageForIndex() {
            _UpdateDisplayedImage(RendererFallback);
            var imageUrl = ImageUrls[_currentImageIndex];
            if (imageUrl == null) {
                _Error($"Invalid URL at position {_currentImageIndex}");
                return;
            }

            var downloadedImage = _downloadedImages[imageUrl.ToString()];
            if (downloadedImage.Error != DataError.None
                || downloadedImage.TokenType != TokenType.Reference) {
                _StartDownload();
                return;
            }

            var texture = (Texture2D)downloadedImage.Reference;
            if (!texture) {
                _Error($"Invalid texture stored for URL: {imageUrl}");
                return;
            }

            _UpdateDisplayedImage(texture);
        }


        public override void OnImageLoadSuccess(IVRCImageDownload result) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnImageLoadSuccess)} {result.Url} ({result.SizeInMemoryBytes / 1024:F3} Kb)");
#endif
            #endregion

            base.OnImageLoadSuccess(result);

            if (result.Url.ToString() != ImageUrls[_currentImageIndex].ToString()) {
                // skip as the user already switched to another image
                return;
            }

            _SetDownloadedTexture(result);
        }

        private void _SetDownloadedTexture(IVRCImageDownload result) {
            if (!Renderer) {
                _Warn($"{nameof(Renderer)} not set, skipping property block setting");
                return;
            }

            var downloadedImage = result.Result;
            _downloadedImages[result.Url.ToString()] = downloadedImage;
            _UpdateDisplayedImage(downloadedImage);
        }

        public override void OnImageLoadError(IVRCImageDownload result) {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(OnImageLoadError)} {result.Url} ({result.ErrorMessage})");
#endif
            #endregion

            base.OnImageLoadError(result);

            if (result.Url.ToString() == ImageUrls.ToString()
                && !string.IsNullOrEmpty(ImageUrls.ToString())
                && _imageDownload != null) {
                SendCustomEventDelayedSeconds(nameof(_Retry), 5f);
            }

            _downloadedImages[result.Url.ToString()] = (Texture2D)null;
            _SetFallbackTexture();
        }
        #endregion

        #region Delayed Events
        public void _UpdateProgress() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_UpdateProgress)} of {ImageUrls}");
#endif
            #endregion

            if (_imageDownload == null
                || _imageDownload.State != VRCImageDownloadState.Pending
                || _imageDownload.Progress >= 1f) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"Download of {ImageUrls} ended");
#endif
                #endregion

                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"Download progress of {ImageUrls}: {_imageDownload.Progress * 100f:F3}%");
#endif
            #endregion

            SendCustomEventDelayedSeconds(nameof(_UpdateProgress), 3f);
        }


        public void _Retry() {
            if (_imageDownloader == null
                || _imageDownload == null) {
                #region TLP_DEBUG
#if TLP_DEBUG
                _DebugLog($"{nameof(_Retry)} cancelled");
#endif
                #endregion

                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog($"{nameof(_Retry)} downloading {ImageUrls}");
#endif
            #endregion

            _StartDownload();
        }
        #endregion

        #region Internal
        private void _StartDownload() {
            #region TLP_DEBUG
#if TLP_DEBUG
            _DebugLog(nameof(_StartDownload));
#endif
            #endregion

            if (!Utilities.IsValid(_imageDownloader)) {
                _Error($"{nameof(_imageDownloader)} invalid");
                return;
            }

            _imageDownload = _imageDownloader.DownloadImage(
                    ImageUrls[_currentImageIndex],
                    ImageMaterial,
                    GetComponent<UdonBehaviour>(),
                    Settings
            );
            SendCustomEventDelayedSeconds(nameof(_UpdateProgress), 10f);
        }

        private void _UpdateDisplayedImage(Texture2D texture) {
            var materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetTexture(RendererMaterialProperty, texture);
            Renderer.SetPropertyBlock(materialPropertyBlock);

            _UpdateAspectRatio(texture);
        }

        private void _SetFallbackTexture() {
            if (!Renderer) {
                _Warn($"{nameof(Renderer)} not set, skipping property block setting");
                return;
            }

            if (RendererFallback == null) {
                _Warn($"{nameof(RendererFallback)} texture not set");
                return;
            }

            _UpdateDisplayedImage(RendererFallback);
        }


        private void _UpdateAspectRatio(Texture2D texture) {
            if (!RendererAutoAspectRatio || texture == null) {
                return;
            }

            float height = RendererAspectRatioScaleLimits.y;
            float width = RendererAspectRatioScaleLimits.x;
            var rendererTransform = Renderer.transform;
            if (texture.height > texture.width && texture.height > 0) {
                width = height * (texture.width / (float)texture.height);
                rendererTransform.localScale = new Vector3(width, height, rendererTransform.localScale.z);
                return;
            }

            if (texture.width > 0) {
                height = width * (texture.height / (float)texture.width);
                rendererTransform.localScale = new Vector3(width, height, rendererTransform.localScale.z);
            }
        }
        #endregion
    }
}