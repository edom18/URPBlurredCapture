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
    private RenderTargetIdentifier _resultTarget = default;
    private float _offset = 0;
    private float _blur = 0;

    private float[] _weights = new float[10];

    private int _blurredID1 = 0;
    private int _blurredID2 = 0;
    private int _weightsID = 0;
    private int _offsetsID = 0;
    private int _grabBlurTextureID = 0;

    public GrabBluredTextureRendererPass(Shader shader, RenderPassEvent passEvent)
    {
        renderPassEvent = passEvent;
        _material = new Material(shader);
        
        _resultTarget = new RenderTargetIdentifier(BlurRendererFeature.BlurredResult);

        _blurredID1 = Shader.PropertyToID("_BlurTemp1");
        _blurredID2 = Shader.PropertyToID("_BlurTemp2");
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
        context.ExecuteCommandBuffer(buf);
        buf.Clear();

        // ref CameraData camData = ref renderingData.cameraData;
        CameraData camData = renderingData.cameraData;

        int width = camData.camera.scaledPixelWidth;
        int height = camData.camera.scaledPixelHeight;

        var target = (camData.targetTexture != null) ? new RenderTargetIdentifier(camData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
        
        int hw = width;// / 2;
        int hh = height;// / 2;
        
        // 半分の解像度で2枚のRender Textureを生成
        // buf.GetTemporaryRT(_blurredID1, hw, hh, 0, FilterMode.Bilinear);
        buf.GetTemporaryRT(_blurredID2, hw, hh, 0, FilterMode.Bilinear);
        
        // // 半分にスケールダウンしてコピー
        buf.Blit(target, _resultTarget);
        
        float x = _offset / width;
        float y = _offset / height;
        
        buf.SetGlobalFloatArray(_weightsID, _weights);
        
        // 横方向のブラー
        buf.SetGlobalVector(_offsetsID, new Vector4(x, 0, 0, 0));
        buf.Blit(_resultTarget, _blurredID2, _material);
        
        // 縦方向のブラー
        buf.SetGlobalVector(_offsetsID, new Vector4(0, y, 0, 0));
        buf.Blit(_blurredID2, _resultTarget, _material);
        
        buf.SetGlobalTexture(_grabBlurTextureID, _resultTarget);
        
        buf.SetRenderTarget(_currentTarget);

        context.ExecuteCommandBuffer(buf);
        CommandBufferPool.Release(buf);
    }
}