using System;

namespace CommNetManagerAPI
{
    /// <summary>
    /// Method attribute used to specify if the method returning a bool should be joined to other cooperating methods using AND or OR.
    /// <para>WARNING: Methods that implement 'OR' MUST NOT call base.method() in their body.</para>
    /// <para>Since the stock method will be called anyway, methods should not call base.method() anyway in their body if they detect a CommNetManager installation.</para>
    /// <para>Instead, methods implementing 'AND' should return true and methods implementing 'OR' should return false.</para>
    /// <para>NOTE: Methods implementing 'OR' should generally also be marked 'EARLY' and methods implementing 'AND' should generally be marked 'LATE'.</para>
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CNMAttrAndOr : System.Attribute
    {
        /// <exclude />
        public readonly options andOr;
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
        /// <param name="andOr">Enum specifying the option selected.</param>
        public CNMAttrAndOr(options andOr)
        {
            this.andOr = andOr;
        }

        /// <exclude />
        public override string ToString()
        {
            return String.Format("CNMAttrAndOr.{0}", andOr.ToString());
        }
    }

    /// <summary>
    /// Method attribute used to specify if the method should be called before or after the stock method.
    /// <para>CAUTION: Methods should not call base.method() in their body if they detect a CommNetManager installation.</para>
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CNMAttrSequence : System.Attribute
    {
        /// <exclude />
        public readonly options when;
        /// <exclude />
        public enum options
        {
            /// <exclude />
            EARLY = 1,
            /// <exclude />
            LATE = 0,
            /// <exclude />
            POST = 2
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="CNMAttrSequence"/> class.
        /// </summary>
        /// <param name="when">Enum specifying the option selected.</param>
        public CNMAttrSequence(options when)
        {
            this.when = when;
        }

        /// <exclude />
        public override string ToString()
        {
            return String.Format("CNMAttrSequence.{0}", when.ToString());
        }
    }

    /// <summary>
    /// Method attribute to specify that the method in question should precede the target type's method.
    /// <para /> Not yet implemented. 
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class CNMAttrBefore : System.Attribute
    {
        /// <exclude />
        public readonly string target = "";
        /// <summary>
        /// Initializes a new instance of the <see cref="CNMAttrBefore"/> class.
        /// </summary>
        /// <param name="target">The target class.</param>
        public CNMAttrBefore(string target)
        {
            this.target = target;
        }
    }

    /// <summary>
    /// Method attribute to specify that the method in question should be preceded by the target type's method.
    /// <para /> Not yet implemented. 
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class CNMAttrAfter : System.Attribute
    {
        /// <exclude />
        public readonly string target = "";
        /// <summary>
        /// Initializes a new instance of the <see cref="CNMAttrAfter"/> class.
        /// </summary>
        /// <param name="target">The target class.</param>
        public CNMAttrAfter(string target)
        {
            this.target = target;
        }
    }
}
