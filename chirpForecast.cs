using ColossalFramework;
using ICities;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Rainfall
{
    public class ChirpForecast : IChirperMessage 
    {
        public static void SendMessage(string senderName, string message)
        {
            ChirpPanel cp = ChirpPanel.instance;
            if (cp == null)
                return;

            cp.AddMessage(new ChirpForecast() { senderName = senderName, text = message });

        }
        public uint senderID
        {
            get
            {
                return 0;
            }
        }
        public string senderName
        {
            get; set;
        }
        public string text
        {
            get; set;
        }
    }
}
