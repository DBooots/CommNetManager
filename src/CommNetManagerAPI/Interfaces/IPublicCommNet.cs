using CommNet;
using System;
using System.Collections.Generic;

namespace CommNetManagerAPI
{
    /// <summary>
    /// Public implementation of many protected methods. Cast a CommNetManager instance to this interface to call these methods.
    /// <para />USE WITH CAUTION.
    /// </summary>
    public interface IPublicCommNet
    {
        /// <summary>
        /// Gets the instance of CommNetManagerNetwork.
        /// </summary>
        /// <returns></returns>
        CommNetwork GetInstance();
        /// <summary>
        /// Sets the node connection.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        /// <remarks>Calls <see cref="TryConnect(CommNode, CommNode, double, bool, bool, bool)"/>.</remarks> 
        bool SetNodeConnection(CommNode a, CommNode b);
        /// <summary>
        /// Adds the specified connection.
        /// </summary>
        /// <param name="conn">The connection.</param>
        /// <returns></returns>
        CommNode Add(CommNode conn);
        /// <summary>
        /// Adds the specified connection.
        /// </summary>
        /// <param name="conn">The connection.</param>
        /// <returns></returns>
        Occluder Add(Occluder conn);
        /// <summary>
        /// Connects two nodes.
        /// <para />Note: This does not set many variables associated with a <see cref="CommLink"/> . Generally, <see cref="TryConnect(CommNode, CommNode, double, bool, bool, bool)"/> would do much of that.
        /// </summary>
        /// <param name="a">Node A</param>
        /// <param name="b">Node B</param>
        /// <param name="distance">The distance between nodes.</param>
        /// <returns></returns>
        CommLink Connect(CommNode a, CommNode b, double distance);
        /// <summary>
        /// Creates the shortest path tree.
        /// </summary>
        /// <param name="start">The starting node.</param>
        /// <param name="end">The ending node.</param>
        void CreateShortestPathTree(CommNode start, CommNode end);
        /// <summary>
        /// Disconnects two nodes.
        /// </summary>
        /// <param name="a">Node A</param>
        /// <param name="b">Node B</param>
        /// <param name="removeFromA">Remove B from A as well.</param>
        void Disconnect(CommNode a, CommNode b, bool removeFromA = true);
        /// <summary>
        /// Finds the closest control source.
        /// </summary>
        /// <param name="from">The CommNode from which to find a control source.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        bool FindClosestControlSource(CommNode from, CommPath path = null);
        /// <summary>
        /// Finds the closest according to some clause.
        /// </summary>
        /// <param name="start">The starting node.</param>
        /// <param name="path">The path.</param>
        /// <param name="where">The clause.</param>
        /// <returns></returns>
        CommNode FindClosestWhere(CommNode start, CommPath path, Func<CommNode, CommNode, bool> where);
        /// <summary>
        /// Finds home from a CommNode.
        /// </summary>
        /// <param name="from">The CommNode from which to find home.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        bool FindHome(CommNode from, CommPath path = null);
        /// <summary>
        /// Finds a path between two CommNodes.
        /// </summary>
        /// <param name="start">The starting node.</param>
        /// <param name="path">The path.</param>
        /// <param name="end">The ending node.</param>
        /// <returns></returns>
        bool FindPath(CommNode start, CommPath path, CommNode end);
        /// <summary>
        /// Gets the link points.
        /// </summary>
        /// <param name="discreteLines">The discrete lines.</param>
        void GetLinkPoints(List<UnityEngine.Vector3> discreteLines);
        /// <summary>
        /// Called after updating the CommNodes.
        /// </summary>
        void PostUpdateNodes();
        /// <summary>
        /// Called before updating the CommNodes.
        /// </summary>
        void PreUpdateNodes();
        /// <summary>
        /// Rebuilds the network.
        /// </summary>
        void Rebuild();
        /// <summary>
        /// Removes the specified connection.
        /// </summary>
        /// <param name="conn">The connection.</param>
        /// <returns></returns>
        bool Remove(CommNode conn);
        /// <summary>
        /// Removes the specified connection.
        /// </summary>
        /// <param name="conn">The connection.</param>
        /// <returns></returns>
        bool Remove(Occluder conn);
        /// <summary>
        /// Tests the occlusion.
        /// </summary>
        /// <param name="aPos">a position.</param>
        /// <param name="a">a.</param>
        /// <param name="bPos">The b position.</param>
        /// <param name="b">The b.</param>
        /// <param name="distance">The distance.</param>
        /// <returns></returns>
        bool TestOcclusion(Vector3d aPos, Occluder a, Vector3d bPos, Occluder b, double distance);
        /// <summary>
        /// Tries to connect two nodes.
        /// </summary>
        /// <param name="a">Node A</param>
        /// <param name="b">Node B</param>
        /// <param name="distance">The distance between nodes</param>
        /// <param name="aCanRelay">Can node A relay?</param>
        /// <param name="bCanRelay">Can node B relay?</param>
        /// <param name="bothRelay">Can both nodes relay?</param>
        /// <returns></returns>
        /// <remarks>Calls <see cref="Connect(CommNode, CommNode, double)"/>.</remarks> 
        bool TryConnect(CommNode a, CommNode b, double distance, bool aCanRelay, bool bCanRelay, bool bothRelay);
        /// <summary>
        /// Updates the network.
        /// </summary>
        /// <remarks>Calls <see cref="Rebuild"/> and <see cref="SetNodeConnection(CommNode, CommNode)"/>.</remarks> 
        void UpdateNetwork();
        /// <summary>
        /// Updates the shortest path between two nodes.
        /// </summary>
        /// <param name="a">Node A</param>
        /// <param name="b">Node B</param>
        /// <param name="link">The CommLink</param>
        /// <param name="bestCost">The best cost.</param>
        /// <param name="startNode">The start node.</param>
        /// <param name="endNode">The end node.</param>
        void UpdateShortestPath(CommNode a, CommNode b, CommLink link, double bestCost, CommNode startNode, CommNode endNode);
        /// <summary>
        /// Updates the shortest path with some constraint.
        /// </summary>
        /// <param name="a">Node A</param>
        /// <param name="b">Node B</param>
        /// <param name="link">The CommLink</param>
        /// <param name="bestCost">The best cost.</param>
        /// <param name="startNode">The start node.</param>
        /// <param name="whereClause">The constraint clause.</param>
        /// <returns></returns>
        CommNode UpdateShortestWhere(CommNode a, CommNode b, CommLink link, double bestCost, CommNode startNode, Func<CommNode, CommNode, bool> whereClause);
    }
}
