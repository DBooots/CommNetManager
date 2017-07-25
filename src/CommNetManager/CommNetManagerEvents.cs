using UnityEngine;

namespace CommNetManager
{
    /// <summary>
    /// Contains custom GameEvents that fire before and after the network Update() method runs.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class CommNetManagerEvents : MonoBehaviour
    {
        /// <summary>Fired at the beginning of the UpdateNetwork() method.</summary>
        public static EventData<CommNet.CommNetNetwork, CommNet.CommNetwork> onCommNetPreUpdate;
        /// <summary>Fired at the end of the UpdateNetwork() method.</summary>
        public static EventData<CommNet.CommNetNetwork, CommNet.CommNetwork> onCommNetPostUpdate;

        /// <summary>Registers the custom GameEvents. Do not attempt to link to them before this method is called.</summary>
        public void Awake()
        {
            Debug.Log("CommNetManager: Registering custom events.");
            onCommNetPreUpdate = new EventData<CommNet.CommNetNetwork, CommNet.CommNetwork>("onCommNetPreUpdate");
            onCommNetPostUpdate = new EventData<CommNet.CommNetNetwork, CommNet.CommNetwork>("onCommNetPostUpdate");
        }
    }
}
