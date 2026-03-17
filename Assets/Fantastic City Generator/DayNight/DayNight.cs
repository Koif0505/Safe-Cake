using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Unity.VisualScripting;

public class DayNight : MonoBehaviour
{

    [Space(10)]
    [Header("Player")]
    public Transform player = null;
    [Space(20)]

    //Don't forget to add the Directional Light here
    [Space(10)]
    [Header("Directional Light")]
    public Light directionalLight;

    [Space(10)]

    //  In the 2 fields below, only the materials that will be alternated in the day/night exchange are registered
    //  When adding your buildings(which will have their own materials), you can register the day and night versions of the materials here.
    //  The index of the daytime version of the material must match the index of the nighttime version of the material
    //  Example: When switching to night scene, materialDay[1] will be replaced by materialNight[1]
    //  (Materials that will be used both night and day do not need to be here)
    public Material[] materialDay;    // Add materials that are only used in the day scene, and are substituted in the night scene
    public Material[] materialNight;  // Add night scene materials that will replace day scene materials. (The sequence must be respected)



    public VolumeProfile volumeProfile_Day;  
    public VolumeProfile volumeProfile_Night;




    [HideInInspector]
    public bool isNight;

    [HideInInspector]
    public bool night;

    [HideInInspector]
    public bool isSpotLights;

    [HideInInspector]
    public bool spotLights;

    [HideInInspector]
    public float intenseMoonLight = 800f;

    [HideInInspector]
    public float _intenseMoonLight;

    [HideInInspector]
    public float intenseSunLight = 8000f;

    [HideInInspector]
    public float _intenseSunLight;


    [HideInInspector]
    public float temperatureSunLight = 6700f;

    [HideInInspector]
    public float _temperatureSunLight;

    [HideInInspector]
    public float temperatureMoonLight = 9500f;

    [HideInInspector]
    public float _temperatureMoonLight;


    private float lastCheckTime = 0f;

    private GameObject[] lightV;
    private GameObject[] spot_Light;


    private void OnEnable()
    {

        

        Transform cityMaker = null;

        if (GameObject.Find("City-Maker") != null)
            cityMaker = GameObject.Find("City-Maker").transform;

        lightV = SearchUtility.FindAllObjectsByName("_LightV");
        spot_Light = SearchUtility.FindAllObjectsByName("_Spot_Light");

    }

    private void Start()
    {
        ChangeMaterial();
    }
    private void FixedUpdate()
    {

        if (Time.time - lastCheckTime >= 3)
        {
            lastCheckTime = Time.time;

            if (isSpotLights && isNight)
            {
                SetStreetLights();
            }

        }


    }

    public void SetStreetLights(bool renew = false)
    {

        if (renew && !player)
        {
            SetCameraPlayer();
        }

        if (lightV == null || renew)
            lightV = SearchUtility.FindAllObjectsByName("_LightV");

        if (spot_Light == null || renew)
            spot_Light = SearchUtility.FindAllObjectsByName("_Spot_Light");


        //GameObject[] tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == ("_LightV")).ToArray();

        foreach (GameObject lines in lightV)
        {
            lines.GetComponent<MeshRenderer>().enabled = isNight;
            if (lines.transform.GetChild(0))
                lines.transform.GetChild(0).GetComponent<Light>().enabled = (isSpotLights && isNight && CheckDistance(lines.transform.position, 100, false));
        }

        //tempArray = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name == ("_Spot_Light")).ToArray();

        foreach (GameObject lines in spot_Light)
            lines.GetComponent<Light>().enabled = (isSpotLights && isNight && CheckDistance(lines.transform.position, 80, false));

    }

    bool CheckDistance(Vector3 pos, float dist, bool _default)
    {
        if (!player)
            return _default;

        return Vector3.Distance(player.position, pos) < dist;

    }

    public void SetCameraPlayer()
    {
        if (!player)
        {
            Debug.LogWarning("Player was not defined in the Day/Night System");

            // Tries to find the main camera
            Camera targetCamera = Camera.main;

            // If the main camera is not found, search for any other camera in the scene
            if (targetCamera == null)
            {
                Camera[] cameras = FindObjectsOfType<Camera>();
                if (cameras.Length > 0)
                {
                    player = cameras[0].transform;
                    Debug.Log("No MainCamera found. Assigning another scene camera as Player in the Day/Night System.");
                }
                else
                    Debug.LogWarning("No camera found in the scene!");
            }
            else
            {
                player = targetCamera.transform;
                Debug.Log("MainCamera was set as Player in the Day/Night System");
            }
        }
    
    }


    public void ChangeVolume()
    {
        GetComponent<Volume>().profile = (isNight) ? volumeProfile_Night : volumeProfile_Day;
    }

    public void ChangeMaterial()
    {

#if UNITY_2020_1_OR_NEWER

        if (GetComponent<Volume>().profile.TryGet<Exposure>(out var exp))
            exp.compensation.SetValue(new FloatParameter(0));

#endif

        // shift VolumeProfile :  day/night
        GetComponent<Volume>().profile = (isNight) ? volumeProfile_Night : volumeProfile_Day;




        /*
        Substituting Night materials for Day materials (or vice versa) in all Mesh Renders within City-Maker
        Only materials that have been added in "materialDay" and "materialNight" Array
        */

        GameObject GmObj = GameObject.Find("City-Maker"); ;
        if (GmObj == null) return;
                
        Renderer[] children = GmObj.GetComponentsInChildren<Renderer>();

        Material[] myMaterials;

        for (int i = 0; i < children.Length; i++)
        {
            myMaterials = children[i].GetComponent<Renderer>().sharedMaterials;

            for (int m = 0; m < myMaterials.Length; m++)
            {
                for (int mt = 0; mt < materialDay.Length; mt++)
                if (isNight)
                {
                    if(myMaterials[m] == materialDay[mt])
                        myMaterials[m] = materialNight[mt];

                }
                else
                {
                    if (myMaterials[m] == materialNight[mt])
                        myMaterials[m] = materialDay[mt];
                }


                children[i].GetComponent<MeshRenderer>().sharedMaterials = myMaterials;
            }


        }

        //Configuring the Directional Light as it is day or night (sun/moon)
        SetDirectionalLight();


        //Toggles street lamp lights on/off
        SetStreetLights(true);



    }

    public void SetDirectionalLight() //Configuring the Directional Light as it is day or night (sun/moon)
    {

        if (directionalLight)
        {
            directionalLight.GetComponent<HDAdditionalLightData>().intensity = (isNight) ? intenseMoonLight : intenseSunLight; // 800 : 8000;

            directionalLight.useColorTemperature = true;
            if (directionalLight.useColorTemperature)
                directionalLight.colorTemperature = (isNight) ? temperatureMoonLight : temperatureSunLight;

        }
        else
            Debug.LogError("You must set the Directional Light in the Inspector of DayNight Prefab");

    }




}
