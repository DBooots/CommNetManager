namespace CommNetManagerAPI
{
    /// <summary>
    /// Derive from this class for CommNetManager to incorporate the methods into the CommNetHome.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    public abstract class CNMHomeComponent : UnityEngine.MonoBehaviour
    {
        /// <summary>
        /// The CommNetHome to which this component is attached.
        /// </summary>
        public CommNet.CommNetHome CommNetHome
        {
            get; set;
        }
        /// <summary>
        /// Initializes the <see cref="CNMHomeComponent"/>.
        /// </summary>
        /// <param name="home">The linked CommNetHome.</param>
        public virtual void Initialize(CommNet.CommNetHome home)
        {
        }
        /*/// <summary>
        /// Creates the CommNode.
        /// </summary>
        protected virtual void CreateNode() { }*/
        /// <summary>
        /// Called when network initialized.
        /// </summary>
        protected virtual void OnNetworkInitialized() { }
        /// <summary>
        /// Called when network pre update.
        /// </summary>
        protected virtual void OnNetworkPreUpdate() { }
        /// <summary>
        /// Start
        /// </summary>
        protected virtual void Start() { }
        /// <summary>
        /// Update
        /// </summary>
        protected virtual void Update() { }
    }
}
