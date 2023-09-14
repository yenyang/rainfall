
using UnityEngine;
using HarmonyLib;
using Rainfall;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Rainfall
{
    [HarmonyPatch(typeof(BuildingInfo), nameof(BuildingInfo.InitializePrefab))]
    public class BuildingInfoInitializePrefabPatch 
    {
        public static bool Prefix(BuildingInfo __instance)
        {
            //Debug.Log("[PLS]BuildingInfoInitializePrefabPatch Prefix");

            var oldAI = __instance.gameObject.GetComponent<PrefabAI>();
            //Debug.Log("[PLS]BuildingInfoInitializePrefabPatch oldAI.GetType().fullname = " + oldAI.GetType().FullName);
            if (oldAI.GetType() == typeof(WaterFacilityAI) )
            {
                //Debug.Log("[PLS]BuildingInfoInitializePrefabPatch " + __instance.name +" is DummyBuildingAI");
                
                Debug.Log("[RF]BuildingInfoInitializePrefabPatch __instance.name = " + __instance.name);
                bool flag = (__instance.name == "2818575811.Water Drain Pipe_Data");
                //Debug.Log("[PLS]BuildingInfoInitializePrefabPatch ModSettings.PSACustomProperties.ContainsKey is " + flag);

                if (flag)
                {

                    //Debug.Log("[PLS]BuildingInfoInitializePrefabPatch " + psa.Key + " found!");
                    UnityEngine.Object.DestroyImmediate(oldAI);

                    // add new ai
                    var newAI = (PrefabAI)__instance.gameObject.AddComponent<StormDrainAI>();

                    var newAIFields = newAI.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);


                    var newAIFieldDic = new Dictionary<string, FieldInfo>(newAIFields.Length);
                    foreach (var field in newAIFields)
                    {
                        //Debug.Log("[PLS]BuildingInfoInitializePrefabPatch Found Field named " + field.Name);
                        newAIFieldDic.Add(field.Name, field);
                    }

                    foreach (var fieldInfo in newAIFields)
                    {
                        // do not copy attributes marked NonSerialized
                        bool copyField = !fieldInfo.IsDefined(typeof(NonSerializedAttribute), true);

                        if (!fieldInfo.IsDefined(typeof(CustomizablePropertyAttribute), true)) copyField = false;

                        if (copyField)
                        {
                            FieldInfo newAIField;
                            newAIFieldDic.TryGetValue(fieldInfo.Name, out newAIField);
                            try
                            {
                                if (newAIField != null && newAIField.GetType().Equals(fieldInfo.GetType()))
                                {
                                    if (fieldInfo.Name == "m_stormWaterOutlet")
                                    {
                                        newAIField.SetValue(newAI, 2500);
                                        //Debug.Log("[PLS]BuildingInfoInitializePrefabPatch Set " + fieldInfo.Name + " to " + psa.Value.Offset);
                                    }
                                    else if (fieldInfo.Name == "m_invert")
                                    {
                                        newAIField.SetValue(newAI, 1f);
                                    }
                                    else if (fieldInfo.Name == "m_soffit")
                                    {
                                        newAIField.SetValue(newAI, 3f);
                                    }
                                    else if (fieldInfo.Name == "m_milestone")
                                    {
                                        newAIField.SetValue(newAI, 3);
                                    }
                                    else if (fieldInfo.Name == "m_placementMode")
                                    {
                                        newAIField.SetValue(newAI, BuildingInfo.PlacementMode.ShorelineOrGround);
                                    }
                                    else if (fieldInfo.Name == "m_placementModeAlt")
                                    {
                                        newAIField.SetValue(newAI, BuildingInfo.PlacementMode.OnTerrain);
                                    }
                                    else if (fieldInfo.Name == "m_waterEffectDistance")
                                    {
                                        newAIField.SetValue(newAI, 10f);
                                    }
                                    else if (fieldInfo.Name == "m_waterConsumption")
                                    {
                                        newAIField.SetValue(newAI, 1);
                                    }
                                    else if (fieldInfo.Name == "m_sewageAccumulation")
                                    {
                                        newAIField.SetValue(newAI, 1);
                                    }
                                    else if (fieldInfo.Name == "m_electricityConsumption")
                                    {
                                        newAIField.SetValue(newAI, 0);
                                    }
                                    else if (fieldInfo.Name == "m_waterLocationOffset")
                                    {
                                        newAIField.SetValue(newAI, new Vector3(0,0,38f));
                                    }
                                    else if (fieldInfo.Name == "m_garbageAccumulation")
                                    {
                                        newAIField.SetValue(newAI, 0);
                                    }
                                    
                                    //Debug.Log("[PLS]BuildingInfoInitializePrefabPatch Ready to set fieldInfo Named " + fieldInfo.Name);
                                    //newAIField.SetValue(newAI, fieldInfo.GetValue(src));
                                }
                            }
                            catch (NullReferenceException)
                            {
                            }
                        }
                    }
                }  
                
            }
          
           return true;
        }
        
    }
}
