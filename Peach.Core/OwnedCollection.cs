using System;
using Peach.Core.Dom;

namespace Peach.Core
{
	/// <summary>
	/// Simple interface for objects that are owned by a parent
	/// </summary>
	/// <typeparam name="T">Type of the owner</typeparam>
	public interface IOwned<T>
	{
		/// <summary>
		/// Returns the parent of the object
		/// </summary>
		T parent { get; set; }
	}

	public class OwnedCollection<TOwner, TObject> : NamedCollection<TObject> where TObject : INamed, IOwned<TOwner>
	{
		private TOwner owner;

		public OwnedCollection(TOwner owner)
		{
			this.owner = owner;
		}

		public OwnedCollection(TOwner owner, string baseName)
			: base(baseName)
		{
			this.owner = owner;
		}

		protected override void InsertItem(int index, TObject item)
		{
			item.parent = owner;

			base.InsertItem(index, item);
		}

		protected override void RemoveItem(int index)
		{
			this[index].parent = default(TOwner);

			base.RemoveItem(index);
		}

		protected override void SetItem(int index, TObject item)
		{
			this[index].parent = default(TOwner);
			item.parent = owner;

			base.SetItem(index, item);
		}
	}
}
