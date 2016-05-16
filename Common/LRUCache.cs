using System;
using System.Linq;
using System.Collections.Generic;


namespace ProceduralCities
{
	public class LRUCache<TValue> : IEnumerable<KeyValuePair<int, TValue>>
	{
		public delegate TValue Constructor(int key);
		class Node
		{
			public int key;
			public TValue data;
			public Node prev;
			public Node next;
		}

		Dictionary<int, Node> dict = new Dictionary<int, Node>();
		readonly int capacity;
		Node first;
		Node last;
		Constructor constructor;

		int Count
		{
			get
			{
				return dict.Count;
			}
		}

		public LRUCache(Constructor constructor, int capacity = 32)
		{
			if (capacity <= 0)
				throw new ArgumentOutOfRangeException("capacity");
			
			this.capacity = capacity;
			this.constructor = constructor;
		}

		void Remove(Node node)
		{
			if (node.next == null)
				last = node.prev;
			else
				node.next.prev = node.prev;

			if (node.prev == null)
				first = node.next;
			else
				node.prev.next = node.next;
		}

		void Add(Node node)
		{
			node.prev = null;
			node.next = first;

			if (node.next == null)
				last = node;
			else
				node.next.prev = node;

			first = node;
		}

		void MoveToFirst(Node node)
		{
			if (node.prev == null)
				return;
			
			Remove(node);
			Add(node);
		}

		void RemoveLast()
		{
			dict.Remove(last.key);
			Remove(last);
		}

		public bool ContainsKey(int key)
		{
			lock (this)
			{
				return dict.ContainsKey(key);
			}
		}

		public void Clear()
		{
			lock (this)
			{
				first = null;
				last = null;
				dict.Clear();
			}
		}

		public bool TryGetValue(int key, out TValue value)
		{
			Node node;
			lock (this)
			{
				if (dict.TryGetValue(key, out node))
				{
					value = node.data;
					MoveToFirst(node);
					return true;
				}
			}

			value = default(TValue);
			return false;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			lock (this)
			{
				return dict.Select(x => new KeyValuePair<int, TValue>(x.Key, x.Value.data)).ToList().GetEnumerator();
			}
		}

		public IEnumerator<KeyValuePair<int, TValue>> GetEnumerator()
		{
			lock (this)
			{
				return dict.Select(x => new KeyValuePair<int, TValue>(x.Key, x.Value.data)).ToList().GetEnumerator();
			}
		}

		Node GetNode(int key)
		{
			Node node;
			if (dict.TryGetValue(key, out node))
			{
				MoveToFirst(node);
			}
			else
			{
				node = new Node();
				node.key = key;
				node.data = constructor(key);
				dict.Add(key, node);
				Add(node);

				if (dict.Count > capacity)
					RemoveLast();
			}
			return node;
		}

		public TValue this[int key]
		{
			get
			{
				lock (this)
				{
					return GetNode(key).data;
				}
			}
			set
			{
				lock (this)
				{
					GetNode(key).data = value;
				}
			}
		}
	}
}

