using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Peach.Core.IO
{
	[Serializable]
	[DebuggerDisplay("Count = {Count}")]
	public class BitStreamList : BitwiseStream, IList<BitwiseStream>
	{
		#region Private Members

		private IList<BitwiseStream> _streams;
		private long _position;
		private long _length;

		#endregion

		#region Constructor

		public BitStreamList()
		{
			_streams = new List<BitwiseStream>();
		}

		public BitStreamList(int capacity)
		{
			_streams = new List<BitwiseStream>(capacity);
		}

		public BitStreamList(IEnumerable<BitwiseStream> collection)
		{
			_streams = new List<BitwiseStream>(collection);
			_streams.ForEach(s => _length += s.LengthBits);
		}

		#endregion

		#region IDisposable

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			foreach (var item in _streams)
				item.Dispose();

			_streams.Clear();
		}

		#endregion

		#region BitwiseStream Interface

		public override long LengthBits
		{
			get { return _length; }
		}

		public override long PositionBits
		{
			get
			{
				return _position;
			}
			set
			{
				_position = value;
			}
		}

		public override long SeekBits(long offset, SeekOrigin origin)
		{
			long pos = 0;

			switch (origin)
			{
				case SeekOrigin.Begin:
					pos = offset;
					break;
				case SeekOrigin.End:
					pos = LengthBits + offset;
					break;
				case SeekOrigin.Current:
					pos = PositionBits + offset;
					break;
			}

			if (pos < 0)
				throw new IOException("An attempt was made to move the position before the beginning of the stream.");

			PositionBits = pos;
			return PositionBits;
		}


		public override int ReadBit()
		{
			long pos = 0;

			foreach (var item in this)
			{
				long next = pos + item.LengthBits;

				if (next > PositionBits)
				{
					var offset = item.PositionBits;
					item.PositionBits = PositionBits - pos;
					var ret = item.ReadBit();
					item.PositionBits = offset;
					++PositionBits;
					return ret;
				}

				pos = next;
			}

			return -1;
		}

		public override int ReadBits(out ulong bits, int count)
		{
			if (count > 64 || count < 0)
				throw new ArgumentOutOfRangeException("count");

			bits = 0;

			int needed = count;
			long pos = 0;

			foreach (var item in this)
			{
				long next = pos + item.LengthBits;

				if (next >= PositionBits)
				{
					long offset = item.PositionBits;
					item.PositionBits = PositionBits - pos;
					ulong tmp;
					int len = item.ReadBits(out tmp, needed);
					item.PositionBits = offset;

					bits <<= len;
					bits |= tmp;
					PositionBits += len;
					needed -= len;

					if (needed == 0)
						break;
				}

				pos = next;
			}

			return count - needed;
		}

		public override void SetLengthBits(long value)
		{
			throw new NotSupportedException("Stream does not support writing.");
		}

		public override void WriteBit(int value)
		{
			throw new NotSupportedException("Stream does not support writing.");
		}

		public override void WriteBits(ulong bits, int count)
		{
			throw new NotSupportedException("Stream does not support writing.");
		}

		IEnumerable<BitStream> Walk()
		{
			var toVisit = new Stack<BitwiseStream>();
			toVisit.Push(null);

			BitwiseStream elem = this;

			while (elem != null)
			{
				var asList = elem as BitStreamList;

				if (asList != null)
				{
					for (int i = asList.Count - 1; i >= 0; --i)
						toVisit.Push(asList[i]);
				}
				else
				{
					yield return (BitStream)elem;
				}

				elem = toVisit.Pop();
			}
		}

		public override BitwiseStream SliceBits(long length)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException("length");

			long pos = 0;

			// Recursively walk the bitstream lists looking
			// for the BitStream that is at 'position'

			var needed = length;

			var ret = new BitStreamList();

			var offset = pos;

			// Skip until we find the 1st element at our position
			var seq = Walk().SkipWhile(e => (PositionBits >= (pos += e.LengthBits)));

			foreach (var e in seq)
			{
				if (pos != 0)
				{
					offset = e.LengthBits - (pos - PositionBits);
					pos = 0;

					// First time thru, we should always have at least 1 bit to read
					Debug.Assert((e.LengthBits - offset) > 0);
				}

				var toWrite = Math.Min(e.LengthBits - offset, needed);

				if (toWrite > 0)
				{
					var cur = e.PositionBits;
					e.SeekBits(offset, SeekOrigin.Begin);
					ret.Add(e.SliceBits(toWrite));
					e.SeekBits(cur, SeekOrigin.Begin);
				}

				needed -= toWrite;
				offset = 0;

				if (needed == 0)
					break;
			}

			if (needed != 0)
				throw new ArgumentException("length");

			// mark list as read only
			ret._streams = ((List<BitwiseStream>)ret._streams).AsReadOnly();

			// Update our position
			SeekBits(length, SeekOrigin.Current);

			return ret;
		}

		#endregion

		#region Element Positions

		public override bool TryGetPosition(string name, out long position)
		{
			position = 0;
			return ScanUntil(name, ref position);
		}

		protected bool ScanUntil(string name, ref long position)
		{
			if (Name == name)
				return true;

			foreach (var item in this)
			{
				if (item.Name == name)
					return true;

				var lst = item as BitStreamList;

				if (lst == null)
					position += item.LengthBits;
				else if (lst.ScanUntil(name, ref position))
					return true;
			}

			return false;
		}

		#endregion

		#region Stream Interface

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return true; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override void Flush()
		{
		}

		public override long Length
		{
			get { return LengthBits / 8; }
		}

		public override long Position
		{
			get
			{
				return PositionBits / 8;
			}
			set
			{
				PositionBits = value * 8;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");

			if (count < 0)
				throw new ArgumentOutOfRangeException("count");

			if ((offset + count) > buffer.Length)
				throw new ArgumentOutOfRangeException("count");

			int bits = 0;
			int needed = count;
			long pos = 0;
			byte glue = 0;

			foreach (var item in this)
			{
				long next = pos + item.LengthBits;

				if (next >= PositionBits)
				{
					long restore = item.PositionBits;
					item.PositionBits = PositionBits - pos;

					// If we are not aligned reading into buffer, get back aligned
					ulong tmp;
					if (bits != 0)
					{
						int len = item.ReadBits(out tmp, 8 - bits);
						glue |= (byte)(tmp << (8 - bits - len));
						PositionBits += len;
						bits += len;

						// Advance offset once buffer is aligned again
						if (bits == 8)
						{
							buffer[offset] = glue;
							++offset;
							--needed;
							bits = 0;
							glue = 0;
						}
					}

					// If we are aligned, read directly into the buffer
					if (bits == 0)
					{
						int len = item.Read(buffer, offset, needed);

						offset += len;
						needed -= len;
						PositionBits += ((long)len * 8);

						// Ensure we read any leftover bits
						if (needed > 0)
						{
							bits = item.ReadBits(out tmp, 7);
							glue = (byte)(tmp << (8 - bits));
							PositionBits += bits;
						}
					}

					item.PositionBits = restore;

					if (bits == 0 && needed == 0)
						break;
				}

				pos = next;
			}

			// If we have partial bits we failed to glue into a whole byte
			// we need to back up our position
			PositionBits -= bits;

			return count - needed;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return SeekBits(offset * 8, origin) / 8;
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException("Stream does not support writing.");
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException("Stream does not support writing.");
		}

		#endregion

		#region IList<BitwiseStream> Members

		public int IndexOf(BitwiseStream item)
		{
			return _streams.IndexOf(item);
		}

		public void Insert(int index, BitwiseStream item)
		{
			_streams.Insert(index, item);
			_length += item.LengthBits;
		}

		public void RemoveAt(int index)
		{
			_length -= _streams[index].LengthBits;
			Debug.Assert(_length >= 0);
			_streams.RemoveAt(index);
		}

		public BitwiseStream this[int index]
		{
			get
			{
				return _streams[index];
			}
			set
			{
				_length -= _streams[index].LengthBits;
				Debug.Assert(_length >= 0);

				_streams[index] = value;
				_length += value.LengthBits;
			}
		}

		public void Add(BitwiseStream item)
		{
			_streams.Add(item);
			_length += item.LengthBits;
		}

		public void Clear()
		{
			_streams.Clear();
			_length = 0;
		}

		public bool Contains(BitwiseStream item)
		{
			return _streams.Contains(item);
		}

		public void CopyTo(BitwiseStream[] array, int arrayIndex)
		{
			_streams.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _streams.Count; }
		}

		public bool IsReadOnly
		{
			get { return _streams.IsReadOnly; }
		}

		public bool Remove(BitwiseStream item)
		{
			if (!_streams.Remove(item))
				return false;

			_length -= item.LengthBits;
			Debug.Assert(_length >= 0);
			return true;
		}

		public IEnumerator<BitwiseStream> GetEnumerator()
		{
			return _streams.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _streams.GetEnumerator();
		}

		#endregion
	}
}
