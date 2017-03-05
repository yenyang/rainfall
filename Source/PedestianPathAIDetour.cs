using Rainfall.Redirection.Attributes;
using ColossalFramework;
using ColossalFramework.Math;
using System;

using UnityEngine;

namespace Rainfall
{

    [TargetType(typeof(PedestrianPathAI))]
    public class PedestiranPathAIDetour : PedestrianPathAI
    {
        [RedirectMethod]
        public override void SimulationStep(ushort segmentID, ref NetSegment data)
        {
            //Start PlayerNEtAI.SimulationStep

            if (this.HasMaintenanceCost(segmentID, ref data))
            {
                NetManager playerNetAIinstance = Singleton<NetManager>.instance;
                Vector3 playerNetAIposition = playerNetAIinstance.m_nodes.m_buffer[(int)data.m_startNode].m_position;
                Vector3 playerNetAIposition2 = playerNetAIinstance.m_nodes.m_buffer[(int)data.m_endNode].m_position;
                int playerNetAInum = this.GetMaintenanceCost(playerNetAIposition, playerNetAIposition2);
                bool playerNetAIflag = (ulong)(Singleton<SimulationManager>.instance.m_currentFrameIndex >> 8 & 15u) == (ulong)((long)(segmentID & 15));
                if (playerNetAInum != 0)
                {
                    if (playerNetAIflag)
                    {
                        playerNetAInum = playerNetAInum * 16 / 100 - playerNetAInum / 100 * 15;
                    }
                    else
                    {
                        playerNetAInum /= 100;
                    }
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, playerNetAInum, this.m_info.m_class);
                }
                if (playerNetAIflag)
                {
                    float playerNetAInum2 = (float)playerNetAIinstance.m_nodes.m_buffer[(int)data.m_startNode].m_elevation;
                    float playerNetAInum3 = (float)playerNetAIinstance.m_nodes.m_buffer[(int)data.m_endNode].m_elevation;
                    if (this.IsUnderground())
                    {
                        playerNetAInum2 = -playerNetAInum2;
                        playerNetAInum3 = -playerNetAInum3;
                    }
                    int constructionCost = this.GetConstructionCost(playerNetAIposition, playerNetAIposition2, playerNetAInum2, playerNetAInum3);
                    if (constructionCost != 0)
                    {
                        StatisticBase statisticBase = Singleton<StatisticsManager>.instance.Acquire<StatisticInt64>(StatisticType.CityValue);
                        if (statisticBase != null)
                        {
                            statisticBase.Add(constructionCost);
                        }
                    }
                }
            }
            //End  PlayerNEtAI.SimulationStep

            if (!this.m_invisible)
            {
                NetManager instance = Singleton<NetManager>.instance;
                Notification.Problem problem = Notification.RemoveProblems(data.m_problems, Notification.Problem.Flood);
                Vector3 position = instance.m_nodes.m_buffer[(int)data.m_startNode].m_position;
                Vector3 position2 = instance.m_nodes.m_buffer[(int)data.m_endNode].m_position;
                Vector3 vector = (position + position2) * 0.5f;
                float num = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(vector));
                if (num > vector.y + ModSettings.PedestrianPathFloodedTolerance)
                {
                    if ((data.m_flags & NetSegment.Flags.Flooded) == NetSegment.Flags.None)
                    {
                        data.m_flags |= NetSegment.Flags.Flooded;
                        data.m_modifiedIndex = Singleton<SimulationManager>.instance.m_currentBuildIndex++;
                    }
                    problem = Notification.AddProblems(problem, Notification.Problem.Flood | Notification.Problem.MajorProblem);
                }
                else
                {
                    data.m_flags &= ~NetSegment.Flags.Flooded;
                    if (num > vector.y + ModSettings.PedestrianPathFloodingTolerance)
                    {
                        problem = Notification.AddProblems(problem, Notification.Problem.Flood);
                    }
                }
                DistrictManager instance2 = Singleton<DistrictManager>.instance;
                byte district = instance2.GetDistrict(vector);
                DistrictPolicies.CityPlanning cityPlanningPolicies = instance2.m_districts.m_buffer[(int)district].m_cityPlanningPolicies;
                if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.BikeBan) != DistrictPolicies.CityPlanning.None)
                {
                    data.m_flags |= NetSegment.Flags.BikeBan;
                }
                else
                {
                    data.m_flags &= ~NetSegment.Flags.BikeBan;
                }
                data.m_problems = problem;
            }
            if ((data.m_flags & NetSegment.Flags.Collapsed) != NetSegment.Flags.None && (ulong)(Singleton<SimulationManager>.instance.m_currentFrameIndex >> 8 & 15u) == (ulong)((long)(segmentID & 15)))
            {
                int delta = Mathf.RoundToInt(data.m_averageLength);
                StatisticBase statisticBase = Singleton<StatisticsManager>.instance.Acquire<StatisticInt32>(StatisticType.DestroyedLength);
                statisticBase.Add(delta);
            }

        }
    }
}
