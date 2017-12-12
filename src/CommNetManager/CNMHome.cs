using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CommNetManagerAPI;

namespace CommNetManager
{
    /// <summary>
    /// CommNetManager's implementation of <see cref="CommNet.CommNetHome"/>.
    /// </summary>
    /// <seealso cref="CommNet.CommNetHome" />
    public sealed class CNMHome : CommNet.CommNetHome, ICNMHome
    {
        Logger log = new Logger("CNMHome:");
        private SequenceList<Action> Sequence_CreateNode = new SequenceList<Action>();
        private SequenceList<Action> Sequence_OnNetworkInitialized = new SequenceList<Action>();
        private SequenceList<Action> Sequence_OnNetworkPreUpdate = new SequenceList<Action>();
        private SequenceList<Action> Sequence_Start = new SequenceList<Action>();
        private SequenceList<Action> Sequence_Update = new SequenceList<Action>();
        /// <summary>
        /// The modular CNMHomeComponents implemented in this type.
        /// </summary>
        public List<CNMHomeComponent> Components { get; internal set; } = new List<CNMHomeComponent>();
        private static Dictionary<MethodInfo, Type> methodTypes = new Dictionary<MethodInfo, Type>();
        private Dictionary<Type, CNMHomeComponent> modularRefs = new Dictionary<Type, CNMHomeComponent>();
        private static Dictionary<string, SequenceList<MethodInfo>> methodsSequence = new Dictionary<string, SequenceList<MethodInfo>>();
        private static bool methodsLoaded = false;
        private static List<Type> modularTypes = null;

        /// <summary>
        /// The altitude of this station.
        /// </summary>
        public double Alt { get { return this.alt; } }
        /// <summary>
        /// The longitude of this station.
        /// </summary>
        public double Lon { get { return this.lon; } }
        /// <summary>
        /// The latitude of this station.
        /// </summary>
        public double Lat { get { return this.lat; } }
        /// <summary>
        /// The <see cref="CelestialBody"/> on which this Home is located.
        /// </summary>
        public CelestialBody Body { get { return this.body; } }
        /// <summary>
        /// The <see cref="CommNet.CommNode"/> attached to this Home.
        /// </summary>
        public CommNet.CommNode Comm { get { return this.comm; } }

        /// <summary>
        /// Initializes this instance based on the specified stock home.
        /// </summary>
        /// <param name="stockHome">The stock home.</param>
        public void Initialize(CommNet.CommNetHome stockHome)
        {
            this.nodeName = stockHome.nodeName;
            this.nodeTransform = stockHome.nodeTransform;
            this.isKSC = stockHome.isKSC;
            this.body = stockHome.GetComponentInParent<CelestialBody>();
            //comm, lat, alt, lon are initialised by CreateNode() later
            this.Initialize();
        }
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (!methodsLoaded)
                LoadModularTypes();
            this.InstantiateModularTypes();
        }

        /// <summary>
        /// Gets the <see cref="CNMHomeComponent"/>  instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to get.</typeparam>
        /// <returns></returns>
        public T GetModuleOfType<T>() where T : CNMHomeComponent
        {
            CNMHomeComponent value;
            if (!modularRefs.TryGetValue(typeof(T), out value))
                return null;
            return (T)value;
        }
        /// <summary>
        /// Gets the <see cref="CNMHomeComponent"/>  instance of the specified type.
        /// </summary>
        /// <param name="type">The type to get.</param>
        /// <returns></returns>
        public CNMHomeComponent GetModuleOfType(Type type)
        {
            CNMHomeComponent value;
            if (!modularRefs.TryGetValue(type, out value))
                return null;
            return value;
        }

        /// <summary>
        /// Creates the CommNode.
        /// </summary>
        protected override void CreateNode()
        {
            for (int i = 0; i < Sequence_CreateNode.EarlyLate.Count; i++)
            {
                try { Sequence_CreateNode.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.CreateNode();
            for (int i = 0; i < Sequence_CreateNode.Post.Count; i++)
            {
                try { Sequence_CreateNode.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }
        /// <summary>
        /// Per Unity docs.
        /// </summary>
        protected override void OnDestroy()
        {
            for (int i = this.Components.Count - 1; i >= 0; i--)
            {
                Destroy(Components[i]);
            }
            base.OnDestroy();
        }
        /// <summary>
        /// Called when network initialized.
        /// </summary>
        protected override void OnNetworkInitialized()
        {
            for (int i = 0; i < Sequence_OnNetworkInitialized.EarlyLate.Count; i++)
            {
                try { Sequence_OnNetworkInitialized.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.OnNetworkInitialized();
            for (int i = 0; i < Sequence_OnNetworkInitialized.Post.Count; i++)
            {
                try { Sequence_OnNetworkInitialized.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }
        /// <summary>
        /// Called when network pre update.
        /// </summary>
        protected override void OnNetworkPreUpdate()
        {
            for (int i = 0; i < Sequence_OnNetworkPreUpdate.EarlyLate.Count; i++)
            {
                try { Sequence_OnNetworkPreUpdate.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.OnNetworkPreUpdate();
            for (int i = 0; i < Sequence_OnNetworkPreUpdate.Post.Count; i++)
            {
                try { Sequence_OnNetworkPreUpdate.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }
        /// <summary>
        /// Start
        /// </summary>
        protected override void Start()
        {
            this.body = this.GetComponentInParent<CelestialBody>();
            if ((UnityEngine.Object)this.nodeTransform == (UnityEngine.Object)null)
                this.nodeTransform = this.transform;
            this.Initialize();
            if (CommNet.CommNetNetwork.Initialized)
                this.OnNetworkInitialized();
            GameEvents.CommNet.OnNetworkInitialized.Add(new EventVoid.OnEvent(this.OnNetworkInitialized));

            /*for (int i = 0; i < Sequence_Start.EarlyLate.Count; i++)
            {
                try { Sequence_Start.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.Start();
            for (int i = 0; i < Sequence_Start.Post.Count; i++)
            {
                try { Sequence_Start.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }*/
        }
        /// <summary>
        /// Update
        /// </summary>
        protected override void Update()
        {
            for (int i = 0; i < Sequence_Update.EarlyLate.Count; i++)
            {
                try { Sequence_Update.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.Update();
            for (int i = 0; i < Sequence_Update.Post.Count; i++)
            {
                try { Sequence_Update.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        internal static void LoadModularTypes()
        {
            modularTypes = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                modularTypes.AddRange(assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(CNMHomeComponent))));
                // typeof(ModularCommNetVessel).IsAssignableFrom(type) && !type.IsAbstract).ToList());
            }

            foreach (Type type in modularTypes)
            {
                MethodInfo[] methodsInType = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (MethodInfo method in methodsInType)
                {
                    if (method.DeclaringType != type)
                        continue;
                    methodTypes.Add(method, type);

                    if (!methodsSequence.ContainsKey(method.Name))
                        methodsSequence.Add(method.Name, new SequenceList<MethodInfo>());

                    if (Attribute.IsDefined(method, typeof(CNMAttrSequence)))
                    {
                        CNMAttrSequence attr = Attribute.GetCustomAttribute(method, typeof(CNMAttrSequence)) as CNMAttrSequence;
                        methodsSequence[method.Name].Add(attr.when, method);
                    }
                    else
                    {
                        methodsSequence[method.Name].Add(CNMAttrSequence.options.LATE, method);
                    }
                }
            }
            methodsLoaded = true;
        }
        /// <summary>
        /// Instantiates the modular types.
        /// </summary>
        internal void InstantiateModularTypes()
        {
            modularRefs.Clear();
            Components.Clear();
            Sequence_OnNetworkInitialized.Clear();
            Sequence_OnNetworkPreUpdate.Clear();
            Sequence_Start.Clear();

            log.debug("Instantiate " + modularTypes.Count);
            foreach (Type type in modularTypes)
            {
                if (type == typeof(CNMHomeComponent))
                {
                    log.debug("Skipping type " + type.Name);
                    continue;
                }
                AttachComponent(type);
            }
            foreach (SequenceList<MethodInfo> methodList in methodsSequence.Values)
            {
                foreach (MethodInfo method in methodList.Early)
                {
                    if (!modularRefs.ContainsKey(methodTypes[method]))
                    {
                        log.warning("No instance of the CommNetwork type (" + methodTypes[method].DeclaringType.FullName.ToString() + ") was instantiated.");
                        continue;
                    }
                    ParseDelegates(method.Name, method, CNMAttrSequence.options.EARLY);
                }
                foreach (MethodInfo method in methodList.Late)
                {
                    if (!modularRefs.ContainsKey(methodTypes[method]))
                    {
                        log.warning("No instance of the CommNetwork type (" + methodTypes[method].DeclaringType.FullName.ToString() + ") was instantiated.");
                        continue;
                    }
                    ParseDelegates(method.Name, method, CNMAttrSequence.options.LATE);
                }
                foreach (MethodInfo method in methodList.Post)
                {
                    if (!modularRefs.ContainsKey(methodTypes[method]))
                    {
                        log.warning("No instance of the CommNetwork type (" + methodTypes[method].DeclaringType.FullName.ToString() + ") was instantiated.");
                        continue;
                    }
                    ParseDelegates(method.Name, method, CNMAttrSequence.options.POST);
                }
            }
        }

        /// <summary>
        /// Attaches a <see cref="CNMHomeComponent"/>.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <returns><c>true</c> if successful.</returns>
        public bool AttachComponent(Type componentType)
        {
            CNMHomeComponent modularCommNetHomeInstance = null;
            try
            {
                modularCommNetHomeInstance = gameObject.AddComponent(componentType) as CNMHomeComponent;
                modularCommNetHomeInstance.CommNetHome = this;
                modularCommNetHomeInstance.Initialize(this);
            }
            catch (Exception ex)
            {
                log.error("Encountered an exception while calling the constructor for " + componentType.Name);
                log.error(ex);
            }
            if (modularCommNetHomeInstance != null)
            {
                Components.Add(modularCommNetHomeInstance);
                modularRefs.Add(componentType, modularCommNetHomeInstance);
                log.debug("Activated an instance of type: " + componentType.Name);
                return true;
            }
            else
                log.warning("Failed to activate " + componentType.Name);
            return false;
        }

        private void ParseDelegates(string methodName, MethodInfo method, CNMAttrSequence.options sequence)
        {
            CNMHomeComponent instance = modularRefs[methodTypes[method]];
    #if DEBUG
            Debug.LogFormat("CNMHome: Parsing {0} from {1} as {2}.", methodName, instance.GetType().Name, sequence);
    #endif

            try
            {
                switch (methodName)
                {
                    case "CreateNode":
                        Sequence_CreateNode.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "OnNetworkInitialized":
                        Sequence_OnNetworkInitialized.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "OnNetworkPreUpdate":
                        Sequence_OnNetworkPreUpdate.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "Start":
                        Sequence_Start.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "Update":
                        Sequence_Update.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    default:
    #if DEBUG
                        log.warning("The method passed (" + methodName + ") was not a standard CommNet method.");
    #endif
                        return;
                }
                log.debug("Successfully parsed " + methodName + " from type " + instance.GetType().Name);
            }
            catch (Exception ex)
            {
                log.error("Encountered an error creating a delegate for " + methodName + " from type " + instance.GetType().Name);
                log.error(ex);
            }
        }
    }
}
