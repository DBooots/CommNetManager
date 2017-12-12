using System;
using System.Collections.Generic;

namespace CommNetManagerAPI
{
    /// <summary>
    /// An Interface for the <see cref="CommNet.CommNetVessel"/>  instance used by CommNetManager.
    /// </summary>
    /// <seealso cref="CommNet.CommNetVessel" />
    /// <seealso cref="CommNetManagerAPI.IPublicCommNetVessel" />
    public interface IModularCommNetVessel
    {
        /// <summary>
        /// Per KSP docs.
        /// </summary>
        Vessel Vessel { get; set; }

        /// <summary>
        /// Per KSP docs.
        /// </summary>
        CommNet.CommNode Comm { get; set; }

        /// <summary>
        /// The <see cref="ModularCommNetVesselComponent"/>s implemented in this type.
        /// </summary>
        List<ModularCommNetVesselComponent> Components { get; }

        /// <summary>
        /// Gets the <see cref="ModularCommNetVesselComponent"/> instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to get.</typeparam>
        /// <returns></returns>
        T GetModuleOfType<T>() where T : ModularCommNetVesselComponent;
        /// <summary>
        /// Gets the <see cref="ModularCommNetVesselComponent"/> instance of the specified type.
        /// </summary>
        /// <param name="type">The type to get.</param>
        /// <returns></returns>
        ModularCommNetVesselComponent GetModuleOfType(Type type);

        /// <summary>
        /// Per KSP docs.
        /// </summary>
        void OnGoOffRails();
        /// <summary>
        /// Per KSP docs.
        /// </summary>
        void OnGoOnRails();

        /// <summary>
        /// Gets the best transmitter.
        /// </summary>
        /// <returns></returns>
        IScienceDataTransmitter GetBestTransmitter();
        /// <summary>
        /// Gets the control level.
        /// </summary>
        Vessel.ControlLevel GetControlLevel();
        /// <summary>
        /// Gets the signal strength modifier.
        /// </summary>
        /// <param name="b">The other node.</param>
        /// <returns></returns>
        double GetSignalStrengthModifier(CommNet.CommNode b);
    }
}
