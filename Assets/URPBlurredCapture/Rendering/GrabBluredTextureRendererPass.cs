using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrabBluredTextureRendererPass : ScriptableRenderPass
{
    private const string NAME = nameof(GrabBluredTextureRendererPass);

    private Material _material = null;
    private RenderTargetIdentifier _currentTarget = default;
    private float _offset = 0;
    private float _blur = 0;

    private float[] _weights = new float[10];

    private int _blurredTempID1 = 0;
    private int _blurredTempID2 = 0;
    private int _weightsID = 0;
    private int _offsetsID = 0;
    private int _grabBlurTextureID = 0;

    public GrabBluredTextureRendererPass(Shader shader, RenderPassEvent passEvent)
    {
        renderPassEvent = passEvent;
        _material = new Material(shader);

        _blurredTempID1 = Shader.PropertyToID("_BlurTemp1");
        _blurredTempID2 = Shader.PropertyToID("_BlurTemp2");
        _weightsID = Shader.PropertyToID("_Weights");
        _offsetsID = Shader.PropertyToID("_Offsets");
        _grabBlurTextureID = Shader.PropertyToID("_GrabBlurTexture");
    }

    public void UpdateWeights()
    {
        float total = 0;
        float d = _blur * _blur * 0.001f;

        for (int i = 0; i < _weights.Length; i++)
        {
            float r = 1.0f + 2.0f * i;
            float w = Mathf.Exp(-0.5f * (r * r) / d);
            _weights[i] = w;
            if (i > 0)
            {
                w *= 2.0f;
            }

            total += w;
        }

        for (int i = 0; i < _weights.Length; i++)
        {
            _weights[i] /= total;
        }
    }

    public void SetParams(float offset, float blur)
    {
        _offset = offset;
        _blur = blur;
    }

    public void SetRenderTarget(RenderTargetIdentifier target)
    {
        _currentTarget = target;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer buf = CommandBufferPool.Get(NAME);

        ref CameraData camData = ref renderingData.cameraData;
        
        if (camData.isSceneViewCamera)
        {
            return;
        }

        RenderTextureDescriptor descriptor = camData.cameraTargetDescriptor;

        int screenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
        buf.GetTemporaryRT(screenCopyID, descriptor, FilterMode.Bilinear);

        descriptor.width /= 2;
        descriptor.height /= 2;

        buf.GetTemporaryRT(_blurredTempID1, descriptor, FilterMode.Bilinear);
        buf.GetTemporaryRT(_blurredTempID2, descriptor, FilterMode.Bilinear);

        int width = camData.camera.scaledPixelWidth;
        int height = camData.camera.scaledPixelHeight;
        float x = _offset / width;
        float y = _offset / height;
        
        buf.SetGlobalFloatArray(_weightsID, _weights);
        
        buf.Blit(_currentTarget, screenCopyID);
        Blit(buf, screenCopyID, _blurredTempID1);
        buf.ReleaseTemporaryRT(screenCopyID);
        
        for (int i = 0; i < 2; i++)
        {
            buf.SetGlobalVector(_offsetsID, new Vector4(x, 0, 0, 0));
            Blit(buf, _blurredTempID1, _blurredTempID2, _material);
        
            buf.SetGlobalVector(_offsetsID, new Vector4(0, y, 0, 0));
            Blit(buf, _blurredTempID2, _blurredTempID1, _material);
        }
        
        buf.ReleaseTemporaryRT(_blurredTempID2);

        buf.SetGlobalTexture(_grabBlurTextureID, _blurredTempID1);

        context.ExecuteCommandBuffer(buf);
        CommandBufferPool.Release(buf);
    }
}