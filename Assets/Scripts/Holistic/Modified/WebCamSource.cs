using Mediapipe.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//* TODO: Android patch.
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace HardCoded.VRigUnity
{
    public class WebCamSource : ImageSource
    {
        private const string _TAG = nameof(WebCamSource);
        private static readonly object _PermissionLock = new();
        private static bool _IsPermitted = false;

        /// <summary>
        /// Default resolutions
        /// </summary>
        public List<ResolutionStruct> AvailableResolutions => availableResolutions;
        public List<ResolutionStruct> availableResolutions = new List<ResolutionStruct>();
        //public readonly ResolutionStruct[] AvailableResolutions = new ResolutionStruct[] {
        //	new(1920, 1080, 30),
        //	new(1600, 896, 30),
        //	new(1280, 720, 30),
        //	new(960, 540, 30),
        //	new(848, 480, 30),
        //	new(640, 480, 30),
        //	new(640, 360, 30),
        //	new(424, 240, 30),
        //	//* TODO: Android patch.
        //	// new(320, 180, 30), // 320x240 -> 16:19 better
        //	new(320, 180, 10), // 320x240 -> 16:19 better
        //	new(176, 144, 30),
        //};
        // public ResolutionStruct DefaultResolution => AvailableResolutions[6];
        public ResolutionStruct DefaultResolution => AvailableResolutions.Find(obj => true);


        public override Texture CurrentTexture => webCamTexture;
        public override int TextureWidth => IsPrepared ? webCamTexture.width : 0;
        public override int TextureHeight => IsPrepared ? webCamTexture.height : 0;
        public override bool IsPrepared => webCamTexture != null;
        public override bool IsPlaying => webCamTexture != null && webCamTexture.isPlaying;
        public override bool IsVerticallyFlipped { get => IsPrepared && webCamTexture.videoVerticallyMirrored; set { } }
        public override RotationAngle Rotation => IsPrepared ? (RotationAngle)webCamTexture.videoRotationAngle : RotationAngle.Rotation0;
        public override string SourceName => (webCamDevice is WebCamDevice valueOfWebCamDevice) ? valueOfWebCamDevice.name : null;
        public override string[] SourceCandidateNames => availableSources?.Select(device => device.name).ToArray();

        private WebCamDevice? _webCamDevice;
        private WebCamDevice? webCamDevice
        {
            get => _webCamDevice;
            set
            {
                if (_webCamDevice is WebCamDevice valueOfWebCamDevice)
                {
                    if (value is WebCamDevice valueOfValue && valueOfValue.name == valueOfWebCamDevice.name)
                    {
                        // not changed
                        return;
                    }
                }
                else if (value == null)
                {
                    // not changed
                    return;
                }
                _webCamDevice = value;
                Resolution = DefaultResolution;
            }
        }

        private WebCamDevice[] _availableSources;
        private WebCamDevice[] availableSources
        {
            get
            {
                if (_availableSources == null)
                {
                    _availableSources = WebCamTexture.devices;
                }

                return _availableSources;
            }
            set => _availableSources = value;
        }

        private WebCamTexture webCamTexture;
        private bool isInitialized;

        protected virtual IEnumerator Start()
        {
            yield return UpdateSources();
        }

        public override ResolutionStruct[] GetResolutions()
        {
            return AvailableResolutions.ToArray();
        }

        public IEnumerator UpdateSources()
        {
            yield return GetPermission();

            if (!_IsPermitted)
            {
                isInitialized = true;
                yield break;
            }

            availableSources = WebCamTexture.devices;
            availableResolutions.Clear();
            foreach (var device in availableSources)
            {
                foreach (Resolution resolution in device.availableResolutions)
                {
                    availableResolutions.Add(new ResolutionStruct(resolution, device.isFrontFacing));
                }
            }

#if UNITY_ANDROID || UNITY_IOS
            if (availableSources != null && availableSources.Length > 1)
            {
                webCamDevice = availableSources[1];
            }
            else
            {
                if (availableSources != null && availableSources.Length > 0)
                {
                    webCamDevice = availableSources[0];
                }
            }
#elif UNITY_EDITOR
			if (availableSources != null && availableSources.Length > 0) {
                webCamDevice = availableSources[0];
            }
#endif

            isInitialized = true;
        }

        private IEnumerator GetPermission()
        {
            lock (_PermissionLock)
            {
                if (_IsPermitted)
                {
                    yield break;
                }

                //* TODO: Android patch.
#if UNITY_ANDROID
                if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    Permission.RequestUserPermission(Permission.Camera);
                    yield return new WaitForSeconds(0.1f);
                }
#elif UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) {
          yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }
#endif

#if UNITY_ANDROID
                if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    Logger.Warning(_TAG, "Not permitted to use Camera");
                    yield break;
                }
#elif UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) {
          Logger.Warning(_TAG, "Not permitted to use WebCam");
          yield break;
        }
#endif

                _IsPermitted = true;

                yield return new WaitForEndOfFrame();
            }
        }

        public virtual void SelectSource(int sourceId)
        {
            if (sourceId < 0 || sourceId >= availableSources.Length)
            {
                throw new ArgumentException($"Invalid source ID: {sourceId}");
            }

            webCamDevice = availableSources[sourceId];
        }

        public override IEnumerator Play()
        {
            yield return new WaitUntil(() => isInitialized);
            if (!_IsPermitted)
            {
                throw new InvalidOperationException("Not permitted to access cameras");
            }

            InitializeWebCamTexture();
            webCamTexture.Play();
            yield return WaitForWebCamTexture();
        }

        public override void UpdateFromSettings()
        {
            SelectSourceFromName(Settings.CameraName);
            SelectResolutionFromString(Settings.CameraResolution);
            IsHorizontallyFlipped = Settings.CameraFlipped;
        }

        public override void Stop()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
            }
            webCamTexture = null;
        }

        private void InitializeWebCamTexture()
        {
            Logger.Verbose("call InitializeWebCamTexture()");
            Stop();
            if (webCamDevice is WebCamDevice valueOfWebCamDevice)
            {
                //* TODO: Android patch test.
                Logger.Verbose($"valueOfWebCamDevice.name: {valueOfWebCamDevice.name}");
                webCamTexture = new WebCamTexture(valueOfWebCamDevice.name, Resolution.width, Resolution.height, (int)Resolution.frameRate);
                //webCamTexture = new WebCamTexture("Camera 1", Resolution.width, Resolution.height, (int)Resolution.frameRate);
                Logger.Verbose($"webCamTexture: {webCamTexture}");
                Logger.Verbose($"webCamTexture.videoRotationAngle: {webCamTexture.videoRotationAngle}");
                return;
            }

            throw new InvalidOperationException("Cannot initialize WebCamTexture because WebCamDevice is not selected");
        }

        private IEnumerator WaitForWebCamTexture()
        {
            const int timeoutFrame = 30;
            var count = 0;
            Logger.Verbose("Waiting for WebCamTexture to start");
            yield return new WaitUntil(() => count++ > timeoutFrame || webCamTexture.width > 16);

            if (webCamTexture.width <= 16)
            {
                throw new TimeoutException("Failed to start WebCam");
            }
        }

        // Custom
        public int SelectSourceFromName(string name)
        {
            int index = SourceCandidateNames.ToList().FindIndex(source => source == name);
            if (index >= 0)
            {
                SelectSource(index);
            }

            return index;
        }

        public int SelectResolutionFromString(string text, bool allowCustom = true)
        {
            int index = AvailableResolutions.ToList().FindIndex(option => option.ToString() == text);
            if (index >= 0)
            {
                SelectResolution(index);
            }
            else if (allowCustom)
            {
                Resolution = SettingsUtil.GetResolution(text);
            }

            return index;
        }

        public void SelectResolution(int resolutionId)
        {
            var resolutions = AvailableResolutions;
            if (resolutionId < 0 || resolutionId >= resolutions.Count)
            {
                throw new ArgumentException($"Invalid resolution ID: {resolutionId}");
            }

            Resolution = resolutions[resolutionId];
        }
    }
}
