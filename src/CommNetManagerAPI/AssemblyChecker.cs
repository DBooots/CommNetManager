using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommNet;

namespace CommNetManagerAPI
{
    /// <summary>
    /// CommNetManager's class to ensure that only the most recent version of the API implements its <see cref="CommNetBody"/> and <see cref="CommNetHome"/> classes.
    /// </summary>
    public static class AssemblyChecker
    {
        private static bool assyChecked = false;
        private static bool _isElected = false;
        private static Assembly electionWinner;
        private static bool typesSet = false;
        /// <summary>
        /// The <see cref="CNMBody"/> instances actually used in game.
        /// </summary>
        public static List<CNMBody> CNMBodies { get; private set; }
        /// <summary>
        /// The <see cref="CNMHome"/> instances actually used in game.
        /// </summary>
        public static List<CNMHome> CNMHomes { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this instance is elected.
        /// </summary>
        public static bool IsElected
        {
            get
            {
                if (!assyChecked)
                    _isElected = AmITheOne();
                return _isElected;
            }
        }

        /// <summary>
        /// Sets the comm net types.
        /// </summary>
        /// <returns></returns>
        public static bool SetCommNetTypes()
        {
            if (IsElected)
            {
                //Replace the CommNet ground stations
                CommNetHome[] homes = UnityEngine.GameObject.FindObjectsOfType<CommNetHome>();
                for (int i = 0; i < homes.Length; i++)
                {
                    if (homes[i] is CNMHome)
                        continue;
                    CNMHome customHome = homes[i].gameObject.AddComponent<CNMHome>();
                    customHome.Initialize(homes[i]);
                    UnityEngine.Object.Destroy(homes[i]);
                }

                //Replace the CommNet celestial bodies
                CommNetBody[] bodies = UnityEngine.GameObject.FindObjectsOfType<CommNetBody>();
                for (int i = 0; i < bodies.Length; i++)
                {
                    if (bodies[i] is CNMBody)
                        continue;
                    CNMBody customBody = bodies[i].gameObject.AddComponent<CNMBody>();
                    customBody.Initialize(bodies[i]);
                    UnityEngine.Object.Destroy(bodies[i]);
                }

                return typesSet = true;
            }
            else
            {
                Type winnerType = electionWinner.GetType("AssemblyChecker", true);
                typesSet = (bool)winnerType.GetMethod("SetCommNetTypes").Invoke(null, null);
                CNMBodies = (List<CNMBody>)winnerType.GetProperty("CNMBodies").GetValue(null, null);
                CNMHomes = (List<CNMHome>)winnerType.GetProperty("CNMHomes").GetValue(null, null);
                return typesSet;
            }
        }

        private static bool AmITheOne()
        {
            // Credit to Sarbian and/or Ialdabaoth for this method. #AllHailNyanCat
            Assembly currentAssembly = Assembly.GetExecutingAssembly();

            IEnumerable<AssemblyLoader.LoadedAssembly> eligible = from a in AssemblyLoader.loadedAssemblies
                                                                  let ass = a.assembly
                                                                  where ass.GetName().Name == currentAssembly.GetName().Name
                                                                  orderby ass.GetName().Version descending, a.path ascending
                                                                  select a;

            // Elect the newest loaded version of CNM to process all patch files.
            // If there is a newer version loaded then don't do anything
            // If there is a same version but earlier in the list, don't do anything either.
            electionWinner = eligible.First().assembly;
            if (eligible.First().assembly != currentAssembly)
            {
                //loaded = true;
                UnityEngine.Debug.Log("CNMAPI: version " + currentAssembly.GetName().Version + " at " + currentAssembly.Location +
                    " lost the election");
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
            return true;
        }
    }
}
