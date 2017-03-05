using Rainfall.Redirection.Attributes;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;

namespace Rainfall
{
    [TargetType(typeof(ThunderStormAI))]
    internal class ThunderStormAIDetour:ThunderStormAI
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
                            Debug.Log("[RF]ThunderStormAIDetour Limtied Thunderstorm Rain to " + Singleton<WeatherManager>.instance.m_targetRain);
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
                Debug.Log("[RF]ThunderStormAIDetour Limtied Thunderstorm Rain to " + Singleton<WeatherManager>.instance.m_targetRain);
                Singleton<WeatherManager>.instance.m_targetCloud = newThunderstormTargetRain;
                //end edit
                uint currentFrameIndex2 = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                int num = 100;
                num = Mathf.Min(num, (int)(currentFrameIndex2 - data.m_activationFrame >> 3));
                num = Mathf.Min(num, (int)(data.m_activationFrame + this.m_activeDuration - currentFrameIndex2 >> 3));
                num = (num * (int)data.m_intensity + 50) / 100;
                Randomizer randomizer = new Randomizer(data.m_randomSeed ^ (ulong)(currentFrameIndex2 >> 8));
                int num2 = randomizer.Int32(Mathf.Max(1, num / 20), Mathf.Max(1, 1 + num / 10));
                float num3 = this.m_radius * (0.25f + (float)data.m_intensity * 0.0075f);
                InstanceID id = default(InstanceID);
                id.Disaster = disasterID;
                InstanceManager.Group group = Singleton<InstanceManager>.instance.GetGroup(id);
                for (int i = 0; i < num2; i++)
                {
                    float f = (float)randomizer.Int32(10000u) * 0.0006283185f;
                    float num4 = Mathf.Sqrt((float)randomizer.Int32(10000u) * 0.0001f) * num3;
                    uint startFrame = currentFrameIndex2 + randomizer.UInt32(256u);
                    Vector3 targetPosition = data.m_targetPosition;
                    targetPosition.x += Mathf.Cos(f) * num4;
                    targetPosition.z += Mathf.Sin(f) * num4;
                    targetPosition.y = Singleton<TerrainManager>.instance.SampleRawHeightSmoothWithWater(targetPosition, false, 0f);
                    Quaternion quaternion = Quaternion.AngleAxis((float)randomizer.Int32(360u), Vector3.up);
                    quaternion *= Quaternion.AngleAxis((float)randomizer.Int32(-15, 15), Vector3.right);
                    Singleton<WeatherManager>.instance.QueueLightningStrike(startFrame, targetPosition, quaternion, group);
                }
            }
        }


    }
}
