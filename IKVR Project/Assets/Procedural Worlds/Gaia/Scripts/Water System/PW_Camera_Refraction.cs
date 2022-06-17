using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class PW_Camera_Refraction : MonoBehaviour
{
	private CameraEvent 	_cameraEvent 		= CameraEvent.AfterForwardOpaque;
	private CommandBuffer 	_cbuf;
	private string          _cbufName 			= "Echo_Refaction";
	private int             _grabID;
	private int             _screenWidth 	= 0;
	private int             _screenHeight 	= 0;
	private Camera          _camera;

	[System.Serializable]
	public enum PW_RENDER_SIZE
	{
		FULL 	= -1,
		HALF 	= -2,
		QUARTER = -3
	};

	 public PW_RENDER_SIZE renderSize = PW_RENDER_SIZE.HALF;

	//-------------------------------------------------------------------------
	private void CommandBufferDestroy( Camera i_cam )
	{
		if ( i_cam == null ) 
			return;

        if ( _cbuf != null) 
		{
            i_cam.RemoveCommandBuffer ( _cameraEvent, _cbuf );
            _cbuf.Clear();
            _cbuf.Dispose();
            _cbuf = null;
        }

        CommandBuffer[] commandBuffers = i_cam.GetCommandBuffers ( _cameraEvent );

        foreach ( CommandBuffer cbuf in commandBuffers ) 
		{
            if ( cbuf.name == _cbufName ) 
			{
                i_cam.RemoveCommandBuffer ( _cameraEvent, cbuf );
                cbuf.Clear();
                cbuf.Dispose();
            }
        }
	}

	//-------------------------------------------------------------------------
	private void CommandBufferCreate()
	{
		CommandBufferDestroy(_camera);

		_cbuf = new CommandBuffer();
        _cbuf.name = _cbufName;
        _cbuf.Clear();

        _cbuf.GetTemporaryRT ( _grabID, (int)renderSize, (int)renderSize, 0, FilterMode.Bilinear );
        _cbuf.Blit ( BuiltinRenderTextureType.CurrentActive, _grabID );
        _cbuf.SetGlobalTexture ( "_CameraOpaqueTexture", _grabID );

		_camera.AddCommandBuffer ( _cameraEvent, _cbuf );
	}

	//=========================================================================
	void OnPreRender() 
	{
        if ( _screenHeight != _camera.pixelHeight || _screenWidth != _camera.pixelWidth) 
		{
			_screenWidth 	= _camera.pixelWidth;
			_screenHeight 	= _camera.pixelHeight;

            CommandBufferCreate();
        }
    }

	//=========================================================================
	void OnDisable()
	{
		CommandBufferDestroy(_camera);
	}

	//=========================================================================
    void OnEnable()
    {
		_camera = GetComponent<Camera>();

		if ( _camera == null ) 
			_camera 	= Camera.main;

		_screenWidth 	= _camera.pixelWidth;
		_screenHeight 	= _camera.pixelHeight;

		CommandBufferCreate();

		_grabID 		= Shader.PropertyToID ( "_EchoTemp" );
    }
}
