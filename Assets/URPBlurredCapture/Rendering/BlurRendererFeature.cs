using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BlurRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader _shader = null;
    [SerializeField, Range(1f, 100f)] private float _offset = 1f;
    [SerializeField, Range(10f, 1000f)] private float _blur = 100f;
    [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

    public static RenderTexture BlurredResult = null;
    
    private GrabBluredTextureRendererPass _grabBluredTexturePass = null;

    public override void Create()
    {
        Debug.Log("Create Blur Renderer Feature.");

        if (_grabBluredTexturePass == null)
        {
            BlurredResult = new RenderTexture(Screen.width / 2, Screen.height / 2, 0);
            BlurredResult.name = "Blurred Result";
            BlurredResult.Create();
            
            _grabBluredTexturePass = new GrabBluredTextureRendererPass(_shader, _renderPassEvent);
            _grabBluredTexturePass.SetParams(_offset, _blur);
            _grabBluredTexturePass.UpdateWeights();
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _grabBluredTexturePass.SetRenderTarget(renderer.cameraColorTarget);
        _grabBluredTexturePass.SetParams(_offset, _blur);
        renderer.EnqueuePass(_grabBluredTexturePass);
    }
}