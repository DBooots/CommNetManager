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
        void Update(ModularCommNetVessel callingInstance);
        /// <summary>
        /// Called when network initialized.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void OnNetworkInitialized(ModularCommNetVessel callingInstance);
        /// <summary>
        /// Called when network pre update.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void OnNetworkPreUpdate(ModularCommNetVessel callingInstance);
        /// <summary>
        /// Called when network post update.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void OnNetworkPostUpdate(ModularCommNetVessel callingInstance);
        /// <summary>
        /// Calculates the plasma mult.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void CalculatePlasmaMult(ModularCommNetVessel callingInstance);
        /// <summary>
        /// Updates the Comm field.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        void UpdateComm(ModularCommNetVessel callingInstance);
        /// <summary>
        /// Creates the control connection.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <returns></returns>
        bool CreateControlConnection(ModularCommNetVessel callingInstance);
        /// <summary>
        /// Gets the best transmitter.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <returns></returns>
        IScienceDataTransmitter GetBestTransmitter(ModularCommNetVessel callingInstance);
        /// <summary>
        /// Gets the control level.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <returns></returns>
        Vessel.ControlLevel GetControlLevel(ModularCommNetVessel callingInstance);
        /// <summary>
        /// Called when map focus changes.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <param name="target">The target.</param>
        void OnMapFocusChange(ModularCommNetVessel callingInstance, MapObject target);
        /// <summary>
        /// Gets the signal strength modifier.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <param name="b">The other node.</param>
        /// <returns></returns>
        double GetSignalStrengthModifier(ModularCommNetVessel callingInstance, CommNode b);
    }
}
