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
	public class UnityDataReader: BinaryReader
	{
		public Endinanness Endianness { get; }
		public UnityDataReader(Stream stream, Endinanness endianness = Endinanness.BigEndian)
		 : base(stream)
		{
			this.Endianness = endianness;
		}


		public String ReadString(int size = -1)
		{
			byte[] bytes;
			if (size >= 0)
			{
				bytes = this.ReadBytes(size);
			} else
			{
				List<Byte> bs = new List<Byte>();
				while (true)
				{
					try
					{
						var b = this.ReadByte();
						if ((int)b == '\0')
						{
							break;
						}
						bs.Add(b);
					} catch (Exception )
					{
						break;
					}
				}
				bytes = bs.ToArray();
			}

			return System.Text.Encoding.UTF8.GetString(bytes);
		}

		public override int ReadInt32()
		{
			if (this.Endianness == Endinanness.LittleEndian)
			{
				return base.ReadInt32();
			}

			var bytes = ReadBytes(4);
			Array.Reverse(bytes);
			return System.BitConverter.ToInt32(bytes, 0);
		}

		public override uint ReadUInt32()
		{
			if (this.Endianness == Endinanness.LittleEndian)
			{
				return base.ReadUInt32();
			}

			var bytes = ReadBytes(4);
			Array.Reverse(bytes);
			return System.BitConverter.ToUInt32(bytes, 0);
		}

		public override short ReadInt16()
		{
			if (this.Endianness == Endinanness.LittleEndian)
			{
				return base.ReadInt16();
			}

			var bytes = ReadBytes(2);
			Array.Reverse(bytes);
			return System.BitConverter.ToInt16(bytes, 0);
		}

		public override ushort ReadUInt16()
		{
			if (this.Endianness == Endinanness.LittleEndian)
			{
				return base.ReadUInt16();
			}

			var bytes = ReadBytes(2);
			Array.Reverse(bytes);
			return System.BitConverter.ToUInt16(bytes, 0);
		}

		public override long ReadInt64()
		{
			if (this.Endianness == Endinanness.LittleEndian)
			{
				return base.ReadInt64();
			}

			var bytes = ReadBytes(8);
			Array.Reverse(bytes);
			return System.BitConverter.ToInt64(bytes, 0);
		}

		public override ulong ReadUInt64()
		{
			if (this.Endianness == Endinanness.LittleEndian)
			{
				return base.ReadUInt64();
			}

			var bytes = ReadBytes(8);
			Array.Reverse(bytes);
			return System.BitConverter.ToUInt64(bytes, 0);
		}
	}
}
