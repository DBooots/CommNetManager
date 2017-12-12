using System;
using System.Collections.Generic;

namespace CommNetManagerAPI
{
    /// <summary>
    /// An Interface for CommNetManager's implementation of <see cref="CommNet.CommNetHome"/>.
    /// </summary>
    /// <seealso cref="CommNet.CommNetHome" />
    public interface ICNMHome
    {
        /// <summary>
        /// The modular <see cref="CNMHomeComponent"/>s  implemented in this type.
        /// </summary>
        List<CNMHomeComponent> Components { get; }
        
        /// <summary>
        /// The altitude of this station.
        /// </summary>
        double Alt { get; }
        /// <summary>
        /// The longitude of this station.
        /// </summary>
        double Lon { get; }
        /// <summary>
        /// The latitude of this station.
        /// </summary>
        double Lat { get; }
        /// <summary>
        /// The <see cref="CelestialBody"/> on which this Home is located.
        /// </summary>
        CelestialBody Body { get; }
        /// <summary>
        /// The <see cref="CommNet.CommNode"/> attached to this Home.
        /// </summary>
        CommNet.CommNode Comm { get; }

        /// <summary>
        /// Initializes this instance based on the specified stock home.
        /// </summary>
        /// <param name="stockHome">The stock home.</param>
        void Initialize(CommNet.CommNetHome stockHome);
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Attaches a <see cref="CNMHomeComponent"/>.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <returns><c>true</c> if successful.</returns>
        bool AttachComponent(Type componentType);

        /// <summary>
        /// Gets the <see cref="CNMHomeComponent"/>  instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to get.</typeparam>
        /// <returns></returns>
        T GetModuleOfType<T>() where T : CNMHomeComponent;
        /// <summary>
        /// Gets the <see cref="CNMHomeComponent"/>  instance of the specified type.
        /// </summary>
        /// <param name="type">The type to get.</param>
        /// <returns></returns>
        CNMHomeComponent GetModuleOfType(Type type);
    }
}
