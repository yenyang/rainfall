using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using UnityEngine;
using ICities;

namespace Rainfall
{
    public class RFSerializableDataExtension : ISerializableDataExtension
    {
        public void OnCreated(ISerializableData serializedData)
        {

        }
        public void OnReleased()
        {

        }
        public void OnLoadData()
        {

        }
        public void OnSaveData()
        {
            if (DrainageBasinGrid.areYouAwake() && LoadingFunctions.loaded == true)
            {
                DrainageBasinGrid.Clear();
            }
        }

    }
}
