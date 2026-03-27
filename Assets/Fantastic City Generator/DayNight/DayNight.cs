using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;

namespace FCG
{
    public class DayNight : MonoBehaviour
    {
        [Space(10)]
        [Header("Player")]
        public Transform player = null;
        [Space(20)]

        [Space(10)]
        [Header("Directional Light")]
        public Light directionalLight;

        public Material[] materialDay;
        public Material[] materialNight;

        public Material skyBoxDay;
        public Material skyBoxNight;

        [Space(10)]
        [HideInInspector] public bool isNight;
        [HideInInspector] public bool isMoonLight;
        [HideInInspector] public bool isSpotLights;

        [HideInInspector] public bool night;
        [HideInInspector] public bool moonLight;
        [HideInInspector] public bool spotLights;

        [HideInInspector] public float intenseMoonLight = 20f;
        [HideInInspector] public float _intenseMoonLight;

        [HideInInspector] public float intenseSunLight = 100f;
        [HideInInspector] public float _intenseSunLight;

        [HideInInspector] public Color skyColorDay = new Color(0.74f, 0.62f, 0.60f);
        [HideInInspector] public Color equatorColorDay = new Color(0.74f, 0.74f, 0.74f);

        [HideInInspector] public Color _skyColorDay;
        [HideInInspector] public Color _equatorColorDay;

        [HideInInspector] public Color skyColorNight = new Color(0.78f, 0.72f, 0.72f);
        [HideInInspector] public Color equatorColorNight = new Color(0.16f, 0.16f, 0.16f);

        [HideInInspector] public Color _skyColorNight;
        [HideInInspector] public Color _equatorColorNight;

        [HideInInspector] public Color sunLightColor = new Color(1f, 0.95f, 0.85f);
        [HideInInspector] public Color _sunLightColor;

        [HideInInspector] public Color moonLightColor = new Color(0.75f, 0.8f, 1f);
        [HideInInspector] public Color _moonLightColor;

        private float lastCheckTime = 0f;

        GameObject[] lightV;
        GameObject[] spot_Light;

        private void OnEnable()
        {
            lightV = SearchUtility.FindAllObjectsByName("_LightV");
            spot_Light = SearchUtility.FindAllObjectsByName("_Spot_Light");
        }

        private void Start()
        {
            // Mặc định vào scene là ban ngày
            SetDayMode();
        }

        private void FixedUpdate()
        {
            if (Time.time - lastCheckTime >= 3f)
            {
                lastCheckTime = Time.time;

                if (isSpotLights && isNight)
                {
                    SetStreetLights();
                }
            }
        }

        // ===== HÀM MỚI: GAME MANAGER GỌI =====

        public void SetDayMode()
        {
            isNight = false;
            isMoonLight = false;
            isSpotLights = false;
            ChangeMaterial();
        }

        public void SetNightMode()
        {
            isNight = true;
            isMoonLight = true;
            isSpotLights = true;
            ChangeMaterial();
        }

        // =====================================

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

            foreach (GameObject lines in lightV)
            {
                lines.GetComponent<MeshRenderer>().enabled = isNight;

                if (lines.transform.childCount > 0)
                {
                    Light childLight = lines.transform.GetChild(0).GetComponent<Light>();
                    if (childLight != null)
                    {
                        childLight.enabled = (isSpotLights && isNight && CheckDistance(lines.transform.position, 100f, false));
                    }
                }
            }

            foreach (GameObject lines in spot_Light)
            {
                Light l = lines.GetComponent<Light>();
                if (l != null)
                {
                    l.enabled = (isSpotLights && isNight && CheckDistance(lines.transform.position, 80f, true));
                }
            }
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

                Camera targetCamera = Camera.main;

                if (targetCamera == null)
                {
                    Camera[] cameras = FindObjectsOfType<Camera>();
                    if (cameras.Length > 0)
                    {
                        player = cameras[0].transform;
                        Debug.Log("No MainCamera found. Assigning another scene camera as Player in the Day/Night System.");
                    }
                    else
                    {
                        Debug.LogWarning("No camera found in the scene!");
                    }
                }
                else
                {
                    player = targetCamera.transform;
                    Debug.Log("MainCamera was set as Player in the Day/Night System");
                }
            }
        }

        public void ChangeMaterial()
        {
            RenderSettings.skybox = isNight ? skyBoxNight : skyBoxDay;
            UpdateColor();

            GameObject GmObj = GameObject.Find("City-Maker");
            if (GmObj == null) return;

            Renderer[] children = GmObj.GetComponentsInChildren<Renderer>();
            Material[] myMaterials;

            for (int i = 0; i < children.Length; i++)
            {
                myMaterials = children[i].sharedMaterials;

                for (int m = 0; m < myMaterials.Length; m++)
                {
                    for (int mt = 0; mt < materialDay.Length; mt++)
                    {
                        if (isNight)
                        {
                            if (myMaterials[m] == materialDay[mt])
                                myMaterials[m] = materialNight[mt];
                        }
                        else
                        {
                            if (myMaterials[m] == materialNight[mt])
                                myMaterials[m] = materialDay[mt];
                        }
                    }
                }

                children[i].sharedMaterials = myMaterials;
            }

            SetDirectionalLight();
            SetStreetLights(true);

            DynamicGI.UpdateEnvironment();
        }

        public void UpdateColor()
        {
            if (isNight)
            {
                if (directionalLight != null)
                    directionalLight.color = moonLightColor;

                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = skyColorNight;
                RenderSettings.ambientEquatorColor = equatorColorNight;
                RenderSettings.ambientGroundColor = new Color(0.07f, 0.07f, 0.07f);
            }
            else
            {
                if (directionalLight != null)
                    directionalLight.color = sunLightColor;

                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = skyColorDay;
                RenderSettings.ambientEquatorColor = equatorColorDay;
                RenderSettings.ambientGroundColor = new Color(0.4f, 0.4f, 0.4f);
            }
        }

        public void SetDirectionalLight()
        {
            if (directionalLight != null)
            {
                directionalLight.enabled = (!isNight || isMoonLight);
                directionalLight.intensity = isNight ? intenseMoonLight / 100f : intenseSunLight / 100f;
            }
        }
    }
}