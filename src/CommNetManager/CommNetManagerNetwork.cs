using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommNetManagerAPI;
using CommNet;
using UnityEngine;

namespace CommNetManager
{
    public class CommNetManagerNetwork : CommNetwork
    {
        public static CommNetwork Instance { get; protected set; } = null;
        private static bool methodsLoaded = false;

        private static Dictionary<MethodInfo, Type> methodTypes = new Dictionary<MethodInfo, Type>();
        private static Dictionary<string, PrePostList<MethodInfo>> methodsPrePost = new Dictionary<string, PrePostList<MethodInfo>>();
        
        private static Dictionary<MethodInfo, CNMAttrAndOr.options> andOrList = new Dictionary<MethodInfo, CNMAttrAndOr.options>();
        private Dictionary<Delegate, CNMAttrAndOr.options> invokesAndOr = new Dictionary<Delegate, CNMAttrAndOr.options>();
        private Dictionary<Type, CommNetwork> commNetworks = new Dictionary<Type, CommNetwork>();

        private class PrePostList<T>
        {
            public List<T> Pre;
            public List<T> Post;
            public PrePostList(List<T> Pre = null, List<T> Post = null)
            {
                if (Pre != null)
                    this.Pre = Pre;
                else
                    this.Pre = new List<T>();
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
                        case 0: return this.Pre;
                        case 1: return this.Post;
                        default:
                            Debug.LogError("CommNetManager: The provided int was out of range.");
                            return null;
                    }
                }
            }
            public List<T> this[CNMAttrPrePost.options PrePost]
            {
                get
                {
                    switch (PrePost)
                    {
                        case CNMAttrPrePost.options.PRE: return this.Pre;
                        case CNMAttrPrePost.options.POST: return this.Post;
                        default: return null;
                    }
                }
            }
            public void Add(CNMAttrPrePost.options PrePost, T obj)
            {
                switch (PrePost)
                {
                    case CNMAttrPrePost.options.PRE: this.Pre.Add(obj);
                        break;
                    case CNMAttrPrePost.options.POST: this.Post.Add(obj);
                        break;
                }
            }
            public void Clear()
            {
                this.Pre.Clear();
                this.Post.Clear();
            }
            public void Clear(CNMAttrPrePost.options PrePost)
            {
                switch (PrePost)
                {
                    case CNMAttrPrePost.options.PRE: this.Pre.Clear();
                        break;
                    case CNMAttrPrePost.options.POST: this.Post.Clear();
                        break;
                }
            }
        }

        public static List<Type> networkTypes { get; private set; }

        private PrePostList<Func<CommNode, CommNode, bool>> PrePost_SetNodeConnection = new PrePostList<Func<CommNode, CommNode, bool>>();
        private PrePostList<Func<CommNode, CommNode>> PrePost_Add_CommNode = new PrePostList<Func<CommNode, CommNode>>();
        private PrePostList<Func<Occluder, Occluder>> PrePost_Add_Occluder = new PrePostList<Func<Occluder, Occluder>>();
        private PrePostList<Func<CommNode, CommNode, double, CommLink>> PrePost_Connect = new PrePostList<Func<CommNode, CommNode, double, CommLink>>();
        private PrePostList<Action<CommNode, CommNode>> PrePost_CreateShortestPathTree = new PrePostList<Action<CommNode, CommNode>>();
        private PrePostList<Action<CommNode, CommNode, bool>> PrePost_Disconnect = new PrePostList<Action<CommNode, CommNode, bool>>();
        private PrePostList<Func<CommNode, CommPath, bool>> PrePost_FindClosestControlSource = new PrePostList<Func<CommNode, CommPath, bool>>();
        private PrePostList<Func<CommNode, CommPath, Func<CommNode, CommNode, bool>, CommNode>> PrePost_FindClosestWhere = new PrePostList<Func<CommNode, CommPath, Func<CommNode, CommNode, bool>, CommNode>>();
        private PrePostList<Func<CommNode, CommPath, bool>> PrePost_FindHome = new PrePostList<Func<CommNode, CommPath, bool>>();
        private PrePostList<Func<CommNode, CommPath, CommNode, bool>> PrePost_FindPath = new PrePostList<Func<CommNode, CommPath, CommNode, bool>>();
        private PrePostList<Action<List<Vector3>>> PrePost_GetLinkPoints = new PrePostList<Action<List<Vector3>>>();
        private PrePostList<Action> PrePost_PostUpdateNodes = new PrePostList<Action>();
        private PrePostList<Action> PrePost_PreUpdateNodes = new PrePostList<Action>();
        private PrePostList<Action> PrePost_Rebuild = new PrePostList<Action>();
        private PrePostList<Func<CommNode, bool>> PrePost_Remove_CommNode = new PrePostList<Func<CommNode, bool>>();
        private PrePostList<Func<Occluder, bool>> PrePost_Remove_Occluder = new PrePostList<Func<Occluder, bool>>();
        private PrePostList<Func<Vector3d, Occluder, Vector3d, Occluder, double, bool>> PrePost_TestOcclusion = new PrePostList<Func<Vector3d, Occluder, Vector3d, Occluder, double, bool>>();
        private PrePostList<Func<CommNode, CommNode, double, bool, bool, bool, bool>> PrePost_TryConnect = new PrePostList<Func<CommNode, CommNode, double, bool, bool, bool, bool>>();
        private PrePostList<Action> PrePost_UpdateNetwork = new PrePostList<Action>();
        private PrePostList<Action<CommNode, CommNode, CommLink, double, CommNode, CommNode>> PrePost_UpdateShortestPath = new PrePostList<Action<CommNode, CommNode, CommLink, double, CommNode, CommNode>>();
        private PrePostList<Func<CommNode, CommNode, CommLink, double, CommNode, Func<CommNode, CommNode, bool>, CommNode>> PrePost_UpdateShortestWhere = new PrePostList<Func<CommNode, CommNode, CommLink, double, CommNode, Func<CommNode, CommNode, bool>, CommNode>>();
        
        internal static void Initiate()
        {
            if (networkTypes == null)
                networkTypes = new List<Type>();
            else
                networkTypes.Clear();
            methodTypes.Clear();
            methodsPrePost.Clear();
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
                        andOrList.Add(method, (Attribute.GetCustomAttribute(method, typeof(CNMAttrAndOr)) as CNMAttrAndOr).op);
                    else
                        // Maybe not necessary
                        andOrList.Add(method, CNMAttrAndOr.options.AND);

                    if (!methodsPrePost.ContainsKey(method.Name))
                        methodsPrePost.Add(method.Name, new PrePostList<MethodInfo>());

                    if (Attribute.IsDefined(method, typeof(CNMAttrPrePost)))
                    {
                        CNMAttrPrePost attr = Attribute.GetCustomAttribute(method, typeof(CNMAttrPrePost)) as CNMAttrPrePost;
                        methodsPrePost[method.Name].Add(attr.op, method);
                    }
                    else
                        // Maybe not necessary
                        methodsPrePost[method.Name].Post.Add(method);
                }
            }

            methodsLoaded = true;
        }
        
        public CommNetManagerNetwork()
        {
            if (Instance != null)
                Debug.LogWarning("CommNetManager: CommNetManagerNetwork.Instance was not null.");
            Instance = this;

            commNetworks.Clear();
            PrePost_SetNodeConnection.Clear();
            PrePost_Add_CommNode.Clear();
            PrePost_Add_Occluder.Clear();
            PrePost_Connect.Clear();
            PrePost_CreateShortestPathTree.Clear();
            PrePost_Disconnect.Clear();
            PrePost_FindClosestControlSource.Clear();
            PrePost_FindClosestWhere.Clear();
            PrePost_FindHome.Clear();
            PrePost_FindPath.Clear();
            PrePost_GetLinkPoints.Clear();
            PrePost_PostUpdateNodes.Clear();
            PrePost_PreUpdateNodes.Clear();
            PrePost_Rebuild.Clear();
            PrePost_Remove_CommNode.Clear();
            PrePost_Remove_Occluder.Clear();
            PrePost_TestOcclusion.Clear();
            PrePost_TryConnect.Clear();
            PrePost_UpdateNetwork.Clear();
            PrePost_UpdateShortestPath.Clear();
            PrePost_UpdateShortestWhere.Clear();

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
            foreach (PrePostList<MethodInfo> methodList in methodsPrePost.Values)
            {
                foreach (MethodInfo method in methodList.Pre)
                {
                    if (!commNetworks.ContainsKey(methodTypes[method]))
                    {
                        Debug.LogWarning("CommNetManager: No instance of the CommNetwork type (" + methodTypes[method].DeclaringType.FullName.ToString()+") was instantiated.");
                        continue;
                    }
                    ParseDelegates(method.Name, method, CNMAttrPrePost.options.PRE);
                }
                foreach (MethodInfo method in methodList.Post)
                {
                    if (!commNetworks.ContainsKey(methodTypes[method]))
                    {
                        Debug.LogWarning("CommNetManager: No instance of the CommNetwork type (" + methodTypes[method].DeclaringType.FullName.ToString() + ") was instantiated.");
                        continue;
                    }
                    ParseDelegates(method.Name, method, CNMAttrPrePost.options.POST);
                }
            }
        }

        private void ParseDelegates(string methodName, MethodInfo method, CNMAttrPrePost.options PrePost)
        {
            CommNetwork networkInstance = commNetworks[methodTypes[method]];
            try
            {
                switch (methodName)
                {
                    case "SetNodeConnection":
                        PrePost_SetNodeConnection.Add(PrePost, Delegate.CreateDelegate(typeof(Func<CommNode, CommNode, bool>), networkInstance, method) as Func<CommNode, CommNode, bool>);
                        invokesAndOr.Add(PrePost_SetNodeConnection[PrePost].Last(), andOrList[method]);
                        break;
                    case "Add_CommNode":
                        PrePost_Add_CommNode.Add(PrePost, Delegate.CreateDelegate(typeof(Func<CommNode, CommNode>), networkInstance, method) as Func<CommNode, CommNode>);
                        break;
                    case "Add_Occluder":
                        PrePost_Add_Occluder.Add(PrePost, Delegate.CreateDelegate(typeof(Func<Occluder, Occluder>), networkInstance, method) as Func<Occluder, Occluder>);
                        break;
                    case "Connect":
                        PrePost_Connect.Add(PrePost, Delegate.CreateDelegate(typeof(Func<CommNode, CommNode, double, CommLink>), networkInstance, method) as Func<CommNode, CommNode, double, CommLink>);
                        break;
                    case "CreateShortestPathTree":
                        PrePost_CreateShortestPathTree.Add(PrePost, Delegate.CreateDelegate(typeof(Action<CommNode, CommNode>), networkInstance, method) as Action<CommNode, CommNode>);
                        break;
                    case "Disconnect":
                        PrePost_Disconnect.Add(PrePost, Delegate.CreateDelegate(typeof(Action<CommNode, CommNode, bool>), networkInstance, method) as Action<CommNode, CommNode, bool>);
                        invokesAndOr.Add(PrePost_SetNodeConnection[PrePost].Last(), andOrList[method]);
                        break;
                    case "FindClosestControlSource":
                        PrePost_FindClosestControlSource.Add(PrePost, Delegate.CreateDelegate(typeof(Func<CommNode, CommPath, bool>), networkInstance, method) as Func<CommNode, CommPath, bool>);
                        invokesAndOr.Add(PrePost_SetNodeConnection[PrePost].Last(), andOrList[method]);
                        break;
                    case "FindClosestWhere":
                        PrePost_FindClosestWhere.Add(PrePost, Delegate.CreateDelegate(typeof(Func<CommNode, CommPath, Func<CommNode, CommNode, bool>, CommNode>), networkInstance, method) as Func<CommNode, CommPath, Func<CommNode, CommNode, bool>, CommNode>);
                        break;
                    case "FindHome":
                        PrePost_FindHome.Add(PrePost, Delegate.CreateDelegate(typeof(Func<CommNode, CommPath, bool>), networkInstance, method) as Func<CommNode, CommPath, bool>);
                        invokesAndOr.Add(PrePost_SetNodeConnection[PrePost].Last(), andOrList[method]);
                        break;
                    case "FindPath":
                        PrePost_FindPath.Add(PrePost, Delegate.CreateDelegate(typeof(Func<CommNode, CommPath, CommNode, bool>), networkInstance, method) as Func<CommNode, CommPath, CommNode, bool>);
                        invokesAndOr.Add(PrePost_SetNodeConnection[PrePost].Last(), andOrList[method]);
                        break;
                    case "GetLinkPoints":
                        PrePost_GetLinkPoints.Add(PrePost, Delegate.CreateDelegate(typeof(Action<List<Vector3>>), networkInstance, method) as Action<List<Vector3>>);
                        break;
                    case "PostUpdateNodes":
                        PrePost_PostUpdateNodes.Add(PrePost, Delegate.CreateDelegate(typeof(Action), networkInstance, method) as Action);
                        break;
                    case "PreUpdateNodes":
                        PrePost_PreUpdateNodes.Add(PrePost, Delegate.CreateDelegate(typeof(Action), networkInstance, method) as Action);
                        break;
                    case "Rebuild":
                        PrePost_Rebuild.Add(PrePost, Delegate.CreateDelegate(typeof(Action), networkInstance, method) as Action);
                        break;
                    case "Remove_CommNode":
                        PrePost_Remove_CommNode.Add(PrePost, Delegate.CreateDelegate(typeof(Func<CommNode, bool>), networkInstance, method) as Func<CommNode, bool>);
                        invokesAndOr.Add(PrePost_SetNodeConnection[PrePost].Last(), andOrList[method]);
                        break;
                    case "Remove_Occluder":
                        PrePost_Remove_Occluder.Add(PrePost, Delegate.CreateDelegate(typeof(Func<Occluder, bool>), networkInstance, method) as Func<Occluder, bool>);
                        invokesAndOr.Add(PrePost_SetNodeConnection[PrePost].Last(), andOrList[method]);
                        break;
                    case "TestOcclusion":
                        PrePost_TestOcclusion.Add(PrePost, Delegate.CreateDelegate(typeof(Func<Vector3d, Occluder, Vector3d, Occluder, double, bool>), networkInstance, method) as Func<Vector3d, Occluder, Vector3d, Occluder, double, bool>);
                        invokesAndOr.Add(PrePost_SetNodeConnection[PrePost].Last(), andOrList[method]);
                        break;
                    case "TryConnect":
                        PrePost_TryConnect.Add(PrePost, Delegate.CreateDelegate(typeof(Func<CommNode, CommNode, double, bool, bool, bool, bool>), networkInstance, method) as Func<CommNode, CommNode, double, bool, bool, bool, bool>);
                        invokesAndOr.Add(PrePost_SetNodeConnection[PrePost].Last(), andOrList[method]);
                        break;
                    case "UpdateNetwork":
                        PrePost_UpdateNetwork.Add(PrePost, Delegate.CreateDelegate(typeof(Action), networkInstance, method) as Action);
                        break;
                    case "UpdateShortestPath":
                        PrePost_UpdateShortestPath.Add(PrePost, Delegate.CreateDelegate(typeof(Action<CommNode, CommNode, CommLink, double, CommNode, CommNode>), networkInstance, method) as Action<CommNode, CommNode, CommLink, double, CommNode, CommNode>);
                        break;
                    case "UpdateShortestWhere":
                        PrePost_UpdateShortestWhere.Add(PrePost, Delegate.CreateDelegate(typeof(Func<CommNode, CommNode, CommLink, double, CommNode, Func<CommNode, CommNode, bool>, CommNode>), networkInstance, method) as Func<CommNode, CommNode, CommLink, double, CommNode, Func<CommNode, CommNode, bool>, CommNode>);
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

        protected override bool SetNodeConnection(CommNode a, CommNode b)
        {
            bool value;
            if (PrePost_SetNodeConnection.Pre.Count > 0)
            {
                try { value = PrePost_SetNodeConnection.Pre[0].Invoke(a, b); }
                catch (Exception ex) { Debug.LogError(ex); value = true; }
                for (int i = 1; i < PrePost_SetNodeConnection.Pre.Count; i++)
                {
                    try
                    {
                        switch (invokesAndOr[PrePost_SetNodeConnection.Pre[i]])
                        {
                            case CNMAttrAndOr.options.AND:
                                value &= PrePost_SetNodeConnection.Pre[i].Invoke(a, b);
                                break;
                            case CNMAttrAndOr.options.OR:
                                value |= PrePost_SetNodeConnection.Pre[i].Invoke(a, b);
                                break;
                        }
                    }
                    catch (Exception ex) { Debug.LogError(ex); }
                }
                value |= base.SetNodeConnection(a, b);
            }
            else
            {
                value = base.SetNodeConnection(a, b);
            }

            for (int i = 1; i < PrePost_SetNodeConnection.Post.Count; i++)
            {
                try
                {
                    switch (invokesAndOr[PrePost_SetNodeConnection.Post[i]])
                    {
                        case CNMAttrAndOr.options.AND:
                            value &= PrePost_SetNodeConnection.Post[i].Invoke(a, b);
                            break;
                        case CNMAttrAndOr.options.OR:
                            value |= PrePost_SetNodeConnection.Post[i].Invoke(a, b);
                            break;
                    }
                }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override CommNode Add(CommNode conn)
        {
            CommNode value;

            for (int i = 0; i < PrePost_Add_CommNode.Pre.Count; i++)
            {
                try { PrePost_Add_CommNode.Pre[i].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            value = base.Add(conn);

            for (int i = 0; i < PrePost_Add_CommNode.Post.Count; i++)
            {
                try { PrePost_Add_CommNode.Post[i].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override Occluder Add(Occluder conn)
        {
            Occluder value;

            for (int i = 0; i < PrePost_Add_Occluder.Pre.Count; i++)
            {
                try { PrePost_Add_Occluder.Pre[i].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            value = base.Add(conn);

            for (int i = 0; i < PrePost_Add_Occluder.Post.Count; i++)
            {
                try { PrePost_Add_Occluder.Post[i].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        protected override CommLink Connect(CommNode a, CommNode b, double distance)
        {
            CommLink value;

            for (int i = 0; i < PrePost_Connect.Pre.Count; i++)
            {
                try { PrePost_Connect.Pre[i].Invoke(a, b, distance); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            value = base.Connect(a, b, distance);

            for (int i = 0; i < PrePost_Connect.Post.Count; i++)
            {
                try { PrePost_Connect.Post[i].Invoke(a, b, distance); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        protected override void CreateShortestPathTree(CommNode start, CommNode end)
        {
            for (int i = 0; i < PrePost_CreateShortestPathTree.Pre.Count; i++)
            {
                try { PrePost_CreateShortestPathTree.Pre[i].Invoke(start, end); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.CreateShortestPathTree(start, end);

            for (int i = 0; i < PrePost_CreateShortestPathTree.Post.Count; i++)
            {
                try { PrePost_CreateShortestPathTree.Post[i].Invoke(start, end); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        protected override void Disconnect(CommNode a, CommNode b, bool removeFromA = true)
        {
            for (int i = 0; i < PrePost_Disconnect.Pre.Count; i++)
            {
                try { PrePost_Disconnect.Pre[i].Invoke(a, b, removeFromA); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.Disconnect(a, b, removeFromA);

            for (int i = 0; i < PrePost_Disconnect.Post.Count; i++)
            {
                try { PrePost_Disconnect.Post[i].Invoke(a, b, removeFromA); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        public override bool FindClosestControlSource(CommNode from, CommPath path = null)
        {
            bool value;

            if (PrePost_FindClosestControlSource.Pre.Count > 0)
            {
                try { value = PrePost_FindClosestControlSource.Pre[0].Invoke(from, path); }
                catch (Exception ex) { Debug.LogError(ex); value = true; }
                for (int i = 1; i < PrePost_FindClosestControlSource.Pre.Count; i++)
                {
                    try
                    {
                        switch (invokesAndOr[PrePost_FindClosestControlSource.Pre[i]])
                        {
                            case CNMAttrAndOr.options.AND:
                                value &= PrePost_FindClosestControlSource.Pre[i].Invoke(from, path);
                                break;
                            case CNMAttrAndOr.options.OR:
                                value |= PrePost_FindClosestControlSource.Pre[i].Invoke(from, path);
                                break;
                        }
                    }
                    catch (Exception ex) { Debug.LogError(ex); }
                }
                value |= base.FindClosestControlSource(from, path);
            }
            else
            {
                value = base.FindClosestControlSource(from, path);
            }

            for (int i = 1; i < PrePost_FindClosestControlSource.Post.Count; i++)
            {
                try
                {
                    switch (invokesAndOr[PrePost_FindClosestControlSource.Post[i]])
                    {
                        case CNMAttrAndOr.options.AND:
                            value &= PrePost_FindClosestControlSource.Post[i].Invoke(from, path);
                            break;
                        case CNMAttrAndOr.options.OR:
                            value |= PrePost_FindClosestControlSource.Post[i].Invoke(from, path);
                            break;
                    }
                }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override CommNode FindClosestWhere(CommNode start, CommPath path, Func<CommNode, CommNode, bool> where)
        {
            CommNode value;

            for (int i = 0; i < PrePost_FindClosestWhere.Pre.Count; i++)
            {
                try { PrePost_FindClosestWhere.Pre[i].Invoke(start, path, where); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            value = base.FindClosestWhere(start, path, where);

            for (int i = 0; i < PrePost_FindClosestWhere.Post.Count; i++)
            {
                try { PrePost_FindClosestWhere.Post[i].Invoke(start, path, where); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override bool FindHome(CommNode from, CommPath path = null)
        {
            bool value;

            if (PrePost_FindHome.Pre.Count > 0)
            {
                try { value = PrePost_FindHome.Pre[0].Invoke(from, path); }
                catch (Exception ex) { Debug.LogError(ex); value = true; }
                for (int i = 1; i < PrePost_FindHome.Pre.Count; i++)
                {
                    try
                    {
                        switch (invokesAndOr[PrePost_FindHome.Pre[i]])
                        {
                            case CNMAttrAndOr.options.AND:
                                value &= PrePost_FindHome.Pre[i].Invoke(from, path);
                                break;
                            case CNMAttrAndOr.options.OR:
                                value |= PrePost_FindHome.Pre[i].Invoke(from, path);
                                break;
                        }
                    }
                    catch (Exception ex) { Debug.LogError(ex); }
                }
                value |= base.FindHome(from, path);
            }
            else
            {
                value = base.FindHome(from, path);
            }

            for (int i = 1; i < PrePost_FindHome.Post.Count; i++)
            {
                try
                {
                    switch (invokesAndOr[PrePost_FindHome.Post[i]])
                    {
                        case CNMAttrAndOr.options.AND:
                            value &= PrePost_FindHome.Post[i].Invoke(from, path);
                            break;
                        case CNMAttrAndOr.options.OR:
                            value |= PrePost_FindHome.Post[i].Invoke(from, path);
                            break;
                    }
                }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override bool FindPath(CommNode start, CommPath path, CommNode end)
        {
            bool value;

            if (PrePost_FindPath.Pre.Count > 0)
            {
                try { value = PrePost_FindPath.Pre[0].Invoke(start, path, end); }
                catch (Exception ex) { Debug.LogError(ex); value = true; }
                for (int i = 1; i < PrePost_FindPath.Pre.Count; i++)
                {
                    try
                    {
                        switch (invokesAndOr[PrePost_FindPath.Pre[i]])
                        {
                            case CNMAttrAndOr.options.AND:
                                value &= PrePost_FindPath.Pre[i].Invoke(start, path, end);
                                break;
                            case CNMAttrAndOr.options.OR:
                                value |= PrePost_FindPath.Pre[i].Invoke(start, path, end);
                                break;
                        }
                    }
                    catch (Exception ex) { Debug.LogError(ex); }
                }
                value |= base.FindPath(start, path, end);
            }
            else
            {
                value = base.FindPath(start, path, end);
            }

            for (int i = 1; i < PrePost_FindPath.Post.Count; i++)
            {
                try
                {
                    switch (invokesAndOr[PrePost_FindPath.Post[i]])
                    {
                        case CNMAttrAndOr.options.AND:
                            value &= PrePost_FindPath.Post[i].Invoke(start, path, end);
                            break;
                        case CNMAttrAndOr.options.OR:
                            value |= PrePost_FindPath.Post[i].Invoke(start, path, end);
                            break;
                    }
                }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override void GetLinkPoints(List<Vector3> discreteLines)
        {
            for (int i = 0; i < PrePost_GetLinkPoints.Pre.Count; i++)
            {
                try { PrePost_GetLinkPoints.Pre[i].Invoke(discreteLines); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.GetLinkPoints(discreteLines);

            for (int i = 0; i < PrePost_GetLinkPoints.Post.Count; i++)
            {
                try { PrePost_GetLinkPoints.Post[i].Invoke(discreteLines); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        protected override void PostUpdateNodes()
        {
            for (int i = 0; i < PrePost_PostUpdateNodes.Pre.Count; i++)
            {
                try { PrePost_PostUpdateNodes.Pre[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.PostUpdateNodes();

            for (int i = 0; i < PrePost_PostUpdateNodes.Post.Count; i++)
            {
                try { PrePost_PostUpdateNodes.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        protected override void PreUpdateNodes()
        {
            for (int i = 0; i < PrePost_PreUpdateNodes.Pre.Count; i++)
            {
                try { PrePost_PreUpdateNodes.Pre[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.PreUpdateNodes();

            for (int i = 0; i < PrePost_PreUpdateNodes.Post.Count; i++)
            {
                try { PrePost_PreUpdateNodes.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        public override void Rebuild()
        {
            for (int i = 0; i < PrePost_Rebuild.Pre.Count; i++)
            {
                try { PrePost_Rebuild.Pre[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.Rebuild();

            for (int i = 0; i < PrePost_Rebuild.Post.Count; i++)
            {
                try { PrePost_Rebuild.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        public override bool Remove(CommNode conn)
        {
            bool value;

            if (PrePost_Remove_CommNode.Pre.Count > 0)
            {
                try { value = PrePost_Remove_CommNode.Pre[0].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); value = true; }
                for (int i = 1; i < PrePost_Remove_CommNode.Pre.Count; i++)
                {
                    try
                    {
                        switch (invokesAndOr[PrePost_Remove_CommNode.Pre[i]])
                        {
                            case CNMAttrAndOr.options.AND:
                                value &= PrePost_Remove_CommNode.Pre[i].Invoke(conn);
                                break;
                            case CNMAttrAndOr.options.OR:
                                value |= PrePost_Remove_CommNode.Pre[i].Invoke(conn);
                                break;
                        }
                    }
                    catch (Exception ex) { Debug.LogError(ex); }
                }
                value |= base.Remove(conn);
            }
            else
            {
                value = base.Remove(conn);
            }

            for (int i = 1; i < PrePost_Remove_CommNode.Post.Count; i++)
            {
                try
                {
                    switch (invokesAndOr[PrePost_Remove_CommNode.Post[i]])
                    {
                        case CNMAttrAndOr.options.AND:
                            value &= PrePost_Remove_CommNode.Post[i].Invoke(conn);
                            break;
                        case CNMAttrAndOr.options.OR:
                            value |= PrePost_Remove_CommNode.Post[i].Invoke(conn);
                            break;
                    }
                }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        public override bool Remove(Occluder conn)
        {
            bool value;

            if (PrePost_Remove_Occluder.Pre.Count > 0)
            {
                try { value = PrePost_Remove_Occluder.Pre[0].Invoke(conn); }
                catch (Exception ex) { Debug.LogError(ex); value = true; }
                for (int i = 1; i < PrePost_Remove_Occluder.Pre.Count; i++)
                {
                    try
                    {
                        switch (invokesAndOr[PrePost_Remove_Occluder.Pre[i]])
                        {
                            case CNMAttrAndOr.options.AND:
                                value &= PrePost_Remove_Occluder.Pre[i].Invoke(conn);
                                break;
                            case CNMAttrAndOr.options.OR:
                                value |= PrePost_Remove_Occluder.Pre[i].Invoke(conn);
                                break;
                        }
                    }
                    catch (Exception ex) { Debug.LogError(ex); }
                }
                value |= base.Remove(conn);
            }
            else
            {
                value = base.Remove(conn);
            }

            for (int i = 1; i < PrePost_Remove_Occluder.Post.Count; i++)
            {
                try
                {
                    switch (invokesAndOr[PrePost_Remove_Occluder.Post[i]])
                    {
                        case CNMAttrAndOr.options.AND:
                            value &= PrePost_Remove_Occluder.Post[i].Invoke(conn);
                            break;
                        case CNMAttrAndOr.options.OR:
                            value |= PrePost_Remove_Occluder.Post[i].Invoke(conn);
                            break;
                    }
                }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        protected override bool TestOcclusion(Vector3d aPos, Occluder a, Vector3d bPos, Occluder b, double distance)
        {
            bool value;

            if (PrePost_TestOcclusion.Pre.Count > 0)
            {
                try { value = PrePost_TestOcclusion.Pre[0].Invoke(aPos, a, bPos, b, distance); }
                catch (Exception ex) { Debug.LogError(ex); value = true; }
                for (int i = 1; i < PrePost_TestOcclusion.Pre.Count; i++)
                {
                    try
                    {
                        switch (invokesAndOr[PrePost_TestOcclusion.Pre[i]])
                        {
                            case CNMAttrAndOr.options.AND:
                                value &= PrePost_TestOcclusion.Pre[i].Invoke(aPos, a, bPos, b, distance);
                                break;
                            case CNMAttrAndOr.options.OR:
                                value |= PrePost_TestOcclusion.Pre[i].Invoke(aPos, a, bPos, b, distance);
                                break;
                        }
                    }
                    catch (Exception ex) { Debug.LogError(ex); }
                }
                value |= base.TestOcclusion(aPos, a, bPos, b, distance);
            }
            else
            {
                value = base.TestOcclusion(aPos, a, bPos, b, distance);
            }

            for (int i = 1; i < PrePost_TestOcclusion.Post.Count; i++)
            {
                try
                {
                    switch (invokesAndOr[PrePost_TestOcclusion.Post[i]])
                    {
                        case CNMAttrAndOr.options.AND:
                            value &= PrePost_TestOcclusion.Post[i].Invoke(aPos, a, bPos, b, distance);
                            break;
                        case CNMAttrAndOr.options.OR:
                            value |= PrePost_TestOcclusion.Post[i].Invoke(aPos, a, bPos, b, distance);
                            break;
                    }
                }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        protected override bool TryConnect(CommNode a, CommNode b, double distance, bool aCanRelay, bool bCanRelay, bool bothRelay)
        {
            bool value;

            if (PrePost_TryConnect.Pre.Count > 0)
            {
                try { value = PrePost_TryConnect.Pre[0].Invoke(a, b, distance, aCanRelay, bCanRelay, bothRelay); }
                catch (Exception ex) { Debug.LogError(ex); value = true; }
                for (int i = 1; i < PrePost_TryConnect.Pre.Count; i++)
                {
                    try
                    {
                        switch (invokesAndOr[PrePost_TryConnect.Pre[i]])
                        {
                            case CNMAttrAndOr.options.AND:
                                value &= PrePost_TryConnect.Pre[i].Invoke(a, b, distance, aCanRelay, bCanRelay, bothRelay);
                                break;
                            case CNMAttrAndOr.options.OR:
                                value |= PrePost_TryConnect.Pre[i].Invoke(a, b, distance, aCanRelay, bCanRelay, bothRelay);
                                break;
                        }
                    }
                    catch (Exception ex) { Debug.LogError(ex); }
                }
                value |= base.TryConnect(a, b, distance, aCanRelay, bCanRelay, bothRelay);
            }
            else
            {
                value = base.TryConnect(a, b, distance, aCanRelay, bCanRelay, bothRelay);
            }

            for (int i = 1; i < PrePost_TryConnect.Post.Count; i++)
            {
                try
                {
                    switch (invokesAndOr[PrePost_TryConnect.Post[i]])
                    {
                        case CNMAttrAndOr.options.AND:
                            value &= PrePost_TryConnect.Post[i].Invoke(a, b, distance, aCanRelay, bCanRelay, bothRelay);
                            break;
                        case CNMAttrAndOr.options.OR:
                            value |= PrePost_TryConnect.Post[i].Invoke(a, b, distance, aCanRelay, bCanRelay, bothRelay);
                            break;
                    }
                }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        protected override void UpdateNetwork()
        {
            CommNetManagerEvents.onCommNetPreUpdate.Fire(CommNetManager.Instance, this);

            for (int i = 0; i < PrePost_UpdateNetwork.Pre.Count; i++)
            {
                try { PrePost_UpdateNetwork.Pre[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.UpdateNetwork();

            for (int i = 0; i < PrePost_UpdateNetwork.Post.Count; i++)
            {
                try { PrePost_UpdateNetwork.Post[i].Invoke(); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            CommNetManagerEvents.onCommNetPostUpdate.Fire(CommNetManager.Instance, this);
        }

        protected override void UpdateShortestPath(CommNode a, CommNode b, CommLink link, double bestCost, CommNode startNode, CommNode endNode)
        {
            for (int i = 0; i < PrePost_UpdateShortestPath.Pre.Count; i++)
            {
                try { PrePost_UpdateShortestPath.Pre[i].Invoke(a, b, link, bestCost, startNode, endNode); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            base.UpdateShortestPath(a, b, link, bestCost, startNode, endNode);

            for (int i = 0; i < PrePost_UpdateShortestPath.Post.Count; i++)
            {
                try { PrePost_UpdateShortestPath.Post[i].Invoke(a, b, link, bestCost, startNode, endNode); }
                catch (Exception ex) { Debug.LogError(ex); }
            }
        }

        protected override CommNode UpdateShortestWhere(CommNode a, CommNode b, CommLink link, double bestCost, CommNode startNode, Func<CommNode, CommNode, bool> whereClause)
        {
            CommNode value;

            for (int i = 0; i < PrePost_UpdateShortestWhere.Pre.Count; i++)
            {
                try { PrePost_UpdateShortestWhere.Pre[i].Invoke(a, b, link, bestCost, startNode, whereClause); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            value = base.UpdateShortestWhere(a, b, link, bestCost, startNode, whereClause);

            for (int i = 0; i < PrePost_UpdateShortestWhere.Post.Count; i++)
            {
                try { PrePost_UpdateShortestWhere.Post[i].Invoke(a, b, link, bestCost, startNode, whereClause); }
                catch (Exception ex) { Debug.LogError(ex); }
            }

            return value;
        }

        // Things that should be built into the language...
        public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 T1, T2 T2, T3 T3, T4 T4, T5 T5);
        public delegate TResult Func<T1, T2, T3, T4, T5, T6, TResult>(T1 T1, T2 T2, T3 T3, T4 T4, T5 T5, T6 T6);
        public delegate void Action<T1, T2, T3, T4, T5>(T1 T1, T2 T2, T3 T3, T4 T4, T5 T5);
        public delegate void Action<T1, T2, T3, T4, T5, T6>(T1 T1, T2 T2, T3 T3, T4 T4, T5 T5, T6 T6);
    }
}
