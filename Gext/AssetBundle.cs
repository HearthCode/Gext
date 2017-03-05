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
			var ciblockSize = reader.ReadUInt32();
			var uiblockSize = reader.ReadUInt32();

			var flags = reader.ReadUInt32();

			var compression = (CompressionType)(flags & 0x3F);

			var data = this.ReadCompressedData(reader, ciblockSize, uiblockSize, compression);
			Console.WriteLine(data.Length);
		}

		private byte[] ReadCompressedData(UnityDataReader reader, UInt32 ciblockSize, UInt32 uiblockSize, CompressionType compression)
		{
			if (compression == CompressionType.None)
			{
				return reader.ReadBytes((int)ciblockSize);;
			}

			if (compression == CompressionType.Lz4 || compression == CompressionType.Lz4hc)
			{
				byte[] sizeBytes = BitConverter.GetBytes(uiblockSize);
				var compressedData = new byte[sizeBytes.Length + ciblockSize];
				sizeBytes.CopyTo(compressedData, 0);
				reader.ReadBytes((int)ciblockSize).CopyTo(compressedData, sizeBytes.Length);

				var decompressor = new LZ4Stream(new MemoryStream(compressedData), LZ4StreamMode.Decompress);

				using (var ms = new MemoryStream())
			    {
			        decompressor.CopyTo(ms);
			        return ms.ToArray();
			    }
			}

			return new byte[0];
		}

		private void LoadRaw(UnityDataReader reader)
		{
		}


	}
}
