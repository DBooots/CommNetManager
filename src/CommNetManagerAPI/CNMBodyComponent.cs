namespace CommNetManagerAPI
{
    /// <summary>
    /// Derive from this class for CommNetManager to incorporate the methods into the CommNetBody.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    public class CNMBodyComponent : UnityEngine.MonoBehaviour
    {
        /// <summary>
        /// The CommNetBody to which this component is attached.
        /// </summary>
        public CommNet.CommNetBody CommNetBody
        {
            get; protected internal set;
        }

        /// <summary>
        /// Initializes the <see cref="CNMBodyComponent"/>.
        /// <para/> CAUTION: If overriding, you must call base.Initialize(body). 
        /// </summary>
        /// <param name="body">The linked CommNetBody.</param>
        public virtual void Initialize(CommNet.CommNetBody body)
        {
            this.CommNetBody = body;
        }
        /*/// <summary>
        /// Creates the occluder.
        /// </summary>
        /// <returns></returns>
        protected virtual Occluder CreateOccluder() { return null; }*/
        /// <summary>
        /// Called when network initialized.
        /// </summary>
        protected virtual void OnNetworkInitialized() { }
        /// <summary>
        /// Called when network pre update.
        /// </summary>
        public virtual void OnNetworkPreUpdate() { }
        /// <summary>
        /// Start
        /// </summary>
        protected virtual void Start() { }
    }
}
