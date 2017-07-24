using System;

namespace CommNetManagerAPI
{
    /// <summary>
    /// Method attribute used to specify if the method returning a bool should be joined to other cooperating methods using AND or OR.
    /// <para>WARNING: Methods that implement 'OR' MUST NOT call base.method() in their body.</para>
    /// <para>Since the stock method will be called anyway, methods should not call base.method() anyway in their body if they detect a CommNetManager installation.</para>
    /// <para>Instead, methods implementing 'AND' should return true and methods implementing 'OR' should return false.</para>
    /// <para>NOTE: Methods implementing 'OR' should generally also be marked 'PRE' and methods implementing 'AND' should generally be marked 'POST'.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CNMAttrAndOr : System.Attribute
    {
        /// <exclude />
        public readonly options op;
        /// <exclude />
        public enum options
        {
            /// <exclude />
            AND,
            /// <exclude />
            OR
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="CNMAttrAndOr"/> class.
        /// </summary>
        /// <param name="op">Enum specifying the option selected.</param>
        public CNMAttrAndOr(options op)
        {
            this.op = op;
        }
    }

    /// <summary>
    /// Method attribute used to specify if the method should be called before or after the stock method.
    /// <para>CAUTION: Methods should not call base.method() in their body if they detect a CommNetManager installation.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CNMAttrPrePost : System.Attribute
    {
        /// <exclude />
        public readonly options op;
        /// <exclude />
        public enum options
        {
            /// <exclude />
            POST,
            /// <exclude />
            PRE
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="CNMAttrPrePost"/> class.
        /// </summary>
        /// <param name="op">Enum specifying the option selected.</param>
        public CNMAttrPrePost(options op)
        {
            this.op = op;
        }
    }
}
