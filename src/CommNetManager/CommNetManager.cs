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
    public class CommNetManager : CommNetNetwork, ICommNetManager
    {
        /// <summary>
        /// Get a list of all <see cref="ICNMBody"/> objects. Not cached.
        /// </summary>
        public List<ICNMBody> Bodies
        {
            get { return FindObjectsOfType<CNMBody>().Cast<ICNMBody>().ToList(); }
        }
        List<ICNMBody> ICommNetManager.Bodies { get { return Bodies; } }

        /// <summary>
        /// Get a list of all <see cref="ICNMHome"/> objects. Not cached.
        /// </summary>
        static public List<ICNMHome> Homes
        {
            get { return FindObjectsOfType<CNMHome>().Cast<ICNMHome>().ToList(); }
        }
        List<ICNMHome> ICommNetManager.Homes { get { return Homes; } }

        /// <summary>
        /// Sets the <see cref="CommNet"/> types to be CommNetManager types instead.
        /// </summary>
        /// <returns><c>true</c> if successful.</returns>
        public bool SetCommNetTypes()
        {
            if (this != Instance)
                return Instance.SetCommNetTypes();

            //Replace the CommNet ground stations
            CommNetHome[] homes = FindObjectsOfType<CommNetHome>();
            for (int i = 0; i < homes.Length; i++)
            {
                if (homes[i] is CNMHome)
                    continue;
                CNMHome customHome = homes[i].gameObject.AddComponent<CNMHome>();
                customHome.Initialize(homes[i]);
                Destroy(homes[i]);
            }

            //Replace the CommNet celestial bodies
            CommNetBody[] bodies = FindObjectsOfType<CommNetBody>();
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] is CNMBody)
                    continue;
                CNMBody customBody = bodies[i].gameObject.AddComponent<CNMBody>();
                customBody.Initialize(bodies[i]);
                Destroy(bodies[i]);
            }
            return true;
        }

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
