using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommNet;

namespace CommNetManagerAPI
{
    /// <summary>
    /// A version of CommNetVessel with certain methods exposed.
    /// </summary>
    public interface PublicCommNetVessel
    {
        /// <summary>
        /// Update
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void Update(ModularCommNetVesselComponent callingInstance);
        /// <summary>
        /// Called when network initialized.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void OnNetworkInitialized(ModularCommNetVesselComponent callingInstance);
        /// <summary>
        /// Called when network pre update.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void OnNetworkPreUpdate(ModularCommNetVesselComponent callingInstance);
        /// <summary>
        /// Called when network post update.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void OnNetworkPostUpdate(ModularCommNetVesselComponent callingInstance);
        /// <summary>
        /// Calculates the plasma mult.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void CalculatePlasmaMult(ModularCommNetVesselComponent callingInstance);
        /// <summary>
        /// Updates the Comm field.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void UpdateComm(ModularCommNetVesselComponent callingInstance);
        /// <summary>
        /// Creates the control connection.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <returns></returns>
        bool CreateControlConnection(ModularCommNetVesselComponent callingInstance);
        /// <summary>
        /// Gets the best transmitter.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <returns></returns>
        IScienceDataTransmitter GetBestTransmitter(ModularCommNetVesselComponent callingInstance);
        /// <summary>
        /// Gets the control level.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <returns></returns>
        Vessel.ControlLevel GetControlLevel(ModularCommNetVesselComponent callingInstance);
        /// <summary>
        /// Called when map focus changes.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <param name="target">The target.</param>
        void OnMapFocusChange(ModularCommNetVesselComponent callingInstance, MapObject target);
        /// <summary>
        /// Gets the signal strength modifier.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <param name="b">The other node.</param>
        /// <returns></returns>
        double GetSignalStrengthModifier(ModularCommNetVesselComponent callingInstance, CommNode b);
    }
}
