using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PW_Camera_Depth : MonoBehaviour
{
	//private RenderTexture 	_renTex;
	private Material 		_mat;
	private Mesh            _mesh;
	public Light 			mainLight;
	public GameObject 		waterGO;

    void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
		Vector3 lightDir;

		lightDir.x	= -mainLight.transform.forward.x;
		lightDir.y	= -mainLight.transform.forward.y;
		lightDir.z	= -mainLight.transform.forward.z;

		//_renTex = new RenderTexture(512,512,1);

		if (waterGO!=null)
			_mat = waterGO.GetComponent<Renderer>().sharedMaterial;

		if ( _mat!=null )
		{
			_mat.SetVector ("_PW_MainLightDir", -mainLight.transform.forward );
			_mat.SetVector ("_PW_MainLightColor", mainLight.color * mainLight.intensity );
		}

		Shader.SetGlobalVector ("_PW_MainLightDir", -mainLight.transform.forward );
		Shader.SetGlobalVector ("_PW_MainLightColor", mainLight.color * mainLight.intensity );
    }

	void Update()
	{
//		Matrix4x4 matx =  Matrix4x4.TRS(waterGO.transform.position, waterGO.transform.rotation, new Vector3(512,1,512));
		Vector3 lightDir;

		lightDir.x	= -mainLight.transform.forward.x;
		lightDir.y	= -mainLight.transform.forward.y;
		lightDir.z	= -mainLight.transform.forward.z;

		if (_mat!=null)
		{
			_mat.SetVector ("_PW_MainLightDir", -mainLight.transform.forward );
			_mat.SetVector ("_PW_MainLightColor", mainLight.color * mainLight.intensity );

		}

		Shader.SetGlobalVector ("_PW_MainLightDir", -mainLight.transform.forward );
		Shader.SetGlobalVector ("_PW_MainLightColor", mainLight.color * mainLight.intensity );
	}
}

