using Rainfall.Redirection.Attributes;
using ColossalFramework;
using ColossalFramework.Math;
using System;

using UnityEngine;

namespace Rainfall
{

    [TargetType(typeof(TrainTrackBaseAI))]
    public class TrainTrackBaseAIDetour : TrainTrackBaseAI
    {
        [RedirectMethod]
        public override void SimulationStep(ushort segmentID, ref NetSegment data)
        {
            //Start PlayerNEtAI.SimulationStep

            NetManager instance = Singleton<NetManager>.instance;
            Notification.Problem problem = Notification.RemoveProblems(data.m_problems, Notification.Problem.Flood);
            float num = 0f;
            uint num2 = data.m_lanes;
            int num3 = 0;
            while (num3 < this.m_info.m_lanes.Length && num2 != 0u)
            {
                NetInfo.Lane lane = this.m_info.m_lanes[num3];
                if (lane.m_laneType == NetInfo.LaneType.Vehicle)
                {
                    num += instance.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_length;
                }
                num2 = instance.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_nextLane;
                num3++;
            }
            int num4 = Mathf.RoundToInt(num) << 4;
            int num5 = 0;
            if (num4 != 0)
            {
                num5 = (int)((byte)Mathf.Min((int)(data.m_trafficBuffer * 100) / num4, 100));
            }
            data.m_trafficBuffer = 0;
            if (num5 > (int)data.m_trafficDensity)
            {
                data.m_trafficDensity = (byte)Mathf.Min((int)(data.m_trafficDensity + 5), num5);
            }
            else if (num5 < (int)data.m_trafficDensity)
            {
                data.m_trafficDensity = (byte)Mathf.Max((int)(data.m_trafficDensity - 5), num5);
            }
            Vector3 position = instance.m_nodes.m_buffer[(int)data.m_startNode].m_position;
            Vector3 position2 = instance.m_nodes.m_buffer[(int)data.m_endNode].m_position;
            Vector3 vector = (position + position2) * 0.5f;
            bool flag = false;
            if ((this.m_info.m_setVehicleFlags & Vehicle.Flags.Underground) == (Vehicle.Flags)0)
            {
                float num6 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(vector));
                if (num6 > vector.y + ModSettings.TrainTrackFloodedTolerance)
                {
                    flag = true;
                    data.m_flags |= NetSegment.Flags.Flooded;
                    problem = Notification.AddProblems(problem, Notification.Problem.Flood | Notification.Problem.MajorProblem);
                }
                else
                {
                    data.m_flags &= ~NetSegment.Flags.Flooded;
                    if (num6 > vector.y + ModSettings.TrainTrackFloodingTolerance)
                    {
                        flag = true;
                        problem = Notification.AddProblems(problem, Notification.Problem.Flood);
                    }
                }
                int num7 = (int)data.m_wetness;
                if (!instance.m_treatWetAsSnow)
                {
                    if (flag)
                    {
                        num7 = 255;
                    }
                    else
                    {
                        int num8 = -(num7 + 63 >> 5);
                        float num9 = Singleton<WeatherManager>.instance.SampleRainIntensity(vector, false);
                        if (num9 != 0f)
                        {
                            int num10 = Mathf.RoundToInt(Mathf.Min(num9 * 4000f, 1000f));
                            num8 += Singleton<SimulationManager>.instance.m_randomizer.Int32(num10, num10 + 99) / 100;
                        }
                        num7 = Mathf.Clamp(num7 + num8, 0, 255);
                    }
                }
                if (num7 != (int)data.m_wetness)
                {
                    if (Mathf.Abs((int)data.m_wetness - num7) > 10)
                    {
                        data.m_wetness = (byte)num7;
                        InstanceID empty = InstanceID.Empty;
                        empty.NetSegment = segmentID;
                        instance.AddSmoothColor(empty);
                        empty.NetNode = data.m_startNode;
                        instance.AddSmoothColor(empty);
                        empty.NetNode = data.m_endNode;
                        instance.AddSmoothColor(empty);
                    }
                    else
                    {
                        data.m_wetness = (byte)num7;
                        instance.m_wetnessChanged = 256;
                    }
                }
            }
            int num11 = (int)(100 - (data.m_trafficDensity - 100) * (data.m_trafficDensity - 100) / 100);
            int num12 = this.m_noiseAccumulation * num11 / 100;
            if (num12 != 0)
            {
                float num13 = Vector3.Distance(position, position2);
                int num14 = Mathf.FloorToInt(num13 / this.m_noiseRadius);
                for (int i = 0; i < num14; i++)
                {
                    Vector3 position3 = Vector3.Lerp(position, position2, (float)(i + 1) / (float)(num14 + 1));
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.NoisePollution, num12, position3, this.m_noiseRadius);
                }
            }
            data.m_problems = problem;
        }
    }
}

