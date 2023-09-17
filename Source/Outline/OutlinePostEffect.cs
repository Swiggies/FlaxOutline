using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FlaxEngine;

namespace Outline
{
    public class OutlinePostEffect : PostProcessEffect
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Data
        {
            public Float4 Color;
            public Float2 TexelSize;
            public float Size;
            public float ColorSensitivity;
            public float NormalSensitivity;
            public float DepthSensitivity;
        }

        public float OutlineScale = 0.1f;
        public float ColorSensitivity = 0;
        public float NormalSensitivity = 2.5f;
        public float DepthSensitivity = 100;

        [Tooltip("Increases depth accuracy at low distances with less accuracy at high distances.")]
        public float DepthAccuracy = 8;

        private GPUPipelineState _psFullscreen;
        private Shader _shader;
        public Color Color = Color.Black;

        public Shader Shader
        {
            get => _shader;
            set
            {
                if (_shader != value)
                {
                    _shader = value;
                    ReleaseShader();
                }
            }
        }

        public override void OnEnable()
        {
#if FLAX_EDITOR
            // Register for asset reloading event and dispose resources that use shader
            Content.AssetReloading += OnAssetReloading;
#endif

            // Register postFx to game view
            MainRenderTask.Instance.AddCustomPostFx(this);
        }

#if FLAX_EDITOR
        private void OnAssetReloading(Asset asset)
        {
            // Shader will be hot-reloaded
            if (asset == Shader)
                ReleaseShader();
        }
#endif

        public override void OnDisable()
        {
            // Remember to unregister from events and release created resources (it's gamedev, not webdev)
            MainRenderTask.Instance.RemoveCustomPostFx(this);
#if FLAX_EDITOR
            Content.AssetReloading -= OnAssetReloading;
#endif
            ReleaseShader();
        }

        private void ReleaseShader()
        {
            // Release resources using shader
            Destroy(ref _psFullscreen);
        }

        public override bool CanRender()
        {
            return base.CanRender() && Shader && Shader.IsLoaded;
        }

        public override unsafe void Render(GPUContext context, ref RenderContext renderContext, GPUTexture input, GPUTexture output)
        {
            // Here we perform custom rendering on top of the in-build drawing

            // Setup missing resources
            if (!_psFullscreen)
            {
                _psFullscreen = new GPUPipelineState();
                var desc = GPUPipelineState.Description.DefaultFullscreenTriangle;
                desc.PS = Shader.GPU.GetPS("PS_Fullscreen");
                _psFullscreen.Init(ref desc);
            }

            // Set constant buffer data (memory copy is used under the hood to copy raw data from CPU to GPU memory)
            var cb = Shader.GPU.GetCB(0);
            if (cb != IntPtr.Zero)
            {
                // When using more constants create structure with `StructLayout(LayoutKind.Sequential)` attribute and pass it's address to copy data
                var data = new Data
                {
                    Color = Color,
                    TexelSize = Float2.One / input.Size,
                    Size = OutlineScale,
                    ColorSensitivity = ColorSensitivity,
                    DepthSensitivity = DepthSensitivity,
                    NormalSensitivity = NormalSensitivity,
                };
                context.UpdateCB(cb, new IntPtr(&data));
            }

            // Draw fullscreen triangle using custom Pixel Shader
            context.BindCB(0, cb);
            context.BindSR(0, input);
            context.BindSR(1, renderContext.Buffers.DepthBuffer);
            context.BindSR(2, renderContext.Buffers.GBuffer1);
            context.SetState(_psFullscreen);
            context.SetRenderTarget(output.View());
            context.DrawFullscreenTriangle();
        }
    }
}
