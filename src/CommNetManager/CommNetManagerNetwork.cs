using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommNetManagerAPI;
using CommNet;
using UnityEngine;

namespace CommNetManager
{
    public class CommNetManagerNetwork : CommNetwork, PublicCommNet
    {
        public static CommNetwork Instance { get; protected set; } = null;
        internal protected static Dictionary<CommNode, Vessel> commNodesVessels = new Dictionary<CommNode, Vessel>();

        private static bool methodsLoaded = false;

        private static Dictionary<MethodInfo, Type> methodTypes = new Dictionary<MethodInfo, Type>();
        private static Dictionary<string, SequenceList<MethodInfo>> methodsSequence = new Dictionary<string, SequenceList<MethodInfo>>();
        
        private static Dictionary<MethodInfo, CNMAttrAndOr.options> andOrList = new Dictionary<MethodInfo, CNMAttrAndOr.options>();
        private Dictionary<Delegate, CNMAttrAndOr.options> invokesAndOr = new Dictionary<Delegate, CNMAttrAndOr.options>();
        private Dictionary<Type, CommNetwork> commNetworks = new Dictionary<Type, CommNetwork>();

        private class SequenceList<T>
        {
            public List<T> Early;
            public List<T> Late;
            public List<T> Post;
            public SequenceList(List<T> Early = null, List<T> Late = null, List<T> Post = null)
            {
                if (Early != null)
                    this.Early = Early;
                else
                    this.Early = new List<T>();
                if (Late != null)
                    this.Late = Late;
                else
                    this.Late = new List<T>();
                if (Post != null)
                    this.Post = Post;
                else
                    this.Post = new List<T>();
            }
            public List<T> this[int i]
            {
                get
                {
                    switch (i)
                    {
                        case 0: return this.Early;
                        case 1: return this.Late;
                        case 2: return this.Post;
                        default:
                            Debug.LogError("CommNetManager: The provided int was out of range.");
                            return null;
                    }
                }
            }
            public List<T> this[CNMAttrSequence.options sequence]
            {
                get
                {
                    switch (sequence)
                    {
                        case CNMAttrSequence.options.EARLY: return this.Early;
                        case CNMAttrSequence.options.LATE: return this.Late;
                        case CNMAttrSequence.options.POST: return this.Post;
                        default: return null;
                    }
                }
            }
            public void Add(CNMAttrSequence.options sequence, T obj)
            {
                switch (sequence)
                {
                    case CNMAttrSequence.options.EARLY: this.Early.Add(obj);
                        break;
                    case CNMAttrSequence.options.LATE: this.Late.Add(obj);
                        break;
                    case CNMAttrSequence.options.POST: this.Post.Add(obj);
                        break;
                }
            }
            public void Clear()
            {
                this.Early.Clear();
                this.Late.Clear();
                this.Post.Clear();
            }
            public void Clear(CNMAttrSequence.options sequence)
            {
                switch (sequence)
                {
                    case CNMAttrSequence.options.EARLY: this.Early.Clear();
                        break;
                    case CNMAttrSequence.options.LATE: this.Late.Clear();
                        break;
                    case CNMAttrSequence.options.POST: this.Post.Clear();
                        break;
                }
            }
        }

        public static List<Type> networkTypes { get; private set; }

        #region SequenceList<Delegate> for each inherited method

        private SequenceList<Func<CommNode, CommNode, bool>> Sequence_SetNodeConnection = new SequenceList<Func<CommNode, CommNode, bool>>();
        private SequenceList<Func<CommNode, CommNode>> Sequence_Add_CommNode = new SequenceList<Func<CommNode, CommNode>>();
        private SequenceList<Func<Occluder, Occluder>> Sequence_Add_Occluder = new SequenceList<Func<Occluder, Occluder>>();
        private SequenceList<Func<CommNode, CommNode, double, CommLink>> Sequence_Connect = new SequenceList<Func<CommNode, CommNode, double, CommLink>>();
        private SequenceList<Action<CommNode, CommNode>> Sequence_CreateShortestPathTree = new SequenceList<Action<CommNode, CommNode>>();
        private SequenceList<Action<CommNode, CommNode, bool>> Sequence_Disconnect = new SequenceList<Action<CommNode, CommNode, bool>>();
        private SequenceList<Func<CommNode, CommPath, bool>> Sequence_FindClosestControlSource = new SequenceList<Func<CommNode, CommPath, bool>>();
        private SequenceList<Func<CommNode, CommPath, Func<CommNode, CommNode, bool>, CommNode>> Sequence_FindClosestWhere = new SequenceList<Func<CommNode, CommPath, Func<CommNode, CommNode, bool>, CommNode>>();
        private SequenceList<Func<CommNode, CommPath, bool>> Sequence_FindHome = new SequenceList<Func<CommNode, CommPath, bool>>();
        private SequenceList<Func<CommNode, CommPath, CommNode, bool>> Sequence_FindPath = new SequenceList<Func<CommNode, CommPath, CommNode, bool>>();
        private SequenceList<Action<List<Vector3>>> Sequence_GetLinkPoints = new SequenceList<Action<List<Vector3>>>();
        private SequenceList<Action> Sequence_PostUpdateNodes = new SequenceList<Action>();
        private SequenceList<Action> Sequence_PreUpdateNodes = new SequenceList<Action>();
        private SequenceList<Action> Sequence_Rebuild = new SequenceList<Action>();
        private SequenceList<Func<CommNode, bool>> Sequence_Remove_CommNode = new SequenceList<Func<CommNode, bool>>();
        private SequenceList<Func<Occluder, bool>> Sequence_Remove_Occluder = new SequenceList<Func<Occluder, bool>>();
        private SequenceList<Func<Vector3d, Occluder, Vector3d, Occluder, double, bool>> Sequence_TestOcclusion = new SequenceList<Func<Vector3d, Occluder, Vector3d, Occluder, double, bool>>();
        private SequenceList<Func<CommNode, CommNode, double, bool, bool, bool, bool>> Sequence_TryConnect = new SequenceList<Func<CommNode, CommNode, double, bool, bool, bool, bool>>();
        private SequenceList<Action> Sequence_UpdateNetwork = new SequenceList<Action>();
        private SequenceList<Action<CommNode, CommNode, CommLink, double, CommNode, CommNode>> Sequence_UpdateShortestPath = new SequenceList<Action<CommNode, CommNode, CommLink, double, CommNode, CommNode>>();
        private SequenceList<Func<CommNode, CommNode, CommLink, double, CommNode, Func<CommNode, CommNode, bool>, CommNode>> Sequence_UpdateShortestWhere = new SequenceList<Func<CommNode, CommNode, CommLink, double, CommNode, Func<CommNode, CommNode, bool>, CommNode>>();

        #endregion

        internal static void Initiate()
        {
            if (networkTypes == null)
                networkTypes = new List<Type>();
            else
                networkTypes.Clear();
            methodTypes.Clear();
            methodsSequence.Clear();
            andOrList.Clear();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                networkTypes.AddRange(assembly.GetTypes().Where(type => typeof(CommNetwork).IsAssignableFrom(type) && !type.IsAbstract).ToList());
            }

            foreach (Type type in networkTypes)
            {
                Debug.Log("CommNetManager: Found a CommNetwork type: " + type.Name);
                if (type == typeof(CommNetwork) || type == typeof(CommNetManagerNetwork))
                {
                    Debug.Log("CommNetManager: Skipping type " + type.Name);
                    continue;
                }

                MethodInfo[] methodsInType = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (MethodInfo method in methodsInType)
                {
                    // If it's not declared in that type, we don't care.
                    // i.e. If it comes from a base class, it's already taken care of.

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
                            methodsSequence[method.Name].Early.Add(method);
                        else
                            methodsSequence[method.Name].Late.Add(method);
                    }
                }
            }

            // Sorting methods:
            // TODO:
            
            methodsLoaded = true;
        }
        
        public CommNetManagerNetwork()
        {
            if (Instance != null)
                Debug.LogWarning("CommNetManager: CommNetManagerNetwork.Instance was not null.");
            Instance = this;

            commNetworks.Clear();
            Sequence_SetNodeConnection.Clear();
            Sequence_Add_CommNode.Clear();
            Sequence_Add_Occluder.Clear();
            Sequence_Connect.Clear();
            Sequence_CreateShortestPathTree.Clear();
            Sequence_Disconnect.Clear();
            Sequence_FindClosestControlSource.Clear();
            Sequence_FindClosestWhere.Clear();
            Sequence_FindHome.Clear();
            Sequence_FindPath.Clear();
            Sequence_GetLinkPoints.Clear();
            Sequence_PostUpdateNodes.Clear();
            Sequence_PreUpdateNodes.Clear();
            Sequence_Rebuild.Clear();
            Sequence_Remove_CommNode.Clear();
            Sequence_Remove_Occluder.Clear();
            Sequence_TestOcclusion.Clear();
            Sequence_TryConnect.Clear();
            Sequence_UpdateNetwork.Clear();
            Sequence_UpdateShortestPath.Clear();
            Sequence_UpdateShortestWhere.Clear();

            if (!methodsLoaded)
            {
                CommNetManagerNetwork.Initiate();
            }
            Debug.Log("CommNetManager: " + networkTypes.Count);
            foreach (Type type in networkTypes)
            {
                if (type == typeof(CommNetwork) || type == typeof(CommNetManagerNetwork))
                {
                    Debug.Log("CommNetManager: Skipping type " + type.Name);
                    continue;
                }
                CommNetwork typeNetworkInstance = null;
                try
                {
                    typeNetworkInstance = Activator.CreateInstance(type) as CommNetwork;
                }
                catch(Exception ex)
                {
                    Debug.LogError("CommNetManager: Encountered an exception while calling the constructor for " + type.Name);
                    Debug.LogError(ex);
                }
                if (typeNetworkInstance != null)
                {
                    commNetworks.Add(type, typeNetworkInstance);
                    Debug.Log("CommNetManager: Activated an instance of type: " + type.Name);
                }
                else
                    Debug.LogWarning("CommNetManager: Failed to activate " + type.Name);
            }
            foreach (SequenceList<MethodInfo> methodList in methodsSequence.Values)
            {
                foreach (MethodInfo method in methodList.Early)
                {
                    if (!commNetworks.ContainsKey(methodTypes[method]))
                    {
                        Debug.LogWarning("CommNetManager: No instance of the CommNetwork type (" + methodTypes[method].DeclaringType.FullName.ToString()+") was instantiated.");
                        continue;
                    }
                    ParseDelegates(method.Name, method, CNMAttrSequence.options.EARLY);
                }
                foreach (MethodInfo method in methodList.Late)
                {
                    if (!commNetworks.ContainsKey(methodTypes[method]))
                    {
                        Debug.LogWarning("CommNetManager: No instance of the CommNetwork type (" + methodTypes[method].DeclaringType.FullName.ToString() + ") was instantiated.");
                        continue;
                    }
                    ParseDelegates(method.Name, method, CNMAttrSequence.options.LATE);
                }
                foreach (MethodInfo method in methodList.Post)
                {
                    if (!commNetworks.ContainsKey(methodTypes[method]))
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
            CommNetwork networkInstance = commNetworks[methodTypes[method]];

            if (andOrList.ContainsKey(method))
                Debug.LogFormat("CommNetManager: Parsing {0} from {1} as {2} with {3}.", methodName, networkInstance.GetType().Name, sequence, andOrList[method]);
            else
                Debug.LogFormat("CommNetManager: Parsing {0} from {1} as {2}.", methodName, networkInstance.GetType().Name, sequence);

            try
            {
                switch (methodName)
                {
                    case "SetNodeConnection":
                        Sequence_SetNodeConnection.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, CommNode, bool>), networkInstance, method) as Func<CommNode, CommNode, bool>);
                        invokesAndOr.Add(Sequence_SetNodeConnection[sequence].Last(), andOrList[method]);
                        break;
                    case "Add_CommNode":
                        Sequence_Add_CommNode.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, CommNode>), networkInstance, method) as Func<CommNode, CommNode>);
                        break;
                    case "Add_Occluder":
                        Sequence_Add_Occluder.Add(sequence, Delegate.CreateDelegate(typeof(Func<Occluder, Occluder>), networkInstance, method) as Func<Occluder, Occluder>);
                        break;
                    case "Connect":
                        Sequence_Connect.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, CommNode, double, CommLink>), networkInstance, method) as Func<CommNode, CommNode, double, CommLink>);
                        break;
                    case "CreateShortestPathTree":
                        Sequence_CreateShortestPathTree.Add(sequence, Delegate.CreateDelegate(typeof(Action<CommNode, CommNode>), networkInstance, method) as Action<CommNode, CommNode>);
                        break;
                    case "Disconnect":
                        Sequence_Disconnect.Add(sequence, Delegate.CreateDelegate(typeof(Action<CommNode, CommNode, bool>), networkInstance, method) as Action<CommNode, CommNode, bool>);
                        invokesAndOr.Add(Sequence_Disconnect[sequence].Last(), andOrList[method]);
                        break;
                    case "FindClosestControlSource":
                        Sequence_FindClosestControlSource.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, CommPath, bool>), networkInstance, method) as Func<CommNode, CommPath, bool>);
                        invokesAndOr.Add(Sequence_FindClosestControlSource[sequence].Last(), andOrList[method]);
                        break;
                    case "FindClosestWhere":
                        Sequence_FindClosestWhere.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, CommPath, Func<CommNode, CommNode, bool>, CommNode>), networkInstance, method) as Func<CommNode, CommPath, Func<CommNode, CommNode, bool>, CommNode>);
                        break;
                    case "FindHome":
                        Sequence_FindHome.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, CommPath, bool>), networkInstance, method) as Func<CommNode, CommPath, bool>);
                        invokesAndOr.Add(Sequence_FindHome[sequence].Last(), andOrList[method]);
                        break;
                    case "FindPath":
                        Sequence_FindPath.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, CommPath, CommNode, bool>), networkInstance, method) as Func<CommNode, CommPath, CommNode, bool>);
                        invokesAndOr.Add(Sequence_FindPath[sequence].Last(), andOrList[method]);
                        break;
                    case "GetLinkPoints":
                        Sequence_GetLinkPoints.Add(sequence, Delegate.CreateDelegate(typeof(Action<List<Vector3>>), networkInstance, method) as Action<List<Vector3>>);
                        break;
                    case "PostUpdateNodes":
                        Sequence_PostUpdateNodes.Add(sequence, Delegate.CreateDelegate(typeof(Action), networkInstance, method) as Action);
                        break;
                    case "PreUpdateNodes":
                        Sequence_PreUpdateNodes.Add(sequence, Delegate.CreateDelegate(typeof(Action), networkInstance, method) as Action);
                        break;
                    case "Rebuild":
                        Sequence_Rebuild.Add(sequence, Delegate.CreateDelegate(typeof(Action), networkInstance, method) as Action);
                        break;
                    case "Remove_CommNode":
                        Sequence_Remove_CommNode.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, bool>), networkInstance, method) as Func<CommNode, bool>);
                        invokesAndOr.Add(Sequence_Remove_CommNode[sequence].Last(), andOrList[method]);
                        break;
                    case "Remove_Occluder":
                        Sequence_Remove_Occluder.Add(sequence, Delegate.CreateDelegate(typeof(Func<Occluder, bool>), networkInstance, method) as Func<Occluder, bool>);
                        invokesAndOr.Add(Sequence_Remove_Occluder[sequence].Last(), andOrList[method]);
                        break;
                    case "TestOcclusion":
                        Sequence_TestOcclusion.Add(sequence, Delegate.CreateDelegate(typeof(Func<Vector3d, Occluder, Vector3d, Occluder, double, bool>), networkInstance, method) as Func<Vector3d, Occluder, Vector3d, Occluder, double, bool>);
                        invokesAndOr.Add(Sequence_TestOcclusion[sequence].Last(), andOrList[method]);
                        break;
                    case "TryConnect":
                        Sequence_TryConnect.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, CommNode, double, bool, bool, bool, bool>), networkInstance, method) as Func<CommNode, CommNode, double, bool, bool, bool, bool>);
                        invokesAndOr.Add(Sequence_TryConnect[sequence].Last(), andOrList[method]);
                        break;
                    case "UpdateNetwork":
                        Sequence_UpdateNetwork.Add(sequence, Delegate.CreateDelegate(typeof(Action), networkInstance, method) as Action);
                        break;
                    case "UpdateShortestPath":
                        Sequence_UpdateShortestPath.Add(sequence, Delegate.CreateDelegate(typeof(Action<CommNode, CommNode, CommLink, double, CommNode, CommNode>), networkInstance, method) as Action<CommNode, CommNode, CommLink, double, CommNode, CommNode>);
                        break;
                    case "UpdateShortestWhere":
                        Sequence_UpdateShortestWhere.Add(sequence, Delegate.CreateDelegate(typeof(Func<CommNode, CommNode, CommLink, double, CommNode, Func<CommNode, CommNode, bool>, CommNode>), networkInstance, method) as Func<CommNode, CommNode, CommLink, double, CommNode, Func<CommNode, CommNode, bool>, CommNode>);
                        break;
                    default:
                        Debug.LogWarning("CommNetManager: The method passed (" + methodName + ") was not a standard CommNet method.");
                        return;
                }
                Debug.Log("CommNetManager: Successfully parsed " + methodName + " from type " + networkInstance.GetType().Name);
            }
            catch(Exception ex)
            {
                Debug.LogError("CommNetManager: Encountered an error creating a delegate for " + methodName + " from type " + networkInstance.GetType().Name);
                Debug.LogError(ex);
            }
        }

        public bool BindNetwork(CommNetwork net)
        {
            // Use reflection to force the network to use this network's associated Lists
            try
            {
                Type netType = net.GetType();
                netType.GetField("candidates", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(net, this.candidates);
                netType.GetField("links", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(net, this.links);
                netType.GetField("nodeEnum", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(net, this.nodeEnum);
                netType.GetField("nodeLink", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(net, this.nodeLink);
                netType.GetField("nodeLinkEnum", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(net, this.nodeLinkEnum);
                netType.GetField("nodes", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(net, this.nodes);
                netType.GetField("occluders", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(net, this.occluders);
                return true;
            }
            catch
            {
                return false;
            }
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

        protected override bool SetNodeConnection(CommNode a, CommNode b)
        {
            bool value = true;
            for (int i = 0; i < Sequence_SetNodeConnection.Early.Count; i++)
            {
                try { value = AndOr(value, Sequence_SetNodeConnection.Early[i].Invoke(a, b), invokesAndOr[Sequence_SetNodeConnection.Early[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_SetNodeConnection.Late.Count; i++)
            {
                try { value = AndOr(value, Sequence_SetNodeConnection.Late[i].Invoke(a, b), invokesAndOr[Sequence_SetNodeConnection.Late[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            if (value == true)
                value &= base.SetNodeConnection(a, b);
            else
            {
                Disconnect(a, b, true);
            }
            
            for (int i = 0; i < Sequence_SetNodeConnection.Post.Count; i++)
            {
                try { value = AndOr(value, Sequence_SetNodeConnection.Post[i].Invoke(a, b), invokesAndOr[Sequence_SetNodeConnection.Post[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override CommNode Add(CommNode conn)
        {
            CommNode value;

            for (int i = 0; i < Sequence_Add_CommNode.Early.Count; i++)
            {
                try { Sequence_Add_CommNode.Early[i].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_Add_CommNode.Late.Count; i++)
            {
                try { Sequence_Add_CommNode.Late[i].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            value = base.Add(conn);
            if (value != null && !commNodesVessels.ContainsKey(conn))
                commNodesVessels.Add(conn, FlightGlobals.Vessels.Find(vessel => vessel != null && vessel.connection != null && vessel.connection.Comm == conn));

            for (int i = 0; i < Sequence_Add_CommNode.Post.Count; i++)
            {
                try { Sequence_Add_CommNode.Post[i].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override Occluder Add(Occluder conn)
        {
            Occluder value;

            for (int i = 0; i < Sequence_Add_Occluder.Early.Count; i++)
            {
                try { Sequence_Add_Occluder.Early[i].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_Add_Occluder.Late.Count; i++)
            {
                try { Sequence_Add_Occluder.Late[i].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            value = base.Add(conn);

            for (int i = 0; i < Sequence_Add_Occluder.Post.Count; i++)
            {
                try { Sequence_Add_Occluder.Post[i].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        protected override CommLink Connect(CommNode a, CommNode b, double distance)
        {
            CommLink value;

            for (int i = 0; i < Sequence_Connect.Early.Count; i++)
            {
                try { Sequence_Connect.Early[i].Invoke(a, b, distance); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_Connect.Late.Count; i++)
            {
                try { Sequence_Connect.Late[i].Invoke(a, b, distance); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            value = base.Connect(a, b, distance);

            for (int i = 0; i < Sequence_Connect.Post.Count; i++)
            {
                try { Sequence_Connect.Post[i].Invoke(a, b, distance); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        protected override void CreateShortestPathTree(CommNode start, CommNode end)
        {
            for (int i = 0; i < Sequence_CreateShortestPathTree.Early.Count; i++)
            {
                try { Sequence_CreateShortestPathTree.Early[i].Invoke(start, end); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_CreateShortestPathTree.Late.Count; i++)
            {
                try { Sequence_CreateShortestPathTree.Late[i].Invoke(start, end); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.CreateShortestPathTree(start, end);

            for (int i = 0; i < Sequence_CreateShortestPathTree.Post.Count; i++)
            {
                try { Sequence_CreateShortestPathTree.Post[i].Invoke(start, end); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        protected override void Disconnect(CommNode a, CommNode b, bool removeFromA = true)
        {
            for (int i = 0; i < Sequence_Disconnect.Early.Count; i++)
            {
                try { Sequence_Disconnect.Early[i].Invoke(a, b, removeFromA); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_Disconnect.Late.Count; i++)
            {
                try { Sequence_Disconnect.Late[i].Invoke(a, b, removeFromA); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.Disconnect(a, b, removeFromA);

            for (int i = 0; i < Sequence_Disconnect.Post.Count; i++)
            {
                try { Sequence_Disconnect.Post[i].Invoke(a, b, removeFromA); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        public override bool FindClosestControlSource(CommNode from, CommPath path = null)
        {
            bool value = true;
            for (int i = 0; i < Sequence_FindClosestControlSource.Early.Count; i++)
            {
                try { value = AndOr(value, Sequence_FindClosestControlSource.Early[i].Invoke(from, path), invokesAndOr[Sequence_FindClosestControlSource.Early[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_FindClosestControlSource.Late.Count; i++)
            {
                try { value = AndOr(value, Sequence_FindClosestControlSource.Late[i].Invoke(from, path), invokesAndOr[Sequence_FindClosestControlSource.Late[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            //if (value == true)
            value &= base.FindClosestControlSource(from, path);

            for (int i = 0; i < Sequence_FindClosestControlSource.Post.Count; i++)
            {
                try { value = AndOr(value, Sequence_FindClosestControlSource.Post[i].Invoke(from, path), invokesAndOr[Sequence_FindClosestControlSource.Post[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override CommNode FindClosestWhere(CommNode start, CommPath path, Func<CommNode, CommNode, bool> where)
        {
            CommNode value;

            for (int i = 0; i < Sequence_FindClosestWhere.Early.Count; i++)
            {
                try { Sequence_FindClosestWhere.Early[i].Invoke(start, path, where); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_FindClosestWhere.Late.Count; i++)
            {
                try { Sequence_FindClosestWhere.Late[i].Invoke(start, path, where); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            value = base.FindClosestWhere(start, path, where);

            for (int i = 0; i < Sequence_FindClosestWhere.Post.Count; i++)
            {
                try { Sequence_FindClosestWhere.Post[i].Invoke(start, path, where); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override bool FindHome(CommNode from, CommPath path = null)
        {
            bool value = true;
            for (int i = 0; i < Sequence_FindHome.Early.Count; i++)
            {
                try { value = AndOr(value, Sequence_FindHome.Early[i].Invoke(from, path), invokesAndOr[Sequence_FindHome.Early[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_FindHome.Late.Count; i++)
            {
                try { value = AndOr(value, Sequence_FindHome.Late[i].Invoke(from, path), invokesAndOr[Sequence_FindHome.Late[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            //if (value == true)
            value &= base.FindHome(from, path);

            for (int i = 0; i < Sequence_FindHome.Post.Count; i++)
            {
                try { value = AndOr(value, Sequence_FindHome.Post[i].Invoke(from, path), invokesAndOr[Sequence_FindHome.Post[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override bool FindPath(CommNode start, CommPath path, CommNode end)
        {
            bool value = true;
            for (int i = 0; i < Sequence_FindPath.Early.Count; i++)
            {
                try { value = AndOr(value, Sequence_FindPath.Early[i].Invoke(start, path, end), invokesAndOr[Sequence_FindPath.Early[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_FindPath.Late.Count; i++)
            {
                try { value = AndOr(value, Sequence_FindPath.Late[i].Invoke(start, path, end), invokesAndOr[Sequence_FindPath.Late[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            //if (value == true)
            value &= base.FindPath(start, path, end);

            for (int i = 0; i < Sequence_FindPath.Post.Count; i++)
            {
                try { value = AndOr(value, Sequence_FindPath.Post[i].Invoke(start, path, end), invokesAndOr[Sequence_FindPath.Post[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override void GetLinkPoints(List<Vector3> discreteLines)
        {
            for (int i = 0; i < Sequence_GetLinkPoints.Early.Count; i++)
            {
                try { Sequence_GetLinkPoints.Early[i].Invoke(discreteLines); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_GetLinkPoints.Late.Count; i++)
            {
                try { Sequence_GetLinkPoints.Late[i].Invoke(discreteLines); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.GetLinkPoints(discreteLines);

            for (int i = 0; i < Sequence_GetLinkPoints.Post.Count; i++)
            {
                try { Sequence_GetLinkPoints.Post[i].Invoke(discreteLines); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        protected override void PostUpdateNodes()
        {
            for (int i = 0; i < Sequence_PostUpdateNodes.Early.Count; i++)
            {
                try { Sequence_PostUpdateNodes.Early[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_PostUpdateNodes.Late.Count; i++)
            {
                try { Sequence_PostUpdateNodes.Late[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.PostUpdateNodes();

            for (int i = 0; i < Sequence_PostUpdateNodes.Post.Count; i++)
            {
                try { Sequence_PostUpdateNodes.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        protected override void PreUpdateNodes()
        {
            for (int i = 0; i < Sequence_PreUpdateNodes.Early.Count; i++)
            {
                try { Sequence_PreUpdateNodes.Early[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_PreUpdateNodes.Late.Count; i++)
            {
                try { Sequence_PreUpdateNodes.Late[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.PreUpdateNodes();

            for (int i = 0; i < Sequence_PreUpdateNodes.Post.Count; i++)
            {
                try { Sequence_PreUpdateNodes.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        public override void Rebuild()
        {
            for (int i = 0; i < Sequence_Rebuild.Early.Count; i++)
            {
                try { Sequence_Rebuild.Early[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_Rebuild.Late.Count; i++)
            {
                try { Sequence_Rebuild.Late[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.Rebuild();

            for (int i = 0; i < Sequence_Rebuild.Post.Count; i++)
            {
                try { Sequence_Rebuild.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        public override bool Remove(CommNode conn)
        {
            bool value = true;
            for (int i = 0; i < Sequence_Remove_CommNode.Early.Count; i++)
            {
                try { value = AndOr(value, Sequence_Remove_CommNode.Early[i].Invoke(conn), invokesAndOr[Sequence_Remove_CommNode.Early[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_Remove_CommNode.Late.Count; i++)
            {
                try { value = AndOr(value, Sequence_Remove_CommNode.Late[i].Invoke(conn), invokesAndOr[Sequence_Remove_CommNode.Late[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            //if (value == true)

            bool baseValue = base.Remove(conn);
            if (baseValue && commNodesVessels.ContainsKey(conn))
                commNodesVessels.Remove(conn);

            value &= baseValue;

            for (int i = 0; i < Sequence_Remove_CommNode.Post.Count; i++)
            {
                try { value = AndOr(value, Sequence_Remove_CommNode.Post[i].Invoke(conn), invokesAndOr[Sequence_Remove_CommNode.Post[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override bool Remove(Occluder conn)
        {
            bool value = true;
            for (int i = 0; i < Sequence_Remove_Occluder.Early.Count; i++)
            {
                try { value = AndOr(value, Sequence_Remove_Occluder.Early[i].Invoke(conn), invokesAndOr[Sequence_Remove_Occluder.Early[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_Remove_Occluder.Late.Count; i++)
            {
                try { value = AndOr(value, Sequence_Remove_Occluder.Late[i].Invoke(conn), invokesAndOr[Sequence_Remove_Occluder.Late[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            //if (value == true)
            value &= base.Remove(conn);

            for (int i = 0; i < Sequence_Remove_Occluder.Post.Count; i++)
            {
                try { value = AndOr(value, Sequence_Remove_Occluder.Post[i].Invoke(conn), invokesAndOr[Sequence_Remove_Occluder.Post[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        protected override bool TestOcclusion(Vector3d aPos, Occluder a, Vector3d bPos, Occluder b, double distance)
        {
            bool value = true;
            for (int i = 0; i < Sequence_TestOcclusion.Early.Count; i++)
            {
                try { value = AndOr(value, Sequence_TestOcclusion.Early[i].Invoke(aPos, a, bPos, b, distance), invokesAndOr[Sequence_TestOcclusion.Early[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_TestOcclusion.Late.Count; i++)
            {
                try { value = AndOr(value, Sequence_TestOcclusion.Late[i].Invoke(aPos, a, bPos, b, distance), invokesAndOr[Sequence_TestOcclusion.Late[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            //if (value == true)
            value &= base.TestOcclusion(aPos, a, bPos, b, distance);

            for (int i = 0; i < Sequence_TestOcclusion.Post.Count; i++)
            {
                try { value = AndOr(value, Sequence_TestOcclusion.Post[i].Invoke(aPos, a, bPos, b, distance), invokesAndOr[Sequence_TestOcclusion.Post[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        protected override bool TryConnect(CommNode a, CommNode b, double distance, bool aCanRelay, bool bCanRelay, bool bothRelay)
        {
            bool value = true;
            for (int i = 0; i < Sequence_TryConnect.Early.Count; i++)
            {
                try { value = AndOr(value, Sequence_TryConnect.Early[i].Invoke(a, b, distance, aCanRelay, bCanRelay, bothRelay), invokesAndOr[Sequence_TryConnect.Early[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_TryConnect.Late.Count; i++)
            {
                try { value = AndOr(value, Sequence_TryConnect.Late[i].Invoke(a, b, distance, aCanRelay, bCanRelay, bothRelay), invokesAndOr[Sequence_TryConnect.Late[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            //if (value == true)
            value &= base.TryConnect(a, b, distance, aCanRelay, bCanRelay, bothRelay);

            for (int i = 0; i < Sequence_TryConnect.Post.Count; i++)
            {
                try { value = AndOr(value, Sequence_TryConnect.Post[i].Invoke(a, b, distance, aCanRelay, bCanRelay, bothRelay), invokesAndOr[Sequence_TryConnect.Post[i]]); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        protected override void UpdateNetwork()
        {
            CommNetManagerEvents.onCommNetPreUpdate.Fire(CommNetManager.Instance, this);

            for (int i = 0; i < Sequence_UpdateNetwork.Early.Count; i++)
            {
                try { Sequence_UpdateNetwork.Early[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_UpdateNetwork.Late.Count; i++)
            {
                try { Sequence_UpdateNetwork.Late[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.UpdateNetwork();

            for (int i = 0; i < Sequence_UpdateNetwork.Post.Count; i++)
            {
                try { Sequence_UpdateNetwork.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            CommNetManagerEvents.onCommNetPostUpdate.Fire(CommNetManager.Instance, this);
        }

        protected override void UpdateShortestPath(CommNode a, CommNode b, CommLink link, double bestCost, CommNode startNode, CommNode endNode)
        {
            for (int i = 0; i < Sequence_UpdateShortestPath.Early.Count; i++)
            {
                try { Sequence_UpdateShortestPath.Early[i].Invoke(a, b, link, bestCost, startNode, endNode); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_UpdateShortestPath.Late.Count; i++)
            {
                try { Sequence_UpdateShortestPath.Late[i].Invoke(a, b, link, bestCost, startNode, endNode); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.UpdateShortestPath(a, b, link, bestCost, startNode, endNode);

            for (int i = 0; i < Sequence_UpdateShortestPath.Post.Count; i++)
            {
                try { Sequence_UpdateShortestPath.Post[i].Invoke(a, b, link, bestCost, startNode, endNode); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        protected override CommNode UpdateShortestWhere(CommNode a, CommNode b, CommLink link, double bestCost, CommNode startNode, Func<CommNode, CommNode, bool> whereClause)
        {
            CommNode value;

            for (int i = 0; i < Sequence_UpdateShortestWhere.Early.Count; i++)
            {
                try { Sequence_UpdateShortestWhere.Early[i].Invoke(a, b, link, bestCost, startNode, whereClause); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            for (int i = 0; i < Sequence_UpdateShortestWhere.Late.Count; i++)
            {
                try { Sequence_UpdateShortestWhere.Late[i].Invoke(a, b, link, bestCost, startNode, whereClause); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            value = base.UpdateShortestWhere(a, b, link, bestCost, startNode, whereClause);

            for (int i = 0; i < Sequence_UpdateShortestWhere.Post.Count; i++)
            {
                try { Sequence_UpdateShortestWhere.Post[i].Invoke(a, b, link, bestCost, startNode, whereClause); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        #endregion

        #region PublicCommNet

        bool PublicCommNet.SetNodeConnection(CommNode a, CommNode b) { return this.SetNodeConnection(a, b); }

        CommNode PublicCommNet.Add(CommNode conn) { return this.Add(conn); }

        Occluder PublicCommNet.Add(Occluder conn) { return this.Add(conn); }

        CommLink PublicCommNet.Connect(CommNode a, CommNode b, double distance) { return this.Connect(a, b, distance); }

        void PublicCommNet.CreateShortestPathTree(CommNode start, CommNode end) { this.CreateShortestPathTree(start, end); }

        void PublicCommNet.Disconnect(CommNode a, CommNode b, bool removeFromA) { this.Disconnect(a, b, removeFromA); }

        bool PublicCommNet.FindClosestControlSource(CommNode from, CommPath path) { return this.FindClosestControlSource(from, path); }

        CommNode PublicCommNet.FindClosestWhere(CommNode start, CommPath path, Func<CommNode, CommNode, bool> where)
            { return this.FindClosestWhere(start, path, where); }

        bool PublicCommNet.FindHome(CommNode from, CommPath path) { return this.FindHome(from, path); }

        bool PublicCommNet.FindPath(CommNode start, CommPath path, CommNode end) { return this.FindPath(start, path, end); }

        void PublicCommNet.GetLinkPoints(List<Vector3> discreteLines) { this.GetLinkPoints(discreteLines); }

        void PublicCommNet.PostUpdateNodes() { this.PostUpdateNodes(); }

        void PublicCommNet.PreUpdateNodes() { this.PreUpdateNodes(); }

        void PublicCommNet.Rebuild() { this.Rebuild(); }

        bool PublicCommNet.Remove(CommNode conn) { return this.Remove(conn); }

        bool PublicCommNet.Remove(Occluder conn) { return this.Remove(conn); }

        bool PublicCommNet.TestOcclusion(Vector3d aPos, Occluder a, Vector3d bPos, Occluder b, double distance)
            { return this.TestOcclusion(aPos, a, bPos, b, distance); }

        bool PublicCommNet.TryConnect(CommNode a, CommNode b, double distance, bool aCanRelay, bool bCanRelay, bool bothRelay)
            { return this.TryConnect(a, b, distance, aCanRelay, bCanRelay, bothRelay); }

        void PublicCommNet.UpdateNetwork() { this.UpdateNetwork(); }

        void PublicCommNet.UpdateShortestPath(CommNode a, CommNode b, CommLink link, double bestCost, CommNode startNode, CommNode endNode)
            { this.UpdateShortestPath(a, b, link, bestCost, startNode, endNode); }

        CommNode PublicCommNet.UpdateShortestWhere(CommNode a, CommNode b, CommLink link, double bestCost, CommNode startNode, Func<CommNode, CommNode, bool> whereClause)
            { return this.UpdateShortestWhere(a, b, link, bestCost, startNode, whereClause); }

        CommNetwork PublicCommNet.GetInstance() { return Instance; }

        #endregion

        #region Func<> and Action<> extension
        // Things that should be built into the language...
        public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 T1, T2 T2, T3 T3, T4 T4, T5 T5);
        public delegate TResult Func<T1, T2, T3, T4, T5, T6, TResult>(T1 T1, T2 T2, T3 T3, T4 T4, T5 T5, T6 T6);
        public delegate void Action<T1, T2, T3, T4, T5>(T1 T1, T2 T2, T3 T3, T4 T4, T5 T5);
        public delegate void Action<T1, T2, T3, T4, T5, T6>(T1 T1, T2 T2, T3 T3, T4 T4, T5 T5, T6 T6);
        #endregion
    }
}
