using UnityEngine;

namespace HardCoded.VRigUnity {
	public class SolutionUtils {
		private static HolisticSolution _solution;
		private static AssetManager _assetManager;
		//* TODO: Android patch.
		private static StreamingAssetsResourceManager _streamingAssetResourceManager;
		private static WebCamSource _webCamSource;
		private static Bootstrap _bootstrap;

		public static HolisticSolution GetSolution() {
			if (_solution == null) {
				GameObject gameObject = GameObject.FindGameObjectWithTag("Solution");
				if (gameObject == null) {
					Debug.Log("gameObject is null.");
					return null;
				}

				_solution = gameObject.GetComponent<HolisticSolution>();
			}

			return _solution;
		}
		
		public static Bootstrap GetBootstrap() {
			if (_bootstrap == null) {
				_bootstrap = GetSolution().GetComponent<Bootstrap>();
			}

			return _bootstrap;
		}

		// TODO: In the future image sources might not always be the webcam
		public static WebCamSource GetImageSource() {
			if (_webCamSource == null) {
				_webCamSource = GetSolution().GetComponent<WebCamSource>();
			}

			return _webCamSource;
		}

		//* TODO: Android patch.
		public static StreamingAssetsResourceManager GetStreamingAssetsResourceManager() {
			if (_streamingAssetResourceManager == null) {
				_streamingAssetResourceManager = GetBootstrap().GetStreamingAssetsResourceManager();
			}

			return _streamingAssetResourceManager;
		}

		public static AssetManager GetAssetManager() {
			if (_assetManager == null) {
				_assetManager = GetBootstrap().GetAssetManager();
			}

			return _assetManager;
		}
	}
}
