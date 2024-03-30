using JetBrains.Annotations;
using TLP.UdonUtils.Extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDK3.Image;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonUtils.Runtime.Common
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ImageDownloader : TlpBaseBehaviour
    {
        #region Executionorder
        protected override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.UiStart + 1;
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
        private void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (string.IsNullOrWhiteSpace(ImageUrls.ToString())) {
                Warn($"{nameof(ImageUrls)} is empty, no image will be loaded");
                return;
            }

            if (Settings == null) {
                Error($"{nameof(Settings)} invalid");
                return;
            }

            if (ImageMaterial == null) {
                Error($"{nameof(ImageMaterial)} invalid");
                return;
            }

            if (!PreviousButton) {
                Error($"{nameof(PreviousButton)} not set");
                return;
            }

            if (!NextButton) {
                Error($"{nameof(NextButton)} not set");
                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Starting download of image: {ImageUrls}");
#endif
            #endregion

            if (Utilities.IsValid(_imageDownloader)) {
                _imageDownloader.Dispose();
            }

            _imageDownloader = new VRCImageDownloader();
            StartDownload();
            SetFallbackTexture();

            PreviousButton.gameObject.SetActive(_currentImageIndex != 0);
            NextButton.gameObject.SetActive(_currentImageIndex < ImageUrls.Length - 1);
        }


        private void OnDisable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDisable));
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

        private void OnDestroy() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnDestroy));
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
        public void ShowNext() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(ShowNext));
#endif
            #endregion

            if (ImageUrls.LengthSafe() < 1) {
                Error("No URLs defined");
                return;
            }

            _currentImageIndex = Mathf.Clamp(_currentImageIndex + 1, 0, ImageUrls.Length - 1);

            DownloadOrDisplayImageForIndex();
            PreviousButton.gameObject.SetActive(_currentImageIndex != 0);
            NextButton.gameObject.SetActive(_currentImageIndex < ImageUrls.Length - 1);
        }

        /// <summary>
        /// Call from Unity UI Button
        /// </summary>
        [PublicAPI]
        public void ShowPrevious() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(ShowPrevious));
#endif
            #endregion

            if (ImageUrls.LengthSafe() < 1) {
                Error("No URLs defined");
                return;
            }

            _currentImageIndex = Mathf.Clamp(_currentImageIndex - 1, 0, ImageUrls.Length - 1);

            DownloadOrDisplayImageForIndex();
            PreviousButton.gameObject.SetActive(_currentImageIndex != 0);
            NextButton.gameObject.SetActive(_currentImageIndex < ImageUrls.Length - 1);
        }

        private void DownloadOrDisplayImageForIndex() {
            UpdateDisplayedImage(RendererFallback);
            var imageUrl = ImageUrls[_currentImageIndex];
            if (imageUrl == null) {
                Error($"Invalid URL at position {_currentImageIndex}");
                return;
            }

            var downloadedImage = _downloadedImages[imageUrl.ToString()];
            if (downloadedImage.Error != DataError.None
                || downloadedImage.TokenType != TokenType.Reference) {
                StartDownload();
                return;
            }

            var texture = (Texture2D)downloadedImage.Reference;
            if (!texture) {
                Error($"Invalid texture stored for URL: {imageUrl}");
                return;
            }

            UpdateDisplayedImage(texture);
        }


        public override void OnImageLoadSuccess(IVRCImageDownload result) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnImageLoadSuccess)} {result.Url} ({result.SizeInMemoryBytes / 1024:F3} Kb)");
#endif
            #endregion

            base.OnImageLoadSuccess(result);

            if (result.Url.ToString() != ImageUrls[_currentImageIndex].ToString()) {
                // skip as the user already switched to another image
                return;
            }

            SetDownloadedTexture(result);
        }

        private void SetDownloadedTexture(IVRCImageDownload result) {
            if (!Renderer) {
                Warn($"{nameof(Renderer)} not set, skipping property block setting");
                return;
            }

            var downloadedImage = result.Result;
            _downloadedImages[result.Url.ToString()] = downloadedImage;
            UpdateDisplayedImage(downloadedImage);
        }

        public override void OnImageLoadError(IVRCImageDownload result) {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(OnImageLoadError)} {result.Url} ({result.ErrorMessage})");
#endif
            #endregion

            base.OnImageLoadError(result);

            if (result.Url.ToString() == ImageUrls.ToString()
                && !string.IsNullOrEmpty(ImageUrls.ToString())
                && _imageDownload != null) {
                SendCustomEventDelayedSeconds(nameof(Retry), 5f);
            }

            _downloadedImages[result.Url.ToString()] = (Texture2D)null;
            SetFallbackTexture();
        }
        #endregion


        #region Delayed Events
        public void UpdateProgress() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(UpdateProgress)} of {ImageUrls}");
#endif
            #endregion

            if (_imageDownload == null
                || _imageDownload.State != VRCImageDownloadState.Pending
                || _imageDownload.Progress >= 1f) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"Download of {ImageUrls} ended");
#endif
                #endregion

                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"Download progress of {ImageUrls}: {_imageDownload.Progress * 100f:F3}%");
#endif
            #endregion

            SendCustomEventDelayedSeconds(nameof(UpdateProgress), 3f);
        }


        public void Retry() {
            if (_imageDownloader == null
                || _imageDownload == null) {
                #region TLP_DEBUG
#if TLP_DEBUG
                DebugLog($"{nameof(Retry)} cancelled");
#endif
                #endregion

                return;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog($"{nameof(Retry)} downloading {ImageUrls}");
#endif
            #endregion

            StartDownload();
        }
        #endregion

        #region Internal
        private void StartDownload() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(StartDownload));
#endif
            #endregion

            Assert(Utilities.IsValid(_imageDownloader), $"{nameof(_imageDownloader)} invalid", this);

            _imageDownload = _imageDownloader.DownloadImage(
                    ImageUrls[_currentImageIndex],
                    ImageMaterial,
                    GetComponent<UdonBehaviour>(),
                    Settings
            );
            SendCustomEventDelayedSeconds(nameof(UpdateProgress), 10f);
        }

        private void UpdateDisplayedImage(Texture2D texture) {
            var materialPropertyBlock = new MaterialPropertyBlock();
            materialPropertyBlock.SetTexture(RendererMaterialProperty, texture);
            Renderer.SetPropertyBlock(materialPropertyBlock);

            UpdateAspectRatio(texture);
        }

        private void SetFallbackTexture() {
            if (!Renderer) {
                Warn($"{nameof(Renderer)} not set, skipping property block setting");
                return;
            }

            if (RendererFallback == null) {
                Warn($"{nameof(RendererFallback)} texture not set");
                return;
            }

            UpdateDisplayedImage(RendererFallback);
        }


        private void UpdateAspectRatio(Texture2D texture) {
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