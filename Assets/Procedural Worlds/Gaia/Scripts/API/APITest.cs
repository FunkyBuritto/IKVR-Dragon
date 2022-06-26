using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gaia;

public class APITest : MonoBehaviour
{
    [Header("World Creation Settings")]
    public int xTiles = 1;
    public int zTiles = 1;
    public int tileSize = 1024;
    public float tileHeight = 1000;
    public int seaLevel = 50;
    public bool createTerrainsInScenes = false;
    public bool autoUnloadScenes = false;
    public bool applyFloatingPointFix = false;

    public bool executeWorldCreation = true;

    [Header("Stamper Settings")]
    public Texture2D stamperTestTexture;
    public bool executeStamp = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateTerrainButton()
    {
        WorldCreationSettings worldCreationSettings = ScriptableObject.CreateInstance<WorldCreationSettings>();
        worldCreationSettings.m_xTiles = xTiles;
        worldCreationSettings.m_zTiles = zTiles;
        worldCreationSettings.m_tileSize = tileSize;
        worldCreationSettings.m_tileHeight = tileHeight;
        worldCreationSettings.m_seaLevel = seaLevel;
        worldCreationSettings.m_createInScene = createTerrainsInScenes;
        worldCreationSettings.m_autoUnloadScenes = autoUnloadScenes;
        worldCreationSettings.m_applyFloatingPointFix = applyFloatingPointFix;
        //worldCreationSettings.m_gaiaDefaults = new GaiaDefaults();
        //worldCreationSettings.m_spawnerPresetList = new List<BiomeSpawnerListEntry>() { }
        GaiaSessionManager.CreateWorld(worldCreationSettings, executeWorldCreation);
    }

    public void StampButton()
    {
        StamperSettings stamperSettings = ScriptableObject.CreateInstance<StamperSettings>();
        stamperSettings.m_width = 150;
        stamperSettings.m_imageMasks = new ImageMask[1] { new ImageMask() {
                                                                            m_operation = ImageMaskOperation.ImageMask,
                                                                            ImageMaskTexture = stamperTestTexture
                                                        }};
        stamperSettings.m_y = 25;
        GaiaSessionManager.Stamp(stamperSettings, executeStamp, null, true);
    }

}
