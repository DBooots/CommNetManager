using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommNet;
using UnityEngine;
using System.Reflection;

namespace CommNetManagerAPI
{
    /// <summary>
    /// The CommNetVessel instance used by CommNetManager
    /// </summary>
    /// <seealso cref="CommNet.CommNetVessel" />
    /// <seealso cref="CommNetManagerAPI.PublicCommNetVessel" />
    public sealed class ModularCommNetVessel : CommNetVessel, PublicCommNetVessel
    {
        private static Assembly electionWinner = null;
        private static Dictionary<MethodInfo, Type> methodTypes = new Dictionary<MethodInfo, Type>();
        private Dictionary<Type, ModularCommNetVesselComponent> modularRefs = new Dictionary<Type, ModularCommNetVesselComponent>();
        private static Dictionary<string, SequenceList<MethodInfo>> methodsSequence = new Dictionary<string, SequenceList<MethodInfo>>();
        private static Dictionary<MethodInfo, CNMAttrAndOr.options> andOrList = new Dictionary<MethodInfo, CNMAttrAndOr.options>();
        private static bool methodsLoaded = false;
        private Dictionary<Delegate, CNMAttrAndOr.options> invokesAndOr = new Dictionary<Delegate, CNMAttrAndOr.options>();

        private static List<Type> modularTypes = null;
        /// <summary>
        /// The modular CommNetVessels implemented in this type.
        /// </summary>
        public List<ModularCommNetVesselComponent> ModularCommNetVessels { get; internal set; } = null;

        #region SequenceList<Delegate> for each inherited method

        private SequenceList<Action> Sequence_Awake = new SequenceList<Action>();
        private SequenceList<Action> Sequence_OnAwake = new SequenceList<Action>();
        private SequenceList<Action> Sequence_OnStart = new SequenceList<Action>();
        private SequenceList<Action> Sequence_OnDestroy = new SequenceList<Action>();
        private SequenceList<Action> Sequence_OnGoOffRails = new SequenceList<Action>();
        private SequenceList<Action> Sequence_OnGoOnRails = new SequenceList<Action>();
        private SequenceList<Action<ConfigNode>, ModularCommNetVesselComponent> Sequence_OnLoad = new SequenceList<Action<ConfigNode>, ModularCommNetVesselComponent>();
        private SequenceList<Action<ConfigNode>, ModularCommNetVesselComponent> Sequence_OnSave = new SequenceList<Action<ConfigNode>, ModularCommNetVesselComponent>();
        private SequenceList<Action, ModularCommNetVesselComponent> Sequence_Update = new SequenceList<Action, ModularCommNetVesselComponent>();
        private SequenceList<Action> Sequence_OnNetworkInitialized = new SequenceList<Action>();
        private SequenceList<Action> Sequence_OnNetworkPreUpdate = new SequenceList<Action>();
        private SequenceList<Action> Sequence_OnNetworkPostUpdate = new SequenceList<Action>();
        private SequenceList<Action, ModularCommNetVesselComponent> Sequence_CalculatePlasmaMult = new SequenceList<Action, ModularCommNetVesselComponent>();
        private SequenceList<Action, ModularCommNetVesselComponent> Sequence_UpdateComm = new SequenceList<Action, ModularCommNetVesselComponent>();
        private SequenceList<Func<bool>, Pair<CNMAttrAndOr.options, ModularCommNetVesselComponent>> Sequence_CreateControlConnection = new SequenceList<Func<bool>, Pair<CNMAttrAndOr.options, ModularCommNetVesselComponent>>();
        private SequenceList<Func<IScienceDataTransmitter>, ModularCommNetVesselComponent> Sequence_GetBestTransmitter = new SequenceList<Func<IScienceDataTransmitter>, ModularCommNetVesselComponent>();
        private SequenceList<Func<Vessel.ControlLevel>, ModularCommNetVesselComponent> Sequence_GetControlLevel = new SequenceList<Func<Vessel.ControlLevel>, ModularCommNetVesselComponent>();
        private SequenceList<Action<MapObject>> Sequence_OnMapFocusChange = new SequenceList<Action<MapObject>>();
        private SequenceList<Func<CommNode, double>, ModularCommNetVesselComponent> Sequence_GetSignalStrengthModifier = new SequenceList<Func<CommNode, double>, ModularCommNetVesselComponent>();

        private class Pair<T1, T2>
        {
            public T1 a;
            public T2 b;
            public Pair(T1 a, T2 b)
            {
                this.a = a;
                this.b = b;
            }
        }
        #endregion

        /// <summary>
        /// Gets the ModularCommNetVessel instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to get.</typeparam>
        /// <returns></returns>
        public T GetModuleOfType<T>() where T : ModularCommNetVesselComponent
        {
            ModularCommNetVesselComponent value;
            if (!modularRefs.TryGetValue(typeof(T), out value))
                return null;
            return (T)value;
            //return this.ModularCommNetVessels.FirstOrDefault(module => module is T);
        }
        /// <summary>
        /// Gets the ModularCommNetVessel instance of the specified type.
        /// </summary>
        /// <param name="type">The type to get.</param>
        /// <returns></returns>
        public ModularCommNetVesselComponent GetModuleOfType(Type type)
        {
            ModularCommNetVesselComponent value;
            if (!modularRefs.TryGetValue(type, out value))
                return null;
            return value;
            //return this.ModularCommNetVessels.FirstOrDefault(module => module.GetType() == type);
        }

        /// <summary>
        /// Per Unity docs.
        /// </summary>
        protected override void OnAwake()
        {
            if (!AmITheOne())
            {
                DestroyImmediate(this);
                return;
            }
            if (this.vessel == null)
            {
                Debug.LogWarning("OnAwake: Vessel is null.");
                return;
            }
            this.InstantiateModularTypes();
            for (int i = 0; i < Sequence_OnAwake.EarlyLate.Count; i++)
            {
                try { Sequence_OnAwake.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.OnAwake();
            for (int i = 0; i < Sequence_OnAwake.Post.Count; i++)
            {
                try { Sequence_OnAwake.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }
        /// <summary>
        /// Per Unity docs.
        /// </summary>
        protected override void OnStart()
        {
            Debug.Log("OnStart: " + this.vessel != null ? this.vessel.name : "");
            if (this.ModularCommNetVessels == null)
                this.InstantiateModularTypes();
            for (int i = 0; i < Sequence_OnStart.EarlyLate.Count; i++)
            {
                try { Sequence_OnStart.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.OnStart();
            for (int i = 0; i < Sequence_OnStart.Post.Count; i++)
            {
                try { Sequence_OnStart.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }
        /// <summary>
        /// Per Unity docs.
        /// </summary>
        protected override void OnDestroy()
        {
            foreach (ModularCommNetVesselComponent module in this.modularRefs.Values)
            {
                Destroy(module);
            }
            base.OnDestroy();
        }
        /// <summary>
        /// Per KSP docs.
        /// </summary>
        public override void OnGoOffRails()
        {
            for (int i = 0; i < Sequence_OnGoOffRails.EarlyLate.Count; i++)
            {
                try { Sequence_OnGoOffRails.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.OnGoOffRails();
            for (int i = 0; i < Sequence_OnGoOffRails.Post.Count; i++)
            {
                try { Sequence_OnGoOffRails.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }
        /// <summary>
        /// Per KSP docs.
        /// </summary>
        public override void OnGoOnRails()
        {
            for (int i = 0; i < Sequence_OnGoOnRails.EarlyLate.Count; i++)
            {
                try { Sequence_OnGoOnRails.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.OnGoOnRails();
            for (int i = 0; i < Sequence_OnGoOnRails.Post.Count; i++)
            {
                try { Sequence_OnGoOnRails.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }
        /// <summary>
        /// Per KSP docs.
        /// </summary>
        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            List<Action<ConfigNode>> all = Sequence_OnLoad.All;
            for (int i = 0; i < all.Count; i++)
            {
                ConfigNode newNode = node.GetNode(Sequence_OnLoad.MetaDict[all[i]].GetType().Name.Replace(' ', '_'));
                if (newNode != null)
                    Debug.Log("success!");
                else
                {
                    Debug.LogWarning("failure!");
                    continue;
                }
                try
                {
                    all[i].Invoke(newNode);
                    node.AddNode(newNode);
                }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }
        /// <summary>
        /// Per KSP docs.
        /// </summary>
        protected override void OnSave(ConfigNode node)
        {
            List<Action<ConfigNode>> all = Sequence_OnSave.All;
            for (int i = 0; i < all.Count; i++)
            {
                ConfigNode newNode = new ConfigNode(Sequence_OnSave.MetaDict[all[i]].GetType().Name.Replace(' ', '_'));
                try
                {
                    all[i].Invoke(newNode);
                    node.AddNode(newNode);
                }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.OnSave(node);
        }

        /// <summary>
        /// Update
        /// </summary>
        protected override void Update() { this.Update(null); }
        /// <summary>
        /// Calculates the plasma mult.
        /// </summary>
        protected override void CalculatePlasmaMult() { this.CalculatePlasmaMult(null); }
        /// <summary>
        /// Updates the Comm field.
        /// </summary>
        protected override void UpdateComm() { this.UpdateComm(null); }
        /// <summary>
        /// Creates the control connection.
        /// </summary>
        /// <returns></returns>
        protected override bool CreateControlConnection() { return this.CreateControlConnection(null); }
        /// <summary>
        /// Gets the best transmitter.
        /// </summary>
        /// <returns></returns>
        public override IScienceDataTransmitter GetBestTransmitter() { return this.GetBestTransmitter(null); }
        /// <summary>
        /// Gets the control level.
        /// </summary>
        public override Vessel.ControlLevel GetControlLevel() { return this.GetControlLevel(null); }
        /// <summary>
        /// Gets the signal strength modifier.
        /// </summary>
        /// <param name="b">The other node.</param>
        /// <returns></returns>
        public override double GetSignalStrengthModifier(CommNode b) { return this.GetSignalStrengthModifier(null, b); }

        #region PublicCommNetVessel methods
        
        /// <summary>
        /// Update
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        public void Update(ModularCommNetVesselComponent callingInstance)
        {
            for (int i = 0; i < Sequence_Update.EarlyLate.Count; i++)
            {
                if (Sequence_Update.MetaDict[Sequence_Update.EarlyLate[i]] == callingInstance)
                    continue;
                try { Sequence_Update.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.Update();
            for (int i = 0; i < Sequence_Update.Post.Count; i++)
            {
                if (Sequence_Update.MetaDict[Sequence_Update.Post[i]] == callingInstance)
                    continue;
                try { Sequence_Update.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        /// <summary>
        /// Called when network initialized.
        /// </summary>
        public void OnNetworkInitialized()
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
        public void OnNetworkPreUpdate()
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
        /// Called when network post update.
        /// </summary>
        public void OnNetworkPostUpdate()
        {
            for (int i = 0; i < Sequence_OnNetworkPostUpdate.EarlyLate.Count; i++)
            {
                try { Sequence_OnNetworkPostUpdate.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.OnNetworkPostUpdate();
            for (int i = 0; i < Sequence_OnNetworkPostUpdate.Post.Count; i++)
            {
                try { Sequence_OnNetworkPostUpdate.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        /// <summary>
        /// Calculates the plasma mult.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        public void CalculatePlasmaMult(ModularCommNetVesselComponent callingInstance)
        {
            for (int i = 0; i < Sequence_CalculatePlasmaMult.EarlyLate.Count; i++)
            {
                if (Sequence_CalculatePlasmaMult.MetaDict[Sequence_CalculatePlasmaMult.EarlyLate[i]] == callingInstance)
                    continue;
                try { Sequence_CalculatePlasmaMult.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.CalculatePlasmaMult();
            for (int i = 0; i < Sequence_CalculatePlasmaMult.Post.Count; i++)
            {
                if (Sequence_CalculatePlasmaMult.MetaDict[Sequence_CalculatePlasmaMult.Post[i]] == callingInstance)
                    continue;
                try { Sequence_CalculatePlasmaMult.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        /// <summary>
        /// Updates the Comm field.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        public void UpdateComm(ModularCommNetVesselComponent callingInstance)
        {
            for (int i = 0; i < Sequence_UpdateComm.EarlyLate.Count; i++)
            {
                if (Sequence_UpdateComm.MetaDict[Sequence_UpdateComm.EarlyLate[i]] == callingInstance)
                    continue;
                try { Sequence_UpdateComm.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.UpdateComm();
            for (int i = 0; i < Sequence_UpdateComm.Post.Count; i++)
            {
                if (Sequence_UpdateComm.MetaDict[Sequence_UpdateComm.Post[i]] == callingInstance)
                    continue;
                try { Sequence_UpdateComm.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        /// <summary>
        /// Creates the control connection.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <returns></returns>
        public bool CreateControlConnection(ModularCommNetVesselComponent callingInstance)
        {
            bool value = true;
            for (int i = 0; i < Sequence_CreateControlConnection.EarlyLate.Count; i++)
            {
                if (Sequence_CreateControlConnection.MetaDict[Sequence_CreateControlConnection.EarlyLate[i]].b == callingInstance)
                    continue;
                try { value = AndOr(value, Sequence_CreateControlConnection.EarlyLate[i].Invoke(), Sequence_CreateControlConnection.MetaDict[Sequence_CreateControlConnection.EarlyLate[i]].a); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            value &= base.CreateControlConnection();
            for (int i = 0; i < Sequence_CreateControlConnection.Post.Count; i++)
            {
                if (Sequence_CreateControlConnection.MetaDict[Sequence_CreateControlConnection.Post[i]].b == callingInstance)
                    continue;
                try { value = AndOr(value, Sequence_CreateControlConnection.Post[i].Invoke(), Sequence_CreateControlConnection.MetaDict[Sequence_CreateControlConnection.Post[i]].a); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            return value;
        }

        /// <summary>
        /// Gets the best transmitter.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <returns></returns>
        public IScienceDataTransmitter GetBestTransmitter(ModularCommNetVesselComponent callingInstance)
        {
            IScienceDataTransmitter value;
            for (int i = 0; i < Sequence_GetBestTransmitter.EarlyLate.Count; i++)
            {
                if (Sequence_GetBestTransmitter.MetaDict[Sequence_GetBestTransmitter.EarlyLate[i]] == callingInstance)
                    continue;
                try { Sequence_GetBestTransmitter.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            value = base.GetBestTransmitter();
            for (int i = 0; i < Sequence_GetBestTransmitter.Post.Count; i++)
            {
                if (Sequence_GetBestTransmitter.MetaDict[Sequence_GetBestTransmitter.Post[i]] == callingInstance)
                    continue;
                try { Sequence_GetBestTransmitter.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            return value;
        }

        /// <summary>
        /// Gets the control level.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        public Vessel.ControlLevel GetControlLevel(ModularCommNetVesselComponent callingInstance)
        {
            Vessel.ControlLevel value;
            for (int i = 0; i < Sequence_GetControlLevel.EarlyLate.Count; i++)
            {
                if (Sequence_GetControlLevel.MetaDict[Sequence_GetControlLevel.EarlyLate[i]] == callingInstance)
                    continue;
                try { Sequence_GetControlLevel.EarlyLate[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            value = base.GetControlLevel();
            for (int i = 0; i < Sequence_GetControlLevel.Post.Count; i++)
            {
                if (Sequence_GetControlLevel.MetaDict[Sequence_GetControlLevel.Post[i]] == callingInstance)
                    continue;
                try { Sequence_GetControlLevel.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            return value;
        }

        /// <summary>
        /// Called when map focus changes.
        /// </summary>
        /// <param name="target">The target.</param>
        public void OnMapFocusChange(MapObject target)
        {
            for (int i = 0; i < Sequence_OnMapFocusChange.EarlyLate.Count; i++)
            {
                try { Sequence_OnMapFocusChange.EarlyLate[i].Invoke(target); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            base.OnMapFocusChange(target);
            for (int i = 0; i < Sequence_OnMapFocusChange.Post.Count; i++)
            {
                try { Sequence_OnMapFocusChange.Post[i].Invoke(target); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        /// <summary>
        /// Gets the signal strength modifier.
        /// </summary>
        /// <param name="callingInstance">The calling instance.</param>
        /// <param name="b">The other node.</param>
        /// <returns></returns>
        public double GetSignalStrengthModifier(ModularCommNetVesselComponent callingInstance, CommNode b)
        {
            double value = 0;
            for (int i = 0; i < Sequence_GetSignalStrengthModifier.EarlyLate.Count; i++)
            {
                if (Sequence_GetSignalStrengthModifier.MetaDict[Sequence_GetSignalStrengthModifier.EarlyLate[i]] == callingInstance)
                    continue;
                try { value += Sequence_GetSignalStrengthModifier.EarlyLate[i].Invoke(b); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            value += base.GetSignalStrengthModifier(b);
            for (int i = 0; i < Sequence_GetSignalStrengthModifier.Post.Count; i++)
            {
                if (Sequence_GetSignalStrengthModifier.MetaDict[Sequence_GetSignalStrengthModifier.Post[i]] == callingInstance)
                    continue;
                try { value += Sequence_GetSignalStrengthModifier.Post[i].Invoke(b); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
            return value;
        }

        private bool AndOr(bool a, bool b, CNMAttrAndOr.options andOr)
        {
            switch (andOr)
            {
                case CNMAttrAndOr.options.AND: return a & b;
                case CNMAttrAndOr.options.OR: return a | b;
                default:
                    Debug.LogError("You should never see this error.");
                    return false;
            }
        }

        #endregion

        #region Initialization methods

        internal static void LoadModularTypes()
        {
            if (methodsLoaded)
                return;

            //modularTypes = AssemblyLoader.loadedTypes.FindAll(type => typeof(ModularCommNetVessel).IsAssignableFrom(type) && type != typeof(ModularCommNetVessel));
            //modularTypes = AssemblyLoader.loadedTypes.FindAll(type => type.IsSubclassOf(typeof(ModularCommNetVessel)));
            modularTypes = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                modularTypes.AddRange(assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(ModularCommNetVesselComponent))));
                // typeof(ModularCommNetVessel).IsAssignableFrom(type) && !type.IsAbstract).ToList());
            }

            foreach (Type type in modularTypes)
            {
                MethodInfo[] methodsInType = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach(MethodInfo method in methodsInType)
                {
                    if (method.DeclaringType != type)
                        continue;
                    methodTypes.Add(method, type);

                    if (Attribute.IsDefined(method, typeof(CNMAttrAndOr)))
                        andOrList.Add(method, (Attribute.GetCustomAttribute(method, typeof(CNMAttrAndOr)) as CNMAttrAndOr).andOr);
                    else
                        // Maybe not necessary
                        andOrList.Add(method, CNMAttrAndOr.options.AND);

                    if (!methodsSequence.ContainsKey(method.Name))
                        methodsSequence.Add(method.Name, new SequenceList<MethodInfo>());

                    if (Attribute.IsDefined(method, typeof(CNMAttrSequence)))
                    {
                        CNMAttrSequence attr = Attribute.GetCustomAttribute(method, typeof(CNMAttrSequence)) as CNMAttrSequence;
                        methodsSequence[method.Name].Add(attr.when, method);
                    }
                    else
                    {
                        // Maybe not necessary
                        if (andOrList.ContainsKey(method) && andOrList[method] == CNMAttrAndOr.options.OR)
                            methodsSequence[method.Name].Add(CNMAttrSequence.options.EARLY, method);
                        else
                            methodsSequence[method.Name].Add(CNMAttrSequence.options.LATE, method);
                    }
                }
            }
            methodsLoaded = true;
        }
        /// <summary>
        /// Instantiates the modular types.
        /// </summary>
        public void InstantiateModularTypes()
        {
            LoadModularTypes();
            modularRefs.Clear();
            this.ModularCommNetVessels = new List<ModularCommNetVesselComponent>();
            Sequence_Awake.Clear();
            Sequence_OnAwake.Clear();
            Sequence_OnStart.Clear();
            Sequence_OnDestroy.Clear();
            Sequence_OnGoOffRails.Clear();
            Sequence_OnGoOnRails.Clear();
            Sequence_OnLoad.Clear();
            Sequence_OnSave.Clear();
            Sequence_Update.Clear();
            Sequence_OnNetworkInitialized.Clear();
            Sequence_OnNetworkPreUpdate.Clear();
            Sequence_OnNetworkPostUpdate.Clear();
            Sequence_CalculatePlasmaMult.Clear();
            Sequence_UpdateComm.Clear();
            Sequence_CreateControlConnection.Clear();
            Sequence_GetBestTransmitter.Clear();
            Sequence_GetControlLevel.Clear();
            Sequence_OnMapFocusChange.Clear();
            Sequence_GetSignalStrengthModifier.Clear();

            Debug.Log("CNMAPI: ModularVessel: Instantiate " + modularTypes.Count);
            foreach (Type type in modularTypes)
            {
                if (type == typeof(ModularCommNetVesselComponent))
                {
                    Debug.Log("CommNetManager: Skipping type " + type.Name);
                    continue;
                }
                ModularCommNetVesselComponent modularCommNetVesselInstance = null;
                try
                {
                    /*if (type.GetConstructor(new Type[] { typeof(ModularCommNetVesselModule) }) != null)
                        modularCommNetVesselInstance = Activator.CreateInstance(type, new object[] { this }) as ModularCommNetVessel;
                    else
                    {
                        modularCommNetVesselInstance = Activator.CreateInstance(type) as ModularCommNetVessel;
                    }*/
                    //modularCommNetVesselInstance = Activator.CreateInstance(type, new object[] { this }) as ModularCommNetVessel;
                    modularCommNetVesselInstance = gameObject.AddComponent(type) as ModularCommNetVesselComponent;
                    modularCommNetVesselInstance.CommNetVessel = this;
                }
                catch (Exception ex)
                {
                    Debug.LogError("CommNetManager: Encountered an exception while calling the constructor for " + type.Name);
                    Debug.LogError(ex);
                }
                if (modularCommNetVesselInstance != null)
                {
                    ModularCommNetVessels.Add(modularCommNetVesselInstance);
                    modularRefs.Add(type, modularCommNetVesselInstance);
                    Debug.Log("CommNetManager: Activated an instance of type: " + type.Name);
                }
                else
                    Debug.LogWarning("CommNetManager: Failed to activate " + type.Name);
            }
            foreach (SequenceList<MethodInfo> methodList in methodsSequence.Values)
            {
                foreach (MethodInfo method in methodList.Early)
                {
                    if (!modularRefs.ContainsKey(methodTypes[method]))
                    {
                        Debug.LogWarning("CommNetManager: No instance of the CommNetwork type (" + methodTypes[method].DeclaringType.FullName.ToString() + ") was instantiated.");
                        continue;
                    }
                    ParseDelegates(method.Name, method, CNMAttrSequence.options.EARLY);
                }
                foreach (MethodInfo method in methodList.Late)
                {
                    if (!modularRefs.ContainsKey(methodTypes[method]))
                    {
                        Debug.LogWarning("CommNetManager: No instance of the CommNetwork type (" + methodTypes[method].DeclaringType.FullName.ToString() + ") was instantiated.");
                        continue;
                    }
                    ParseDelegates(method.Name, method, CNMAttrSequence.options.LATE);
                }
                foreach (MethodInfo method in methodList.Post)
                {
                    if (!modularRefs.ContainsKey(methodTypes[method]))
                    {
                        Debug.LogWarning("CommNetManager: No instance of the CommNetwork type (" + methodTypes[method].DeclaringType.FullName.ToString() + ") was instantiated.");
                        continue;
                    }
                    ParseDelegates(method.Name, method, CNMAttrSequence.options.POST);
                }
            }
        }
        private void ParseDelegates(string methodName, MethodInfo method, CNMAttrSequence.options sequence)
        {
            ModularCommNetVesselComponent instance = modularRefs[methodTypes[method]];

            if (andOrList.ContainsKey(method))
                Debug.LogFormat("CommNetManager: Parsing {0} from {1} as {2} with {3}.", methodName, instance.GetType().Name, sequence, andOrList[method]);
            else
                Debug.LogFormat("CommNetManager: Parsing {0} from {1} as {2}.", methodName, instance.GetType().Name, sequence);

            try
            {
                switch (methodName)
                {
                    case "Awake":
                        Sequence_Awake.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "OnAwake":
                        Sequence_OnAwake.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "OnStart":
                        Sequence_OnStart.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "OnDestroy":
                        Sequence_OnDestroy.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "OnGoOffRails":
                        Sequence_OnGoOffRails.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "OnGoOnRails":
                        Sequence_OnGoOnRails.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "OnLoad":
                        Sequence_OnLoad.Add(sequence, Delegate.CreateDelegate(typeof(Action<ConfigNode>), instance, method) as Action<ConfigNode>, instance);
                        break;
                    case "OnSave":
                        Sequence_OnSave.Add(sequence, Delegate.CreateDelegate(typeof(Action<ConfigNode>), instance, method) as Action<ConfigNode>, instance);
                        break;
                    case "Update":
                        Sequence_Update.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action, instance);
                        break;
                    case "OnNetworkInitialized":
                        Sequence_OnNetworkInitialized.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "OnNetworkPreUpdate":
                        Sequence_OnNetworkPreUpdate.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "OnNetworkPostUpdate":
                        Sequence_OnNetworkPostUpdate.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action);
                        break;
                    case "CalculatePlasmaMult":
                        Sequence_CalculatePlasmaMult.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action, instance);
                        break;
                    case "UpdateComm":
                        Sequence_UpdateComm.Add(sequence, Delegate.CreateDelegate(typeof(Action), instance, method) as Action, instance);
                        break;
                    case "CreateControlConnection":
                        Sequence_CreateControlConnection.Add(sequence, Delegate.CreateDelegate(typeof(Func<bool>), instance, method) as Func<bool>, new Pair<CNMAttrAndOr.options, ModularCommNetVesselComponent>(andOrList[method], instance));
                        break;
                    case "GetBestTransmitter":
                        Sequence_GetBestTransmitter.Add(sequence, Delegate.CreateDelegate(typeof(Func<IScienceDataTransmitter>), instance, method) as Func<IScienceDataTransmitter>, instance);
                        break;
                    case "GetControlLevel":
                        Sequence_GetControlLevel.Add(sequence, Delegate.CreateDelegate(typeof(Func<Vessel.ControlLevel>), instance, method) as Func<Vessel.ControlLevel>, instance);
                        break;
                    case "OnMapFocusChange":
                        Sequence_OnMapFocusChange.Add(sequence, Delegate.CreateDelegate(typeof(Action<MapObject>), instance, method) as Action<MapObject>);
                        break;
                    case "GetSignalStrengthModifier":
                        Sequence_GetSignalStrengthModifier.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, double>), instance, method) as Func<CommNode, double>, instance);
                        break;
                    default:
                        Debug.LogWarning("CommNetManager: The method passed (" + methodName + ") was not a standard CommNet method.");
                        return;
                }
                Debug.Log("CommNetManager: Successfully parsed " + methodName + " from type " + instance.GetType().Name);
            }
            catch (Exception ex)
            {
                Debug.LogError("CommNetManager: Encountered an error creating a delegate for " + methodName + " from type " + instance.GetType().Name);
                Debug.LogError(ex);
            }
        }

        private bool AmITheOne()
        {
            // Credit to Sarbian and/or Ialdabaoth for this method. #AllHailNyanCat
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            if (electionWinner != null)
                return currentAssembly == electionWinner;

            IEnumerable<AssemblyLoader.LoadedAssembly> eligible = from a in AssemblyLoader.loadedAssemblies
                                                                  let ass = a.assembly
                                                                  where ass.GetName().Name == currentAssembly.GetName().Name
                                                                  orderby ass.GetName().Version descending, a.path ascending
                                                                  select a;

            // Elect the newest loaded version of CNM to process all patch files.
            // If there is a newer version loaded then don't do anything
            // If there is a same version but earlier in the list, don't do anything either.
            if (eligible.First().assembly != currentAssembly)
            {
                //loaded = true;
                UnityEngine.Debug.Log("CNMAPI: version " + currentAssembly.GetName().Version + " at " + currentAssembly.Location +
                    " lost the election");
                DestroyImmediate(this);
                Destroy(this);
                return false;
            }
            string candidates = "";
            foreach (AssemblyLoader.LoadedAssembly a in eligible)
            {
                if (currentAssembly.Location != a.path)
                    candidates += "Version " + a.assembly.GetName().Version + " " + a.path + " " + "\n";
            }
            if (candidates.Length >= 0) // TODO: Not publish for zeroes.
            {
                UnityEngine.Debug.Log("CNMAPI: version " + currentAssembly.GetName().Version + " at " + currentAssembly.Location +
                    " won the election against\n" + candidates);
            }

            electionWinner = eligible.First().assembly;
            return true;
        }
        #endregion
    }
}
