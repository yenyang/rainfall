using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;
using HarmonyLib;
using System;

namespace Rainfall
{
    [HarmonyPatch(typeof(TornadoAI), nameof(TornadoAI.SimulationStep), new Type[] { typeof(ushort), typeof(DisasterData) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
    public class TornadoAIPatch
    {
        public static void Postfix(ushort disasterID, ref DisasterData data)
        {
            // Debug.Log("[RF]TornadoAIHarmonics Postfix");
            if ((data.m_flags & DisasterData.Flags.Emerging) != DisasterData.Flags.None)
            {
                uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                if (currentFrameIndex + 1755u > data.m_activationFrame)
                {
                    Singleton<DisasterManager>.instance.DetectDisaster(disasterID, false);
                    Singleton<WeatherManager>.instance.m_forceWeatherOn = 2f;
                    Singleton<WeatherManager>.instance.m_targetFog = 0f;
                    //Begin Edit
                    float newTornadoTargetRain = Mathf.Clamp(((float)data.m_intensity) / 100, 0.25f, 1.0f);
                    Singleton<WeatherManager>.instance.m_targetRain = newTornadoTargetRain;
                    //Debug.Log("[RF]TornadoAIHarmonics Limtied Tornado Rain to " + Singleton<WeatherManager>.instance.m_targetRain);
                    Singleton<WeatherManager>.instance.m_targetCloud = newTornadoTargetRain;
                    //end edit
                }
            }
                
                
            	else if ((data.m_flags & DisasterData.Flags.Active) != DisasterData.Flags.None)
	            {
		        Singleton<WeatherManager>.instance.m_forceWeatherOn = 2f;
		        Singleton<WeatherManager>.instance.m_targetFog = 0f;
                //Begin Edit
                float newTornadoTargetRain = Mathf.Clamp(((float)data.m_intensity) / 100, 0.25f, 1.0f);
                Singleton<WeatherManager>.instance.m_targetRain = newTornadoTargetRain;
                //Debug.Log("[RF]TornadoAIHarmonics Limtied Tornado Rain to " + Singleton<WeatherManager>.instance.m_targetRain);
                Singleton<WeatherManager>.instance.m_targetCloud = newTornadoTargetRain;
                //end edit
            }
        }
    }
}
