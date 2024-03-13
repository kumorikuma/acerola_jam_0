using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

public class PostProcessOutline : ScriptableRendererFeature {
    [System.Serializable]
    public class PostProcessOutlineSettings {
        [Tooltip("Outline Color")] [ColorUsage(true, true)]
        public Color OutlineColor = Color.black;

        [Tooltip("Outline Blend Color")] [ColorUsage(true, true)]
        public Color OutlineBlendColor = Color.black;

        [Tooltip("Outline Second Blend Color")] [ColorUsage(true, true)]
        public Color OutlineBlendColor2 = Color.black;

        [Tooltip("Use the normal map in drawing the outline.")]
        public bool EnableNormalOutline = true;

        [Tooltip("Only draw outline when diffed against background")]
        public bool OnlyCompareToBg = false;

        [Tooltip("Crease Angle Threshold")] [Range(1, 180)]
        public float CreaseAngleThresholdDeg = 15;

        [Tooltip("Past this value, the pixel is considered to be in background.")] [Range(0, 256)]
        public float DepthThreshold = 15;

        public string ShaderName = "CustomURP/PostProcessOutline";

        [Tooltip("Where to insert or inject the Outline.")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public PostProcessOutlineSettings settings = new PostProcessOutlineSettings();
    public PostProcessOutlinePass postProcessOutlinePass;

    public override void Create() {
        postProcessOutlinePass = new PostProcessOutlinePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        postProcessOutlinePass.Setup(renderer);
        renderer.EnqueuePass(postProcessOutlinePass);
    }

    public Material GetPostProcessMaterial() {
        return postProcessOutlinePass.postProcessOutlineMaterial;
    }

    public class PostProcessOutlinePass : ScriptableRenderPass {
        private ShaderTagId[] shaderTagsList = {
            new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly")
        };

        private RTHandle rtCustomColor, rtTempColor;
        public Material postProcessOutlineMaterial;

        public PostProcessOutlinePass(PostProcessOutlineSettings settings) {
            Shader shader = Shader.Find(settings.ShaderName);
            if (shader == null) {
                Debug.LogError($"[PostProcessOutline] Couldn't find shader: {settings.ShaderName}");
                return;
            }

            this.renderPassEvent = settings.renderPassEvent;

            postProcessOutlineMaterial = new Material(Shader.Find(settings.ShaderName));
            postProcessOutlineMaterial.SetColor("_OutlineColor", settings.OutlineColor);
            postProcessOutlineMaterial.SetColor("_OutlineBlendColor", settings.OutlineBlendColor);
            postProcessOutlineMaterial.SetColor("_OutlineBlendColor2", settings.OutlineBlendColor2);
            postProcessOutlineMaterial.SetFloat("_BlendTime", 0);
            postProcessOutlineMaterial.SetFloat("_BlendTime2", 0);
            postProcessOutlineMaterial.SetFloat("_DepthThreshold",
                settings.DepthThreshold);
            postProcessOutlineMaterial.SetFloat("_CreaseAngleThreshold",
                Mathf.Deg2Rad * settings.CreaseAngleThresholdDeg);

            if (settings.EnableNormalOutline) {
                postProcessOutlineMaterial.EnableKeyword("USE_NORMAL_OUTLINE");
            } else {
                postProcessOutlineMaterial.DisableKeyword("USE_NORMAL_OUTLINE");
            }

            if (settings.OnlyCompareToBg) {
                postProcessOutlineMaterial.EnableKeyword("ONLY_BACKGROUND");
            } else {
                postProcessOutlineMaterial.DisableKeyword("ONLY_BACKGROUND");
            }
        }

        public void Setup(ScriptableRenderer renderer) {
            ConfigureInput(ScriptableRenderPassInput.Normal);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            RenderTextureDescriptor colorDesc = renderingData.cameraData.cameraTargetDescriptor;
            colorDesc.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(ref rtTempColor, colorDesc, name: "_TemporaryColorTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (postProcessOutlineMaterial == null) {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("Post Process Outline Pass");

            using (new ProfilingScope(cmd, new ProfilingSampler("Post Process Outline Pass"))) {
                RTHandle camTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

                RendererListDesc desc = new RendererListDesc(shaderTagsList, renderingData.cullResults,
                    renderingData.cameraData.camera);
                RendererList rendererList = context.CreateRendererList(desc);
                cmd.DrawRendererList(rendererList);

                Blitter.BlitTexture(cmd, camTarget, rtTempColor, postProcessOutlineMaterial, 0);
                Blitter.BlitCameraTexture(cmd, rtTempColor, camTarget);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            CommandBufferPool.Release(cmd);
        }

        public void Dispose() {
            rtTempColor?.Release();
        }
    }
}
