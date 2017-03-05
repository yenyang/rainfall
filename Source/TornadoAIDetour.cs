using Rainfall.Redirection.Attributes;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;

namespace Rainfall
{
    [TargetType(typeof(TornadoAI))]
    internal class TornadoAIDetour:TornadoAI
    {
        [RedirectMethod]
        public override void SimulationStep(ushort disasterID, ref DisasterData data)
        {
            //Begin Disaster AI SimulationStep
            if ((data.m_flags & DisasterData.Flags.Clearing) != DisasterData.Flags.None)
            {
                if (!this.IsStillClearing(disasterID, ref data))
                {
                    this.EndDisaster(disasterID, ref data);
                }
            }
            else if ((data.m_flags & DisasterData.Flags.Active) != DisasterData.Flags.None)
            {
                if (!this.IsStillActive(disasterID, ref data))
                {
                    this.DeactivateDisaster(disasterID, ref data);
                }
            }
            else if ((data.m_flags & DisasterData.Flags.Emerging) != DisasterData.Flags.None)
            {
                if (!this.IsStillEmerging(disasterID, ref data))
                {
                    this.ActivateDisaster(disasterID, ref data);
                }
            }
            if ((data.m_flags & DisasterData.Flags.Detected) != DisasterData.Flags.None)
            {
                if ((data.m_flags & DisasterData.Flags.Warning) != DisasterData.Flags.None)
                {
                    if (data.m_broadcastCooldown > 0)
                    {
                        data.m_broadcastCooldown -= 1;
                    }
                    if (data.m_broadcastCooldown == 0)
                    {
                        data.m_broadcastCooldown = 36;
                        if (this.m_info.m_warningBroadcast != null)
                        {
                            Singleton<AudioManager>.instance.QueueBroadcast(this.m_info.m_warningBroadcast);
                        }
                    }
                }
                else
                {
                    StatisticBase statisticBase = Singleton<StatisticsManager>.instance.Acquire<StatisticInt32>(StatisticType.DisasterCount);
                    statisticBase.Add(16);
                    InstanceID id = default(InstanceID);
                    id.Disaster = disasterID;
                    InstanceManager.Group group = Singleton<InstanceManager>.instance.GetGroup(id);
                    if (data.m_broadcastCooldown >= 200)
                    {
                        if (data.m_broadcastCooldown > 200)
                        {
                            data.m_broadcastCooldown -= 1;
                        }
                        if (data.m_broadcastCooldown == 200 && (group == null || group.m_refCount >= 2))
                        {
                            data.m_broadcastCooldown = 236;
                            if (this.m_info.m_activeBroadcast != null)
                            {
                                Singleton<AudioManager>.instance.QueueBroadcast(this.m_info.m_activeBroadcast);
                            }
                        }
                    }
                    else
                    {
                        if (data.m_broadcastCooldown > 0)
                        {
                            data.m_broadcastCooldown -= 1;
                        }
                        if (data.m_broadcastCooldown == 0)
                        {
                            data.m_broadcastCooldown = 236;
                            if (this.m_info.m_activeBroadcast != null)
                            {
                                Singleton<AudioManager>.instance.QueueBroadcast(this.m_info.m_activeBroadcast);
                            }
                        }
                    }
                    if (group == null || group.m_refCount >= 2)
                    {
                        if (data.m_chirpCooldown > 0)
                        {
                            data.m_chirpCooldown -= 1;
                        }
                        else if (this.m_info.m_prefabDataIndex != -1)
                        {
                            string key = PrefabCollection<DisasterInfo>.PrefabName((uint)this.m_info.m_prefabDataIndex);
                            if (Locale.Exists("CHIRP_DISASTER", key))
                            {
                                string disasterName = Singleton<DisasterManager>.instance.GetDisasterName(disasterID);
                                Singleton<MessageManager>.instance.TryCreateMessage("CHIRP_DISASTER", key, Singleton<MessageManager>.instance.GetRandomResidentID(), disasterName);
                                data.m_chirpCooldown = (byte)Singleton<SimulationManager>.instance.m_randomizer.Int32(8, 24);
                            }
                        }
                    }
                }
                GuideController properties = Singleton<GuideManager>.instance.m_properties;
                if (this.m_info.m_disasterWarningGuide != null && properties != null)
                {
                    this.m_info.m_disasterWarningGuide.Activate(properties.m_disasterWarning, this.m_info);
                }
            }
            if ((data.m_flags & DisasterData.Flags.Repeat) != DisasterData.Flags.None && ((data.m_flags & (DisasterData.Flags.Finished | DisasterData.Flags.UnReported)) == DisasterData.Flags.Finished || (data.m_flags & (DisasterData.Flags.Clearing | DisasterData.Flags.UnDetected)) == (DisasterData.Flags.Clearing | DisasterData.Flags.UnDetected)))
            {
                this.StartDisaster(disasterID, ref data);
            }
            //End DisasterAI.Simulation Step
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
                    Debug.Log("[RF]TornadoAIDetour Limtied Tornado Rain to " + Singleton<WeatherManager>.instance.m_targetRain);
                    //End Edit
                    Singleton<WeatherManager>.instance.m_targetCloud = 1f;
                }
            }
            else if ((data.m_flags & DisasterData.Flags.Active) != DisasterData.Flags.None)
            {
                Singleton<WeatherManager>.instance.m_forceWeatherOn = 2f;
                Singleton<WeatherManager>.instance.m_targetFog = 0f;
                //Begin Edit
                float newTornadoTargetRain = Mathf.Clamp(((float)data.m_intensity) / 100, 0.25f, 1.0f);
                Singleton<WeatherManager>.instance.m_targetRain = newTornadoTargetRain;
                Debug.Log("[RF]TornadoAIDetour Limtied Tornado Rain to " + Singleton<WeatherManager>.instance.m_targetRain);
                //End Edit
                Singleton<WeatherManager>.instance.m_targetCloud = 1f;
            }
        }


    }
}
