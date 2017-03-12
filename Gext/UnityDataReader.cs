using System;
using System.IO;
using System.Collections.Generic;

namespace Gext
{
	public enum Endinanness
	{
		BigEndian,
		LittleEndian
	}

	public abstract class UnityDataReader
	{
		public Endinanness Endianness { get; }
		protected UnityDataReader(Endinanness endianness = Endinanness.BigEndian)
		{
			Endianness = endianness;
		}

		public abstract byte[] ReadBytes(int count);
		public abstract long Tell();
		public abstract void Seek(long count, SeekOrigin whence = SeekOrigin.Begin);

		public string ReadString(int size = -1)
		{
			byte[] bytes;
			if (size >= 0)
			{
				bytes = ReadBytes(size);
			}
			else
			{
				var bs = new List<byte>();
				while (true)
				{
					try
					{
						var b = ReadBytes(1)[0];
						if (b == '\0')
						{
							break;
						}
						bs.Add(b);
					}
					catch (Exception)
					{
						break;
					}
				}
				bytes = bs.ToArray();
			}

			return System.Text.Encoding.UTF8.GetString(bytes);
		}

		public int ReadInt32()
		{
			var bytes = ReadBytes(4);

			if (Endianness == Endinanness.BigEndian)
			{
				Array.Reverse(bytes);
			}

			return BitConverter.ToInt32(bytes, 0);
		}

		public uint ReadUInt32()
		{
			var bytes = ReadBytes(4);

			if (Endianness == Endinanness.BigEndian)
			{
				Array.Reverse(bytes);
			}

			return BitConverter.ToUInt32(bytes, 0);
		}

		public short ReadInt16()
		{
			var bytes = ReadBytes(2);

			if (Endianness == Endinanness.BigEndian)
			{
				Array.Reverse(bytes);
			}

			return BitConverter.ToInt16(bytes, 0);
		}

		public ushort ReadUInt16()
		{
			var bytes = ReadBytes(2);

			if (Endianness == Endinanness.BigEndian)
			{
				Array.Reverse(bytes);
			}

			return BitConverter.ToUInt16(bytes, 0);
		}

		public long ReadInt64()
		{
			var bytes = ReadBytes(8);

			if (Endianness == Endinanness.BigEndian)
			{
				Array.Reverse(bytes);
			}

			return BitConverter.ToInt64(bytes, 0);
		}

		public ulong ReadUInt64()
		{
			var bytes = ReadBytes(8);

			if (Endianness == Endinanness.BigEndian)
			{
				Array.Reverse(bytes);
			}

			return BitConverter.ToUInt64(bytes, 0);
		}
	}

	public class UnityDataStreamReader: UnityDataReader
	{
		private readonly BinaryReader binaryReader;

		public UnityDataStreamReader(Stream stream, Endinanness endianness = Endinanness.BigEndian)
		 : base(endianness)
		{
			binaryReader = new BinaryReader(stream);
		}

		public override byte[] ReadBytes(int count)
		{
			return binaryReader.ReadBytes(count);
		}

		public override long Tell()
		{
			return binaryReader.BaseStream.Position;
		}

		public override void Seek(long count, SeekOrigin whence = SeekOrigin.Begin)
		{
			binaryReader.BaseStream.Seek(count, whence);
		}
	}

	/**
	 * Class that reads data from a byte array
	 */
	public class UnityDataArrayReader : UnityDataReader
	{
		private readonly byte[] data;
		private int location = 0;

		public UnityDataArrayReader(byte[] data)
		{
			this.data = data;
		}

		public override byte[] ReadBytes(int count)
		{
			int charsToRead = Math.Min(data.Length - location, count);
			byte[] result = new byte[charsToRead];
			Array.Copy(data,location,result, 0, charsToRead);
			location += charsToRead;
			return result;
		}

		public override long Tell()
		{
			return location;
		}

		public override void Seek(long count, SeekOrigin whence = SeekOrigin.Begin)
		{
			switch (whence)
			{
				case SeekOrigin.Begin:
					location = (int)Math.Min(data.Length, count);
					break;
				case SeekOrigin.Current:
					location = (int)Math.Min(data.Length, location + count);
					break;
				case SeekOrigin.End:
					location = (int)Math.Min(data.Length, data.Length + count);
					break;
			}

		}
	}
}
