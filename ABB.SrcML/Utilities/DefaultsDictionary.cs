/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Utilities
{
    /// <summary>
    /// DefaultsDictionary is a dictionary that starts with a list of default keys and values.
    /// For functions that return only a single key, value, or Key-Value Pair, the method will first check its collection of non-default values. Then, it will check its collection of *default* values.
    /// 
    /// For functions that return a collection or enumerator, the WillReturnDefaultValues flag controls whether or not default values are returned. If false (the default), default values are *not* returned.
    /// This means that a DefaultsDictionary that consists of only default Key-Value Pairs will return an empty enumerator/collection.
    /// </summary>
    /// <typeparam name="TKey">The Key</typeparam>
    /// <typeparam name="TValue">The Value</typeparam>
    public class DefaultsDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly ReadOnlyDictionary<TKey, TValue> defaults;
        private Dictionary<TKey, TValue> options;

        private DefaultsDictionary()
        {

        }

        /// <summary>
        /// Constructs a new defaults dictionary.
        /// </summary>
        /// <param name="defaultDictionary">the initial dictionary of defaultsDictionary</param>
        /// <param name="willReturnDefaultValues">true if it should return default values in collections and enumerators</param>
        public DefaultsDictionary(IDictionary<TKey, TValue> defaultDictionary, bool willReturnDefaultValues)
        {
            this.defaults = new ReadOnlyDictionary<TKey, TValue>(defaultDictionary);
            this.options = new Dictionary<TKey, TValue>();
            WillReturnDefaultValues = willReturnDefaultValues;
        }

        /// <summary>
        /// Constructs a new defaults dictionary.
        /// WillReturnDefaultValues is set to false by default.
        /// </summary>
        /// <param name="defaultDictionary">the initial dictionary of defaultsDictionary</param>
        public DefaultsDictionary(IDictionary<TKey, TValue> defaultDictionary) : this(defaultDictionary, false)
        {
        }

        /// <summary>
        /// This boolean determines whether or not enumerators, iterators, etc will return both default and non-default values.
        /// If true, then default values will be returned as long as a non-default value is not present.
        /// If false, then only changed values will be returned in iterators.
        /// </summary>
        public bool WillReturnDefaultValues
        {
            get;
            set;
        }

        /// <summary>
        /// The number of non-default Key-Value Pairs present in this dictionary.
        /// </summary>
        public int NonDefaultValueCount
        {
            get { return options.Count;  }
        }

        /// <summary>
        /// The number of default Key-Value pairs present in this dictionary.
        /// </summary>
        public int DefaultValueCount
        {
            get { return defaults.Count; }
        }

        #region IDictionary<TKey,TValue> Members

        /// <summary>
        /// Adds the key to the non-default set of key-value pairs
        /// </summary>
        /// <param name="key">the key to add</param>
        /// <param name="value">the value corresponding to the key</param>
        public void Add(TKey key, TValue value)
        {
            this.options.Add(key, value);
        }

        /// <summary>
        /// ContainsKey returns true if there is either a default or non-default key that matches <paramref name="key"/>.
        /// </summary>
        /// <param name="key">the key to search for</param>
        /// <returns>true if the key is present; false otherwise</returns>
        public bool ContainsKey(TKey key)
        {
            return this.defaults.ContainsKey(key) || this.options.ContainsKey(key);
        }

        /// <summary>
        /// If WillReturnDefaultValues is false, this returns the set of non-default keys
        /// If true, it returns the union of non-default and default keys
        /// </summary>
        public ICollection<TKey> Keys
        {
            get
            {
                if(WillReturnDefaultValues)
                    return options.Keys.Union(defaults.Keys).ToArray<TKey>();
                return options.Keys;
            }
        }

        /// <summary>
        /// Removes the given key from the dictionary only if it is not in the initial set of defaults
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <returns>true if the key was removed</returns>
        public bool Remove(TKey key)
        {
            if (options.ContainsKey(key))
                return options.Remove(key);
            return false;
        }

        /// <summary>
        /// Tries to get the value for the  given key from the set of non-default KVPs and default KVPs.
        /// </summary>
        /// <param name="key">the key to find</param>
        /// <param name="value">the value related to <paramref name="key"/></param>
        /// <returns>true  if the Key-Value pair was present, false otherwise.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (options.TryGetValue(key, out value))
                return true;
            else if (defaults.TryGetValue(key, out value))
                return true;
            return false;
        }

        /// <summary>
        /// Returns a list of the default
        /// </summary>
        public ICollection<TValue> Values
        {
            get
            {
                var nonDefaultValues = from key in this.options.Keys
                                       select this.options[key];
                if (WillReturnDefaultValues)
                {
                    var defaultValues = from key in this.defaults.Keys.Except(this.options.Keys)
                                        select this.defaults[key];
                    return nonDefaultValues.Union(defaultValues).ToArray();
                }
                return nonDefaultValues.ToArray();
            }
        }

        /// <summary>
        /// Indexer for getting/setting Key-Value Pairs in the dictionary.
        /// 
        /// Setting a Key-Value Pair causes a new non-default KVP to be set.
        /// 
        /// Getting a Key-Value Pair first checks the non-default KVP set, and then checks the default KVP set.
        /// </summary>
        /// <param name="key">The key to set/get</param>
        /// <returns>The value corresponding to the key</returns>
        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (options.TryGetValue(key, out value))
                    return value;
                value = defaults[key];
                return value;
            }
            set
            {
                this.options[key] = value;
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        /// <summary>
        /// Adds a new Key-Value Pair to the non-default set
        /// </summary>
        /// <param name="item">The Key-Value Pair to add.</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            options.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Clears the non-default values from the dictionary
        /// </summary>
        public void Clear()
        {
            options.Clear();
        }

        /// <summary>
        /// Checks whether or not the given item is present in the dictionary. First, the non-default KVPs are checked, followed by the default KVPs.
        /// </summary>
        /// <param name="item">The item to check for</param>
        /// <returns>True if the item is contained in this dictionary</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (options.ContainsKey(item.Key))
                return item.Value.Equals(options[item.Key]);
            if (defaults.ContainsKey(item.Key))
                return item.Value.Equals(defaults[item.Key]);
            return false;
        }

        /// <summary>
        /// Copies the Key-Value Pairs to the specified array. If WillReturnDefaultValues is true, then this copies all of the non-default KVPs, followed by any default KVPs.
        /// </summary>
        /// <param name="array">The array to copy to</param>
        /// <param name="arrayIndex">The array index to start copying at</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            (options as ICollection<KeyValuePair<TKey, TValue>>).CopyTo(array, arrayIndex);
            if (WillReturnDefaultValues)
            {
                var defaultKvps = from key in defaults.Keys.Except(options.Keys)
                                  select new KeyValuePair<TKey, TValue>(key, defaults[key]);
                defaultKvps.ToArray().CopyTo(array, arrayIndex + options.Count);
            }
        }

        /// <summary>
        /// The number of Key-Value Pairs in this default dictionary. If WillReturnDefaultValues is true, then this is equal to both default and non-default counts.
        /// If WillReturnDefaultValues is false, then Count == NonDefaultValueCount.
        /// </summary>
        public int Count
        {
            get
            {
                if(WillReturnDefaultValues)
                    return options.Keys.Union(defaults.Keys).Count();
                return options.Keys.Count();
            }
        }

        /// <summary>
        /// Returns false, as this dictionary is never read-only
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the given Key-Value Pair from the list of non-default Key-Value Pairs; false otherwise.
        /// This will return false if the Key-Value Pair is a default value.
        /// </summary>
        /// <param name="item">The Key-Value pair to remove</param>
        /// <returns>true if the item was removed; false otherwise</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (options.ContainsKey(item.Key) && options[item.Key].Equals(item.Value))
                return options.Remove(item.Key);
            return false;
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members
        /// <summary>
        /// Returns an enumerator over the contents of this dictionary. If WillReturnDefaultValues is true, this will include both default &amp; non-default options.
        /// If WillReturnDefaultValues is false, this will include only default options.
        /// </summary>
        /// <returns>The enumerator</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            var kvps = from key in (this as IDictionary<TKey, TValue>).Keys
                       let val = options.ContainsKey(key) ? options[key] : defaults[key]
                       select new KeyValuePair<TKey, TValue>(key, val);
            return kvps.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<KeyValuePair<TKey, TValue>>).GetEnumerator();
        }

        #endregion
    }
}
