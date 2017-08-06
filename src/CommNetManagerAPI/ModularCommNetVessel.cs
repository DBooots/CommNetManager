using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommNet;

namespace CommNetManagerAPI
{
    /// <summary>
    /// Derive from this class for CommNetManager to incorporate the methods into the VesselModule.
    /// </summary>
    public class ModularCommNetVessel : UnityEngine.MonoBehaviour
    {
        /// <summary>
        /// Per KSP VesselModule
        /// </summary>
        protected Vessel vessel;
        /// <summary>
        /// Per KSP VesselModule
        /// </summary>
        public Vessel Vessel { get { return this.vessel; } }
        /// <summary>
        /// The CommNetVessel module to which this instance is attached.
        /// </summary>
        public ModularCommNetVesselModule CommNetVessel
        {
            get { return _CommNetVessel; }
            protected internal set
            {
                _CommNetVessel = value;
                CommNetVesselAsPublic = value;
                this.vessel = value.Vessel;
            }
        }
        private ModularCommNetVesselModule _CommNetVessel;
        /// <summary>
        /// The CommNetVessel module to which this instance is attached. Used for interface method calls.
        /// </summary>
        public PublicCommNetVessel CommNetVesselAsPublic
        {
            get;
            private set;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ModularCommNetVessel"/> class.
        /// </summary>
        /// <param name="actualCommNetVessel">The CommNetVessel to which this instance is attached.</param>
        public ModularCommNetVessel(ModularCommNetVesselModule actualCommNetVessel)
        {
            this.CommNetVessel = actualCommNetVessel;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ModularCommNetVessel"/> class.
        /// </summary>
        protected ModularCommNetVessel()
        {
        }

        /// <summary>
        /// Per Unity docs.
        /// </summary>
        public virtual void Awake() { }
        /// <summary>
        /// Per Unity docs.
        /// </summary>
        protected virtual void OnAwake() { }
        /// <summary>
        /// Per Unity docs.
        /// </summary>
        protected virtual void OnStart() { }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        protected virtual void Update() { CommNetVesselAsPublic.Update(this); }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        protected virtual void OnNetworkInitialized() { CommNetVesselAsPublic.OnNetworkInitialized(this); }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        public virtual void OnNetworkPreUpdate() { CommNetVesselAsPublic.OnNetworkPreUpdate(this); }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        public virtual void OnNetworkPostUpdate() { CommNetVesselAsPublic.OnNetworkPostUpdate(this); }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        protected virtual void CalculatePlasmaMult() { CommNetVesselAsPublic.CalculatePlasmaMult(this); }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        protected virtual void UpdateComm() { CommNetVesselAsPublic.UpdateComm(this); }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        protected virtual bool CreateControlConnection() { return CommNetVesselAsPublic.CreateControlConnection(this); }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        public virtual IScienceDataTransmitter GetBestTransmitter() { return CommNetVesselAsPublic.GetBestTransmitter(this); }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        public virtual Vessel.ControlLevel GetControlLevel() { return CommNetVesselAsPublic.GetControlLevel(this); }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        protected virtual void OnMapFocusChange(MapObject target) { CommNetVesselAsPublic.OnMapFocusChange(this, target); }
        /// <summary>
        /// CAUTION: DO NOT CALL base.method when deriving!
        /// </summary>
        public virtual double GetSignalStrengthModifier(CommNode b) { return CommNetVesselAsPublic.GetSignalStrengthModifier(this, b); }
        /// <summary>
        /// Per KSP docs.
        /// </summary>
        public virtual void OnGoOffRails() { }
        /// <summary>
        /// Per KSP docs.
        /// </summary>
        public virtual void OnGoOnRails() { }
        /// <summary>
        /// Per KSP docs.
        /// </summary>
        protected virtual void OnLoad(ConfigNode node) { }
        /// <summary>
        /// Per KSP docs.
        /// </summary>
        protected virtual void OnSave(ConfigNode node) { }
    }
}
