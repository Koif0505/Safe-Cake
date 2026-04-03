using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Runtime debug keys have been disabled by default.
*/

namespace FCG
{
    public class ShiftAtRuntime : MonoBehaviour
    {
        DayNight dayNight;

        public CityGenerator cityGenerator;

        [Space(10)]
        [Range(70, 130)]
        public float downtownSize = 100;

        public bool allowDebugKeys = false;

        private void Start()
        {
            dayNight = FindObjectOfType<DayNight>();
        }

        private void Update()
        {
            if (!allowDebugKeys) return;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (dayNight)
                {
                    dayNight.isNight = !dayNight.isNight;
                    dayNight.ChangeMaterial();
                }
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                if (GameObject.Find("City-Maker") == null)
                    return;

                if (cityGenerator)
                    cityGenerator.GenerateAllBuildings(true, downtownSize);
                else
                    Debug.Log("CityGenerator not assigned in inspector");

                if (dayNight && dayNight.isNight)
                {
                    dayNight.SetStreetLights(true);
                }
            }
#endif
        }
    }
}