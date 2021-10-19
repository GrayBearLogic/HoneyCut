using UnityEngine;
using UnityEngine.Rendering;


namespace Obi
{

	/**
	 * High-quality fluid rendering, supports both 2D and 3D. Performs depth testing against the scene, 
	 * considers reflection, refraction, lighting, transmission, and foam.
	 */
	public class ObiFluidRenderer : ObiBaseFluidRenderer
	{
	
		[Range(0,0.1f)]
		public float blurRadius = 0.02f;
	
		[Range(0.01f,2)]
		public float thicknessCutoff = 1.2f;
	
		private Material depthBlurMaterial;
		private Material normalReconstructMaterial;
		private Material thicknessMaterial;
		private readonly Color thicknessBufferClear = new Color(1,1,1,0); /**< clears alpha to black (0 thickness) and color to white.*/
	
		public Material colorMaterial;
		public Material fluidMaterial;

        protected override void Setup()
        {

            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;

            if (depthBlurMaterial == null)
            {
                depthBlurMaterial = CreateMaterial(Shader.Find("Hidden/ScreenSpaceCurvatureFlow"));
            }

            if (normalReconstructMaterial == null)
            {
                normalReconstructMaterial = CreateMaterial(Shader.Find("Hidden/NormalReconstruction"));
            }

            if (thicknessMaterial == null)
            {
                thicknessMaterial = CreateMaterial(Shader.Find("Hidden/FluidThickness"));
            }

            var shadersSupported = depthBlurMaterial && normalReconstructMaterial && thicknessMaterial;

            if (!shadersSupported ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth) ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
            )
            {
                enabled = false;
                Debug.LogWarning("Obi Fluid Renderer not supported in this platform.");
                return;
            }

            Shader.SetGlobalMatrix("_Camera_to_World", currentCam.cameraToWorldMatrix);
            Shader.SetGlobalMatrix("_World_to_Camera", currentCam.worldToCameraMatrix);
            Shader.SetGlobalMatrix("_InvProj", currentCam.projectionMatrix.inverse);

            var fovY = currentCam.fieldOfView;
            var far = currentCam.farClipPlane;
            var y = currentCam.orthographic
                ? 2 * currentCam.orthographicSize
                : 2 * Mathf.Tan(fovY * Mathf.Deg2Rad * 0.5f) * far;
            var x = y * currentCam.aspect;
            Shader.SetGlobalVector("_FarCorner", new Vector3(x, y, far));

            depthBlurMaterial.SetFloat("_BlurScale",
                currentCam.orthographic
                    ? 1
                    : currentCam.pixelWidth / currentCam.aspect * (1.0f / Mathf.Tan(fovY * Mathf.Deg2Rad * 0.5f)));
            depthBlurMaterial.SetFloat("_BlurRadiusWorldspace", blurRadius);

            if (fluidMaterial != null)
            {
                fluidMaterial.SetFloat("_ThicknessCutoff", thicknessCutoff);
            }
        }

        protected override void Cleanup()
		{
			if (depthBlurMaterial != null)
				DestroyImmediate (depthBlurMaterial);
			if (normalReconstructMaterial != null)
				DestroyImmediate (normalReconstructMaterial);
			if (thicknessMaterial != null)
				DestroyImmediate (thicknessMaterial);
		}
	
		public override void UpdateFluidRenderingCommandBuffer()
		{
			renderFluid.Clear();
	
			if (particleRenderers == null || fluidMaterial == null)
				return;
			
			var refraction = Shader.PropertyToID("_Refraction");
			var foam = Shader.PropertyToID("_Foam");
			var depth = Shader.PropertyToID("_FluidDepthTexture");
	
			var thickness1 = Shader.PropertyToID("_FluidThickness1");
			var thickness2 = Shader.PropertyToID("_FluidThickness2");
	
			var smoothDepth = Shader.PropertyToID("_FluidSurface");
	
			var normals = Shader.PropertyToID("_FluidNormals");
	
			// refraction (background), foam and fluid depth buffers:
			renderFluid.GetTemporaryRT(refraction,-1,-1,0,FilterMode.Bilinear);
			renderFluid.GetTemporaryRT(foam,-1,-1,0,FilterMode.Bilinear);
			renderFluid.GetTemporaryRT(depth,-1,-1,24,FilterMode.Point,RenderTextureFormat.Depth);
	
			// thickness/color, surface depth and normals buffers:
			renderFluid.GetTemporaryRT(thickness1,-2,-2,16,FilterMode.Bilinear,RenderTextureFormat.ARGBHalf);
			renderFluid.GetTemporaryRT(thickness2,-2,-2,0,FilterMode.Bilinear,RenderTextureFormat.ARGBHalf);
			renderFluid.GetTemporaryRT(smoothDepth,-1,-1,0,FilterMode.Point,RenderTextureFormat.RFloat);
			renderFluid.GetTemporaryRT(normals,-1,-1,0,FilterMode.Bilinear,RenderTextureFormat.ARGBHalf);
	
			// Copy screen contents to refract them later.
			renderFluid.Blit (BuiltinRenderTextureType.CurrentActive, refraction);
	
			renderFluid.SetRenderTarget(depth); // fluid depth
			renderFluid.ClearRenderTarget(true,true,Color.clear); //clear 
			
			// draw fluid depth texture:
			foreach(var renderer in particleRenderers){
				if (renderer != null){
					foreach(var mesh in renderer.ParticleMeshes){
						if (renderer.ParticleMaterial != null)
							renderFluid.DrawMesh(mesh,Matrix4x4.identity,renderer.ParticleMaterial,0,0);
					}
				}
			}
	
			// draw fluid thickness and color:
			renderFluid.SetRenderTarget(thickness1);
			renderFluid.ClearRenderTarget(true,true,thicknessBufferClear); 
	
			foreach(var renderer in particleRenderers){
				if (renderer != null){
	
					renderFluid.SetGlobalColor("_ParticleColor",renderer.particleColor);
					renderFluid.SetGlobalFloat("_RadiusScale",renderer.radiusScale);
	
					foreach(var mesh in renderer.ParticleMeshes){
						renderFluid.DrawMesh(mesh,Matrix4x4.identity,thicknessMaterial,0,0);
						renderFluid.DrawMesh(mesh,Matrix4x4.identity,colorMaterial,0,0);
					}
				}
			}
	
			// blur fluid thickness:
			renderFluid.Blit(thickness1,thickness2,thicknessMaterial,1);
			renderFluid.Blit(thickness2,thickness1,thicknessMaterial,2);
	
			// draw foam: 
			renderFluid.SetRenderTarget(foam);
			renderFluid.ClearRenderTarget(true,true,Color.clear);
	
			foreach(var renderer in particleRenderers){
				if (renderer != null){
					var foamGenerator = renderer.GetComponent<ObiFoamGenerator>();
					if (foamGenerator != null && foamGenerator.advector != null && foamGenerator.advector.Particles != null){
						var psRenderer = foamGenerator.advector.Particles.GetComponent<ParticleSystemRenderer>();
						if (psRenderer != null)
							renderFluid.DrawRenderer(psRenderer,psRenderer.material);
					}
				}
			}
			
			// blur fluid surface:
			renderFluid.Blit(depth,smoothDepth,depthBlurMaterial);
			renderFluid.ReleaseTemporaryRT(depth);
	
			// reconstruct normals from smoothed depth:
			renderFluid.Blit(smoothDepth,normals,normalReconstructMaterial);
			
			// render fluid:
			renderFluid.SetGlobalTexture("_FluidDepth", depth);
			renderFluid.SetGlobalTexture("_Foam", foam);
			renderFluid.SetGlobalTexture("_Refraction", refraction);
			renderFluid.SetGlobalTexture("_Thickness",thickness1);
			renderFluid.SetGlobalTexture("_Normals",normals);
			renderFluid.Blit(smoothDepth,BuiltinRenderTextureType.CameraTarget,fluidMaterial);	
	
		}	
	
	}
}

