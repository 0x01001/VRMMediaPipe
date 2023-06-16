using System;
using UnityEngine;

namespace HardCoded.VRigUnity {
	[Serializable]
	public struct ResolutionStruct {
		public int width;
		public int height;
		public double frameRate;
		public bool isFront;
		
		public ResolutionStruct(int width, int height, double frameRate, bool isFront) {
			this.width = width;
			this.height = height;
			this.frameRate = frameRate;
            this.isFront = isFront;
        }

		public ResolutionStruct(Resolution resolution, bool isFront) {
			width = resolution.width;
			height = resolution.height;
			frameRate = resolution.refreshRate;
			this.isFront = isFront;

        }

		public Resolution ToResolution() {
			return new() { width = width, height = height, refreshRate = (int)frameRate };
		}

		public override string ToString() {
			var aspectRatio = $"{width}x{height}";
			var frameRateStr = frameRate.ToString("#.##");
			return frameRate > 0 ? $"{aspectRatio} ({frameRateStr}Hz)" : aspectRatio;
		}
	}
}
