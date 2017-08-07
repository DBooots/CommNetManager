using System;
using System.Collections.Generic;
using System.Linq;

namespace CommNetManagerAPI
{
    /// <summary>
    /// A custom <see cref="List{T}"/> type for sorting methods into early, late, and post sequence.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SequenceList<T>
    {
        private List<T> el;
        private List<T> lp;
        private List<T> ep;
        private List<T> a;
        bool[] dirty = { true, true, true, true };
        /// <summary>
        /// Gets the Early and Late lists concatenated.
        /// </summary>
        public List<T> EarlyLate
        {
            get
            {
                if (dirty[0])
                {
                    this.el = this.Early.Concat(this.Late).ToList();
                    dirty[0] = false;
                }
                return this.el;
            }
        }
        /// <summary>
        /// Gets the Late and Post lists concatenated.
        /// </summary>
        public List<T> LatePost
        {
            get
            {
                if (dirty[1])
                {
                    this.lp = this.Late.Concat(this.Post).ToList();
                    dirty[1] = false;
                }
                return this.lp;
            }
        }
        /// <summary>
        /// Gets the Early and Post lists concatenated.
        /// </summary>
        public List<T> EarlyPost
        {
            get
            {
                if (dirty[2])
                {
                    this.ep = this.Early.Concat(this.Post).ToList();
                    dirty[2] = false;
                }
                return this.ep;
            }
        }
        /// <summary>
        /// Gets the Early, Late, and Post lists concatenated.
        /// </summary>
        public List<T> All
        {
            get
            {
                if (dirty[3])
                {
                    this.a = this.Early.Concat(this.Late).Concat(this.Post).ToList();
                    dirty[3] = false;
                }
                return this.a;
            }
        }
        /// <summary>
        /// The Early list.
        /// </summary>
        public List<T> Early;
        /// <summary>
        /// The Late list.
        /// </summary>
        public List<T> Late;
        /// <summary>
        /// The Post list.
        /// </summary>
        public List<T> Post;
        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceList{T}"/> class.
        /// </summary>
        /// <param name="Early">The Early list.</param>
        /// <param name="Late">The Late list.</param>
        /// <param name="Post">The Post list.</param>
        public SequenceList(List<T> Early = null, List<T> Late = null, List<T> Post = null)
        {
            Action test = this.Clear;
            test += this.Clear;

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
        /// <summary>
        /// Gets the specified <see cref="List{T}"/>.
        /// </summary>
        /// <value>
        /// The <see cref="List{T}"/>.
        /// </value>
        /// <param name="i">The sublist index.<para /> 0=>Early, 1=>Late, 2=> Post</param>
        /// <returns></returns>
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
                        UnityEngine.Debug.LogError("SequenceList: The provided int was out of range.");
                        return null;
                }
            }
        }
        /// <summary>
        /// Gets the specified <see cref="List{T}"/>.
        /// </summary>
        /// <value>
        /// The <see cref="List{T}"/>.
        /// </value>
        /// <param name="sequence">The sublist.</param>
        /// <returns></returns>
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
        /// <summary>
        /// Adds an object to the end of the specified sublist of the <see cref="SequenceList{T}"/>.
        /// </summary>
        /// <param name="sequence">The sublist.</param>
        /// <param name="obj">The object to add.</param>
        public virtual void Add(CNMAttrSequence.options sequence, T obj)
        {
            switch (sequence)
            {
                case CNMAttrSequence.options.EARLY:
                    this.Early.Add(obj);
                    dirty[0] = true;
                    break;
                case CNMAttrSequence.options.LATE:
                    this.Late.Add(obj);
                    dirty[1] = true;
                    break;
                case CNMAttrSequence.options.POST:
                    this.Post.Add(obj);
                    dirty[2] = true;
                    break;
            }
            dirty[3] = true;
        }
        /// <summary>
        /// Removes the first instance of an object from the specified sublist of the <see cref="SequenceList{T}"/>.
        /// </summary>
        /// <param name="sequence">The sublist.</param>
        /// <param name="obj">The object to remove.</param>
        public virtual void Remove(CNMAttrSequence.options sequence, T obj)
        {
            switch (sequence)
            {
                case CNMAttrSequence.options.EARLY:
                    this.Early.Remove(obj);
                    dirty[0] = true;
                    break;
                case CNMAttrSequence.options.LATE:
                    this.Late.Remove(obj);
                    dirty[1] = true;
                    break;
                case CNMAttrSequence.options.POST:
                    this.Post.Remove(obj);
                    dirty[2] = true;
                    break;
            }
            dirty[3] = true;
        }
        /// <summary>
        /// Removes all elements from the <see cref="SequenceList{T}"/>.
        /// </summary>
        public virtual void Clear()
        {
            this.dirty = new bool[] { true, true, true, true };
            this.Early.Clear();
            this.Late.Clear();
            this.Post.Clear();
        }
        /// <summary>
        /// Removes all elements from sublist of the <see cref="SequenceList{T}"/>.
        /// </summary>
        public virtual void Clear(CNMAttrSequence.options sequence)
        {
            switch (sequence)
            {
                case CNMAttrSequence.options.EARLY:
                    this.Early.Clear();
                    dirty[0] = true;
                    break;
                case CNMAttrSequence.options.LATE:
                    this.Late.Clear();
                    dirty[1] = true;
                    break;
                case CNMAttrSequence.options.POST:
                    this.Post.Clear();
                    dirty[2] = true;
                    break;
            }
            dirty[3] = true;
        }
    }

    /// <summary>
    /// A custom <see cref="List{T}"/> type for sorting methods into early, late, and post sequence. Includes a metadata field.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TBacking"></typeparam>
    public class SequenceList<T, TBacking> : SequenceList<T>
    {
        /// <summary>
        /// The metadata dictionary.
        /// </summary>
        public Dictionary<T, TBacking> MetaDict = new Dictionary<T, TBacking>();
        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceList{T, TBacking}"/> class.
        /// </summary>
        public SequenceList()
        {
            this.Early = new List<T>();
            this.Late = new List<T>();
            this.Post = new List<T>();
        }
        /// <summary>
        /// Adds an object to the end of the specified sublist of the <see cref="SequenceList{T, TBacking}"/>.
        /// </summary>
        /// <param name="sequence">The sublist.</param>
        /// <param name="obj">The object to add.</param>
        /// <param name="meta">The metadata for the object.</param>
        public void Add(CNMAttrSequence.options sequence, T obj, TBacking meta)
        {
            MetaDict.Add(obj, meta);
            base.Add(sequence, obj);
        }
        /// <summary>
        /// Removes all elements from the <see cref="SequenceList{T, TBacking}"/>.
        /// </summary>
        public override void Clear()
        {
            this.MetaDict.Clear();
            base.Clear();
        }
        /// <summary>
        /// Removes all elements from sublist of the <see cref="SequenceList{T, TBacking}"/>.
        /// </summary>
        public override void Clear(CNMAttrSequence.options sequence)
        {
            List<T> sublist;
            IEnumerable<T> otherLists;
            switch (sequence)
            {
                case CNMAttrSequence.options.EARLY:
                    sublist = this.Early;
                    otherLists = this.Late.Concat(this.Post);
                    break;
                case CNMAttrSequence.options.LATE:
                    sublist = this.Late;
                    otherLists = this.Early.Concat(this.Post);
                    break;
                case CNMAttrSequence.options.POST:
                    sublist = this.Post;
                    otherLists = this.Early.Concat(this.Late);
                    break;
                default:
                    sublist = null;
                    otherLists = null;
                    break;
            }
            foreach (T obj in sublist)
            {
                if (otherLists.Contains(obj))
                    continue;
                if (MetaDict.ContainsKey(obj))
                    MetaDict.Remove(obj);
            }
            base.Clear(sequence);
        }
        /// <summary>
        /// Removes the first instance of an object from the specified sublist of the <see cref="SequenceList{T, TBacking}"/>.
        /// </summary>
        /// <param name="sequence">The sublist.</param>
        /// <param name="obj">The object to remove.</param>
        public override void Remove(CNMAttrSequence.options sequence, T obj)
        {
            IEnumerable<T> otherLists;
            switch (sequence)
            {
                case CNMAttrSequence.options.EARLY:
                    otherLists = this.Late.Concat(this.Post);
                    break;
                case CNMAttrSequence.options.LATE:
                    otherLists = this.Early.Concat(this.Post);
                    break;
                case CNMAttrSequence.options.POST:
                    otherLists = this.Early.Concat(this.Late);
                    break;
                default:
                    otherLists = null;
                    break;
            }
            if (!otherLists.Contains(obj))
                MetaDict.Remove(obj);
            base.Remove(sequence, obj);
        }
    }
}
