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
using System.Runtime.Serialization;

namespace ABB.SrcML.Utilities
{
	/// <summary>
	/// ReadOnlyDictionary provides a read-only wrapper (similar to ReadOnlyCollection) for dictionaries.
	/// If the dictionary is consistently accessed through this class, then NotSupportedExceptions will be thrown whenever the class is modified.
	/// </summary>
	/// <typeparam name="TKey">The key type</typeparam>
	/// <typeparam name="TValue">The value type</typeparam>
	public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		private readonly IDictionary<TKey, TValue> dictionary;

		/// <summary>
		/// Create a new ReadOnlyDictionary that with the given dictionary
		/// </summary>
		/// <param name="dictionary">The dictionary to provide a read-only interface for</param>
		public ReadOnlyDictionary(IDictionary<TKey,TValue> dictionary)
		{
			this.dictionary = dictionary;
		}

		private ReadOnlyDictionary()
		{

		}

#region IDictionary<TKey,TValue> Members

		/// <summary>
		/// Not supported
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void  Add(TKey key, TValue value)
		{
			throw new NotSupportedException("This is a read-only dictionary.");
		}

		/// <summary>
		/// True if this dictionary contains <paramref name="key"/>
		/// </summary>
		/// <param name="key">the key to check for</param>
		/// <returns>true if present; false otherwise</returns>
		public bool  ContainsKey(TKey key)
		{
			return dictionary.ContainsKey(key);
		}

		/// <summary>
		/// a collection of the keys in this dictionary
		/// </summary>
		public ICollection<TKey>  Keys
		{
			get { return dictionary.Keys; }
		}

		/// <summary>
		/// Not supported
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool  Remove(TKey key)
		{
			throw new NotSupportedException("This is a read-only dictionary.");
		}

		/// <summary>
		/// place the value corresponding to <paramref name="key"/> into <paramref name="value"/>.
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <param name="value">the out parameter to place the result in</param>
		/// <returns>true if the key is present; false otherwise</returns>
		public bool  TryGetValue(TKey key, out TValue value)
		{
			return dictionary.TryGetValue(key, out value);
		}

		/// <summary>
		/// a collection of the values in this dictionary
		/// </summary>
		public ICollection<TValue>  Values
		{
			get { return dictionary.Values; }
		}

		/// <summary>
		/// Returns the value corresponding to the key
		/// The Setter is not supported.
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns>the value corresponding to <paramref name="key"/></returns>
		public TValue this[TKey key]
		{
			get
			{
				return dictionary[key];
			}
			set
			{
				throw new NotSupportedException("This is a read-only dictionary.");
			}
		}
	#endregion

#region ICollection<KeyValuePair<TKey,TValue>> Members
		/// <summary>
		/// Not supported
		/// </summary>
		/// <param name="item"></param>
		public void Add(KeyValuePair<TKey,TValue> item)
		{
			throw new NotSupportedException("This collection is read-only.");
		}

		/// <summary>
		/// Not supported
		/// </summary>
		public void Clear()
		{
			throw new NotSupportedException("This collection is read-only.");
		}

		/// <summary>
		/// Tests whether or not the Key-Value Pair <paramref name="item"/> is contained in this dictionary
		/// </summary>
		/// <param name="item">the key-value pair to check for</param>
		/// <returns>true if present; false otherwise</returns>
		public bool  Contains(KeyValuePair<TKey,TValue> item)
		{
			return dictionary.Contains(item);
		}

		/// <summary>
		/// Copies the contents of this dictionary to <paramref name="array"/>, starting at <paramref name="arrayIndex"/>
		/// </summary>
		/// <param name="array">The array to copy the Key-Value Pairs to</param>
		/// <param name="arrayIndex">The index to start copying to</param>
		public void  CopyTo(KeyValuePair<TKey,TValue>[] array, int arrayIndex)
		{
			dictionary.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Returns the number of Key-Value Pairs in this dictionary
		/// </summary>
		public int  Count
		{
			get { return dictionary.Count; }
		}

		/// <summary>
		/// Returns true, as this dictionary is always read-only
		/// </summary>
		public bool  IsReadOnly
		{
			get { return true; }
		}

		/// <summary>
		/// Not supported
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool  Remove(KeyValuePair<TKey,TValue> item)
		{
			throw new NotSupportedException("This collection is read-only.");
		}

#endregion

#region IEnumerable<KeyValuePair<TKey,TValue>> Members

		/// <summary>
		/// Returns an enumerator over the Key-Value Pairs in this dictionary
		/// </summary>
		/// <returns>An Enumerator</returns>
		public IEnumerator<KeyValuePair<TKey,TValue>>  GetEnumerator()
		{
			return dictionary.GetEnumerator();
		}

#endregion

#region IEnumerable Members
		/// <summary>
		/// Returns an enumerator over the Key-Value Pairs in this dictionary
		/// </summary>
		/// <returns>An Enumerator</returns>
		System.Collections.IEnumerator  System.Collections.IEnumerable.GetEnumerator()
		{
			return dictionary.GetEnumerator();
		}

#endregion
	}
}
