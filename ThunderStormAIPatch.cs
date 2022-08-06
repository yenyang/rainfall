using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;
using HarmonyLib;
using System;

namespace Rainfall
{
    [HarmonyPatch(typeof(ThunderStormAI), nameof(ThunderStormAI.SimulationStep), new Type[] { typeof(ushort), typeof(DisasterData) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
    public class ThunderStormAIPatch
    {
        public static void Postfix(ushort disasterID, ref DisasterData data)
        {
           // Debug.Log("[RF]ThunderStormAIHarmonics Postfix");
            if ((data.m_flags & DisasterData.Flags.Emerging) != DisasterData.Flags.None)
            {
                if (data.m_activationFrame != 0u)
                {
                    uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                    if (currentFrameIndex + 1755u >= data.m_activationFrame)
                    {
                        if ((data.m_flags & DisasterData.Flags.Significant) != DisasterData.Flags.None)
                        {
                            Singleton<DisasterManager>.instance.DetectDisaster(disasterID, false);
                        }
                        if ((data.m_flags & DisasterData.Flags.SelfTrigger) != DisasterData.Flags.None)
                        {
                            Singleton<WeatherManager>.instance.m_forceWeatherOn = 2f;
                            Singleton<WeatherManager>.instance.m_targetFog = 0f;
                            //Begin Edit
                            float newThunderstormTargetRain = Mathf.Clamp(((float)data.m_intensity) / 100, 0.25f, 1.0f);
                            Singleton<WeatherManager>.instance.m_targetRain = newThunderstormTargetRain;
                            //Debug.Log("[RF]ThunderStormAIHarmonics Limtied Thunderstorm Rain to " + Singleton<WeatherManager>.instance.m_targetRain);
                            Singleton<WeatherManager>.instance.m_targetCloud = newThunderstormTargetRain;
                            //end edit
                        }
                    }
                }
            }
            else if ((data.m_flags & DisasterData.Flags.Active) != DisasterData.Flags.None && (data.m_flags & DisasterData.Flags.SelfTrigger) != DisasterData.Flags.None)
            {
                Singleton<WeatherManager>.instance.m_forceWeatherOn = 2f;
                Singleton<WeatherManager>.instance.m_targetFog = 0f;
                //Begin Edit
                float newThunderstormTargetRain = Mathf.Clamp(((float)data.m_intensity) / 100, 0.25f, 1.0f);
                Singleton<WeatherManager>.instance.m_targetRain = newThunderstormTargetRain;
                //Debug.Log("[RF]ThunderStormAIHarmonics Limtied Thunderstorm Rain to " + Singleton<WeatherManager>.instance.m_targetRain);
                Singleton<WeatherManager>.instance.m_targetCloud = newThunderstormTargetRain;
                //end edit
            }
        }
    }
}
