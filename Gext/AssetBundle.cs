using System;
using System.IO;
using LZ4;

namespace Gext
{
	public class AssetBundle
	{
		/**
		 * Returns the path this bundle was loaded from
		 */
		public String FilePath { get; }

		/**
		 * Returns the file system signature of this asset bundle
		 */
		public FileSignature Signature { get; }

		public int FormatVersion { get; }
    	public String UnityVersion { get; }
    	public String GeneratorVersion { get; }

		public bool IsUnityFS()
		{
			return Signature == FileSignature.UnityFS;
		}

		public bool IsCompressed()
		{
			return Signature == FileSignature.UnityWeb;
		}

		/**
		 * Constructs a new Asset bundle based on the file specified by path
		 *
		 * @throws exception if cannot read file
		 */
		public AssetBundle(String filePath)
		{
			this.FilePath = filePath;

			// exception will be propagate
			UnityDataReader reader = new UnityDataReader(File.Open(filePath, FileMode.Open));

			this.Signature = (FileSignature)Enum.Parse(typeof(FileSignature), reader.ReadString());
			this.FormatVersion = reader.ReadInt32();
			this.UnityVersion = reader.ReadString();
			this.GeneratorVersion = reader.ReadString();

			if (this.IsUnityFS())
			{
				this.LoadUnityFs(reader);
			}
			else
			{
				this.LoadRaw(reader);
			}
		}

		private void LoadUnityFs(UnityDataReader reader)
		{
			var fsFileSize = reader.ReadInt64();
			var ciblockSize = (int) reader.ReadUInt32();
			var uiblockSize = (int) reader.ReadUInt32();

			var flags = reader.ReadUInt32();

			var compression = (CompressionType)(flags & 0x3F);

			var data = this.ReadCompressedData(reader, ciblockSize, uiblockSize, compression);
			Console.WriteLine(data.Length);
		}

		private byte[] ReadCompressedData(UnityDataReader reader, int ciblockSize, int uiblockSize, CompressionType compression)
		{
			if (compression == CompressionType.None)
			{
				return reader.ReadBytes(ciblockSize);
			}

			if (compression == CompressionType.Lz4 || compression == CompressionType.Lz4hc)
			{
				var compressedData = reader.ReadBytes(ciblockSize);
				return LZ4Codec.Decode(compressedData, 0, ciblockSize, uiblockSize);
			}

			return new byte[0];
		}

		private void LoadRaw(UnityDataReader reader)
		{
		}


	}
}
