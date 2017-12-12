using System.Collections.Generic;

namespace CommNetManagerAPI
{
    /// <summary>
    /// An Interface for the CommNetManager implementation of <see cref="CommNet.CommNetNetwork"/>.
    /// </summary>
    public interface ICommNetManager
    {
        /// <summary>
        /// Get a list of all <see cref="ICNMBody"/> objects. Not cached.
        /// </summary>
        List<ICNMBody> Bodies { get; }
        /// <summary>
        /// Get a list of all <see cref="ICNMHome"/> objects. Not cached.
        /// </summary>
        List<ICNMHome> Homes { get; }

        /// <summary>
        /// Sets the <see cref="CommNet"/> types to be CommNetManager types instead.
        /// </summary>
        /// <returns><c>true</c> if successful.</returns>
        bool SetCommNetTypes();
    }
}
