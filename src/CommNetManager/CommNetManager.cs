using System;
using System.Collections.Generic;
using System.Linq;
using CommNet;
using System.Reflection;
using KSP;
using UnityEngine;
using CommNetManagerAPI;

namespace CommNetManager
{

    /// <summary>
    /// Extend the functionality of the KSP's CommNetNetwork (co-primary model in the Model–view–controller sense; CommNet<> is the other co-primary one)
    /// </summary>
    public class CommNetManager : CommNetNetwork
    {
        public static new CommNetManager Instance
        {
            get;
            protected set;
        }

        protected override void Awake()
        {
            CommNetNetwork.Instance = this;
            this.CommNet = new CommNetManagerNetwork();

            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                GameEvents.onPlanetariumTargetChanged.Add(new EventData<MapObject>.OnEvent(this.OnMapFocusChange));
            }

            GameEvents.OnGameSettingsApplied.Add(new EventVoid.OnEvent(this.ResetNetwork));
            ResetNetwork(); // Please retain this so that KSP can properly reset
        }

        protected new void ResetNetwork()
        {
            this.CommNet = new CommNetManagerNetwork();
            GameEvents.CommNet.OnNetworkInitialized.Fire();
        }
    }
}
