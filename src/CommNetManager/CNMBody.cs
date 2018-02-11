using System;
using System.Collections.Generic;
using System.Linq;
using CommNet;
using System.Reflection;
using UnityEngine;
using CommNetManagerAPI;

namespace CommNetManager
{
    /// <summary>
    /// CommNetManager's implementation of <see cref="CommNetBody"/>.
    /// </summary>
    /// <seealso cref="CommNet.CommNetBody" />
    public sealed class CNMBody : CommNetBody , ICNMBody
    {
        Logger log = new Logger("CNMBody:");
        private SequenceList<Func<Occluder>> Sequence_CreateOccluder = new SequenceList<Func<Occluder>>();
        private SequenceList<Action> Sequence_OnNetworkInitialized = new SequenceList<Action>();
        private SequenceList<Action> Sequence_OnNetworkPreUpdate = new SequenceList<Action>();
        private SequenceList<Action> Sequence_Start = new SequenceList<Action>();
        /// <summary>
        /// The modular <see cref="CNMBodyComponent"/>s implemented in this type.
        /// </summary>
        public List<CNMBodyComponent> Components { get; internal set; } = new List<CNMBodyComponent>();
        private static Dictionary<MethodInfo, Type> methodTypes = new Dictionary<MethodInfo, Type>();
        private Dictionary<Type, CNMBodyComponent> modularRefs = new Dictionary<Type, CNMBodyComponent>();
        private static Dictionary<string, SequenceList<MethodInfo>> methodsSequence = new Dictionary<string, SequenceList<MethodInfo>>();
        private static bool methodsLoaded = false;
        private bool componentsInitialized = false;
        private static List<Type> modularTypes = null;

        /// <summary>
        /// The <see cref="CelestialBody"/> attached to this body.
        /// </summary>
        public CelestialBody Body { get { return this.body; } }
        /// <summary>
        /// The <see cref="Occluder"/> attached to this body.
        /// </summary>
        public Occluder Occluder { get { return this.occluder; } }

        /// <summary>
        /// Initializes this instance based on the specified stock body.
        /// </summary>
        /// <param name="stockBody">The stock body.</param>
        public void Initialize(CommNetBody stockBody)
        {
            this.body = stockBody.GetComponentInChildren<CelestialBody>();

            //this.occluder is initalised by OnNetworkInitialized() later
            this.Initialize();
        }
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            if (!methodsLoaded)
                LoadModularTypes();
            if (!this.componentsInitialized)
            {
                this.InstantiateModularTypes();
                componentsInitialized = true;
            }
        }
        /// <summary>
        /// Gets the <see cref="CNMBodyComponent"/>  instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to get.</typeparam>
        /// <returns></returns>
        public T GetModuleOfType<T>() where T : CNMBodyComponent
        {
            CNMBodyComponent value;
            if (!modularRefs.TryGetValue(typeof(T), out value))
                return null;
            return (T)value;
        }
        /// <summary>
        /// Gets the <see cref="CNMBodyComponent"/>  instance of the specified type.
        /// </summary>
        /// <param name="type">The type to get.</param>
        /// <returns></returns>
        public CNMBodyComponent GetModuleOfType(Type type)
        {
            CNMBodyComponent value;
            if (!modularRefs.TryGetValue(type, out value))
                return null;
            return value;
        }

        /// <summary>
        /// Creates the occluder.
        /// </summary>
        /// <returns></returns>
        protected override Occluder CreateOccluder()
        {
            Occluder value;
            for (int i = 0; i < Sequence_CreateOccluder.EarlyLate.Count; i++)
            {
                try { Sequence_CreateOccluder.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            value = base.CreateOccluder();
            for (int i = 0; i < Sequence_CreateOccluder.Post.Count; i++)
            {
                try { Sequence_CreateOccluder.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            return value;
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
            GameEvents.CommNet.OnNetworkInitialized.Remove(this.OnNetworkInitialized);
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
        public override void OnNetworkPreUpdate()
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
            this.body = this.GetComponent<CelestialBody>();
            this.Initialize();
            if (CommNetNetwork.Initialized)
                this.OnNetworkInitialized();
            GameEvents.CommNet.OnNetworkInitialized.Add(this.OnNetworkInitialized);

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

        internal static void LoadModularTypes()
        {
            modularTypes = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                modularTypes.AddRange(assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(CNMBodyComponent))));
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
            Sequence_CreateOccluder.Clear();
            Sequence_OnNetworkInitialized.Clear();
            Sequence_OnNetworkPreUpdate.Clear();
            Sequence_Start.Clear();

            log.debug("Instantiate " + modularTypes.Count);
            foreach (Type type in modularTypes)
            {
                if (type == typeof(CNMBodyComponent))
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
        /// Attaches a <see cref="CNMBodyComponent"/>.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <returns><c>true</c> if successful.</returns>
        public bool AttachComponent(Type componentType)
        {
            CNMBodyComponent modularCommNetBodyInstance = null;
            try
            {
                modularCommNetBodyInstance = gameObject.AddComponent(componentType) as CNMBodyComponent;
                modularCommNetBodyInstance.Initialize(this);
            }
            catch (Exception ex)
            {
                log.error("Encountered an exception while calling the constructor for " + componentType.Name);
                log.error(ex);
            }
            if (modularCommNetBodyInstance != null)
            {
                Components.Add(modularCommNetBodyInstance);
                modularRefs.Add(componentType, modularCommNetBodyInstance);
                log.debug("Activated an instance of type: " + componentType.Name);
                return true;
            }
            else
                log.error("Failed to activate " + componentType.Name);
            return false;
        }

        private void ParseDelegates(string methodName, MethodInfo method, CNMAttrSequence.options sequence)
        {
            CNMBodyComponent instance = modularRefs[methodTypes[method]];
    #if DEBUG
            Debug.LogFormat("CommNetManager: Parsing {0} from {1} as {2}.", methodName, instance.GetType().Name, sequence);
    #endif

            try
            {
                switch (methodName)
                {
                    case "CreateOccluder":
                        Sequence_CreateOccluder.Add(sequence, Delegate.CreateDelegate(typeof(Func<Occluder>), instance, method) as Func<Occluder>);
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
