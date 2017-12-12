using CommNet;
using System;
using System.Collections.Generic;

namespace CommNetManagerAPI
{
    /// <summary>
    /// An Interface for CommNetManager's implementation of <see cref="CommNetBody"/>.
    /// </summary>
    /// <seealso cref="CommNet.CommNetBody" />
    public interface ICNMBody
    {
        /// <summary>
        /// The modular <see cref="CNMBodyComponent"/>s implemented in this type.
        /// </summary>
        List<CNMBodyComponent> Components { get; }
        /// <summary>
        /// The <see cref="CelestialBody"/> attached to this body.
        /// </summary>
        CelestialBody Body { get; }
        /// <summary>
        /// The <see cref="Occluder"/> attached to this body.
        /// </summary>
        Occluder Occluder { get; }

        /// <summary>
        /// Initializes this instance based on the specified stock body.
        /// </summary>
        /// <param name="stockBody">The stock body.</param>
        void Initialize(CommNetBody stockBody);
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Attaches a <see cref="CNMBodyComponent"/>.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <returns><c>true</c> if successful.</returns>
        bool AttachComponent(Type componentType);

        /// <summary>
        /// Gets the <see cref="CNMBodyComponent"/>  instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to get.</typeparam>
        /// <returns></returns>
        T GetModuleOfType<T>() where T : CNMBodyComponent;
        /// <summary>
        /// Gets the <see cref="CNMBodyComponent"/>  instance of the specified type.
        /// </summary>
        /// <param name="type">The type to get.</param>
        /// <returns></returns>
        CNMBodyComponent GetModuleOfType(Type type);

        /// <summary>
        /// Called when network pre update.
        /// </summary>
        void OnNetworkPreUpdate();
    }
}
