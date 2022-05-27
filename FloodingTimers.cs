
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Math;
using ICities;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace Rainfall
{
	public class FloodingTimers : ThreadingExtensionBase
	{
        private BuildingManager _buildingManager;
        private NetManager _netManager;
        public static FloodingTimers instance = null;
        private int _buildingCapacity;
        private int _segmentCapacity;
        private float _totalUnpausedRealTimeDelta;
        private float[] _buildingFloodingStartTime;
        private float[] _segmentFloodingStartTime;
        private float[] _buildingFloodedStartTime;
        private float[] _segmentFloodedStartTime;
        private int[] _lastHandleCommonConsumptionEfficiency;

        public override void OnCreated(IThreading threading)
        {
            _buildingManager = Singleton<BuildingManager>.instance;
            _netManager = Singleton<NetManager>.instance;
            instance = this;
            _buildingCapacity = _buildingManager.m_buildings.m_buffer.Length;
            _segmentCapacity = _netManager.m_segments.m_buffer.Length;
            _buildingFloodingStartTime = new float[_buildingCapacity];
            _buildingFloodedStartTime = new float[_buildingCapacity];
            _segmentFloodingStartTime = new float[_segmentCapacity];
            _segmentFloodedStartTime = new float[_segmentCapacity];
            _totalUnpausedRealTimeDelta = 0f;
            _lastHandleCommonConsumptionEfficiency = new int[_buildingCapacity];
            base.OnCreated(threading);
        }
        public FloodingTimers()
        {

        }
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (simulationTimeDelta > 0f)
            {
                _totalUnpausedRealTimeDelta += realTimeDelta;
            }
            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }
        
        public void setBuildingFloodingStartTime(ushort buildingID)
        {
            if (buildingID >= 0 && buildingID < _buildingCapacity && _buildingFloodingStartTime != null)
            {
                _buildingFloodingStartTime[buildingID] = _totalUnpausedRealTimeDelta;
            }
        }
        public float getBuildingFloodingElapsedTime(ushort buildingID)
        {
            if (buildingID >= 0 && buildingID < _buildingCapacity && _buildingFloodingStartTime != null)
            {
                if (_buildingFloodingStartTime[buildingID] != 0f)
                    return (_totalUnpausedRealTimeDelta - _buildingFloodingStartTime[buildingID]);
            }
            return -1f;
        }
        public void resetBuildingFloodingStartTime(ushort buildingID)
        {
            if (buildingID >= 0 && buildingID < _buildingCapacity && _buildingFloodingStartTime != null)
            {
                _buildingFloodingStartTime[buildingID] = 0f;
            }
        }

        public void setBuildingFloodedStartTime(ushort buildingID)
        {
            if (buildingID >= 0 && buildingID < _buildingCapacity && _buildingFloodedStartTime != null)
            {
                _buildingFloodedStartTime[buildingID] = _totalUnpausedRealTimeDelta;
            }
        }
        public float getBuildingFloodedElapsedTime(ushort buildingID)
        {
            if (buildingID >= 0 && buildingID < _buildingCapacity && _buildingFloodedStartTime != null)
            {
                if (_buildingFloodedStartTime[buildingID] != 0f)
                    return (_totalUnpausedRealTimeDelta - _buildingFloodedStartTime[buildingID]);
            }
            return -1f;
        }
        public void resetBuildingFloodedStartTime(ushort buildingID)
        {
            if (buildingID >= 0 && buildingID < _buildingCapacity && _buildingFloodedStartTime != null)
            {
                _buildingFloodedStartTime[buildingID] = 0f;
            }
        }

        public void setSegmentFloodingStartTime(ushort segmentID)
        {
            if (segmentID >= 0 && segmentID < _segmentCapacity && _segmentFloodingStartTime != null)
            {
                _segmentFloodingStartTime[segmentID] = _totalUnpausedRealTimeDelta;
            }
        }
        public float getSegmentFloodingElapsedTime(ushort segmentID)
        {
            if (segmentID >= 0 && segmentID < _segmentCapacity && _segmentFloodingStartTime != null)
            {
                if (_segmentFloodingStartTime[segmentID] != 0f)
                    return (_totalUnpausedRealTimeDelta - _segmentFloodingStartTime[segmentID]);
            }
            return -1f;
        }
        public void resetSegmentFloodingStartTime(ushort segmentID)
        {
            if (segmentID >= 0 && segmentID < _segmentCapacity && _segmentFloodingStartTime != null)
            {
                _segmentFloodingStartTime[segmentID] = 0f;
            }
        }

        public void setSegmentFloodedStartTime(ushort segmentID)
        {
            if (segmentID >= 0 && segmentID < _segmentCapacity && _segmentFloodedStartTime != null)
            {
                _segmentFloodedStartTime[segmentID] = _totalUnpausedRealTimeDelta;
            }
        }
        public float getSegmentFloodedElapsedTime(ushort segmentID)
        {
            if (segmentID >= 0 && segmentID < _segmentCapacity && _segmentFloodedStartTime != null)
            {
                if (_segmentFloodedStartTime[segmentID] != 0f)
                    return (_totalUnpausedRealTimeDelta - _segmentFloodedStartTime[segmentID]);
            }
            return -1f;
        }
        public void resetSegmentFloodedStartTime(ushort segmentID)
        {
            if (segmentID >= 0 && segmentID < _segmentCapacity && _segmentFloodedStartTime != null)
            {
                _segmentFloodedStartTime[segmentID] = 0f;
            }
        }

        public void setLastHandleCommonConsumptionEfficiency(ushort buildingID, int efficiency)
        {
            if (buildingID >= 0 && buildingID < _buildingCapacity && _lastHandleCommonConsumptionEfficiency != null)
            {
                if (efficiency >= 0)
                {
                    _lastHandleCommonConsumptionEfficiency[buildingID] = efficiency;
                }
            }
        }
        public int getLastHandleCommonConsumptionEfficiency(ushort buildingID)
        {
            if (buildingID >= 0 && buildingID < _buildingCapacity && _lastHandleCommonConsumptionEfficiency != null)
            {
                return _lastHandleCommonConsumptionEfficiency[buildingID];
            }
            return 0;
        }
    }
}
