using System;
using System.IO;
using LZ4;
using System.Collections.Generic;
using System.Diagnostics;

namespace Gext
{
	internal struct UnityfsDescriptor
	{
		internal Int64 fsFileSize;
		internal UInt32 ciblockSize;
		internal UInt32 uiblockSize;
    }

	internal struct RawDescriptor
	{
		internal UInt32 fileSize;
		internal Int32 headerSize;
		internal Int32 fileCount;
		internal Int32 bundleCount;
		internal UInt32 bundleSize;
		internal UInt32 uncompressedBundleSize;
		internal UInt32 compressedFileSize;
		internal UInt32 assetHeaderSize;
		internal UInt32 numAssets;
    }

	public class AssetBundle
	{
		/**
		 * Returns the path this bundle was loaded from
		 */
		public string FilePath { get; }

		public string Name { get; private set; }

		/**
		 * Returns the file system signature of this asset bundle
		 */
		public FileSignature Signature { get; }

		public List<Asset> Assets { get; }

		public int FormatVersion { get; }
    	public string UnityVersion { get; }
    	public string GeneratorVersion { get; }

		UnityfsDescriptor unityfsDescriptor;
		RawDescriptor rawDescriptor;

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
		public AssetBundle(string filePath)
		{
			FilePath = filePath;
			Assets = new List<Asset>();

			// exception will be propagate
			UnityDataReader reader = new UnityDataStreamReader(File.Open(filePath, FileMode.Open));

			Signature = (FileSignature)Enum.Parse(typeof(FileSignature), reader.ReadString());
			FormatVersion = reader.ReadInt32();
			UnityVersion = reader.ReadString();
			GeneratorVersion = reader.ReadString();

			if (IsUnityFS())
			{
				LoadUnityFs(reader);
			}
			else
			{
				LoadRaw(reader);
			}
		}

		private void LoadUnityFs(UnityDataReader reader)
		{
			unityfsDescriptor = new UnityfsDescriptor();
			unityfsDescriptor.fsFileSize = reader.ReadInt64();
			unityfsDescriptor.ciblockSize = reader.ReadUInt32();
			unityfsDescriptor.uiblockSize = reader.ReadUInt32();

			var flags = reader.ReadUInt32();

			var compression = (CompressionType)(flags & 0x3F);

			var blk = new UnityDataArrayReader(ReadCompressedData(reader, (int)unityfsDescriptor.ciblockSize, (int)unityfsDescriptor.uiblockSize, compression));

			blk.ReadBytes(16); // guid

			// read Archive block infos
			var numBlocks = blk.ReadInt32();

			ArchiveBlockInfo[] blocks = new ArchiveBlockInfo[numBlocks];
			for (int i = 0; i < numBlocks; i++)
			{
				var busize = blk.ReadInt32();
				var bcsize = blk.ReadInt32();
				var bflags = blk.ReadInt16();

				blocks[i] = new ArchiveBlockInfo(busize, bcsize, bflags);
			}

			// Read Asset data infos
			var numNodes = blk.ReadInt32();

			AssetDataInfo[] nodes = new AssetDataInfo[numNodes];
			for (int i = 0; i < numNodes; i++)
			{
				var offset = blk.ReadInt64();
				var size = blk.ReadInt64();
				var status = blk.ReadInt32();
				var name = blk.ReadString();

				nodes[i] = new AssetDataInfo(offset, size, status, name);
			}

			// read block storage
			var storage = new ArchiveBlockStorage(blocks, reader);

			foreach (var info in nodes)
			{
				storage.Seek(info.Offset);
				var asset = new Asset(this, storage, info.Name);

				Assets.Add(asset);
			}

			if (Assets.Count > 0)
			{
				Name = Assets[0].Name;
			}
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
			throw new DecompressionException("Unsupported compression type: "+compression.ToString());
		}

		private void LoadRaw(UnityDataReader reader)
		{
			rawDescriptor = new RawDescriptor();
			rawDescriptor.fileSize = reader.ReadUInt32();
			rawDescriptor.headerSize = reader.ReadInt32();
			rawDescriptor.fileCount = reader.ReadInt32();

			if (FormatVersion >= 2) {
				rawDescriptor.bundleSize = reader.ReadUInt32();

				if (FormatVersion >= 3) {
					rawDescriptor.uncompressedBundleSize = reader.ReadUInt32();

				}
			}

			if (rawDescriptor.headerSize >= 60) {
				rawDescriptor.compressedFileSize = reader.ReadUInt32();
				rawDescriptor.assetHeaderSize = reader.ReadUInt32();
			}

			reader.ReadInt32();
			reader.ReadBytes(1);
			Name = reader.ReadString();
		}
	}

	internal class ArchiveBlockStorage : UnityDataReader
	{
		ArchiveBlockInfo[] blocks;
		UnityDataReader reader;
		long basePos;
		bool sought;
		long maxPos;
		long cursor;
		UnityDataReader currentStream;
		readonly long currentBlockStart;
		ArchiveBlockInfo currentBlock;

		public ArchiveBlockStorage(ArchiveBlockInfo[] blocks, UnityDataReader reader)
		{
			this.blocks = blocks;
			this.reader = reader;

			basePos = reader.Tell();

			// sum up all block uncompressed sizes
			maxPos = 0;
			foreach (var block in blocks)
			{
				maxPos += block.UncompressedSize;
			}

			sought = false;
			Seek(0);
		}

		public override long Tell()
		{
			return cursor;
		}

		void Seek(long newPos)
		{
			cursor = newPos;
			if (!IsInCurrentBlock(newPos))
			{
				SeekToBlock(newPos);
			}

			if (currentStream != null)
			{
				var k = newPos - currentBlockStart;
				currentStream.Seek(k);
			}
		}

		public override void Seek(long count, SeekOrigin whence = SeekOrigin.Begin)
		{
			long newPos = 0;
			switch (whence)
			{
				case SeekOrigin.Begin:
					newPos = count;
					break;
				case SeekOrigin.Current:
					newPos = cursor + count;
					break;
				case SeekOrigin.End:
					newPos = maxPos + count;
					break;
			}
			if (cursor != newPos)
			{
				Seek(newPos);
			}
		}

		public override byte[] ReadBytes(int count)
		{
			List<byte> buf = new List<byte>();
			var size = count;
			while (size != 0 && cursor < maxPos)
			{
				if (!IsInCurrentBlock(cursor))
				{
					SeekToBlock(cursor);
				}
				var part = currentStream.ReadBytes(size);
				if (size > 0)
				{
					Debug.Assert(part.Length != 0, "EOF error while reading and AssetBlockStorage");
					size -= part.Length;
				}
				cursor += part.Length;
				buf.AddRange(part);
			}
			return buf.ToArray();
		}

		void SeekToBlock(long pos)
		{
			Int32 baseOffset = 0;
			Int32 offset = 0;
			foreach (var b in blocks)
			{
				if ((offset + b.UncompressedSize) > pos)
				{
					currentBlock = b;
					break;
				}
				baseOffset += b.compressedSize;
				offset += b.UncompressedSize;
			}

			this.reader.Seek(basePos + baseOffset);
			if (currentBlock != null)
			{
				var buf = reader.ReadBytes(currentBlock.compressedSize);

				currentStream = new UnityDataArrayReader(currentBlock.Decompress(buf));
			}
		}

		bool IsInCurrentBlock(long pos)
		{
			if (currentBlock != null)
			{
				var end = currentBlockStart + currentBlock.UncompressedSize;
				return (currentBlockStart <= pos) && (pos < end);
			}
			return false;
		}
	}

	internal class ArchiveBlockInfo
	{
		internal Int32 UncompressedSize { get; private set; }
		internal Int32 compressedSize;
		Int16 flags;

		CompressionType CompressionType
		{
			get
			{
				return (CompressionType)(flags & 0x3f);
			}
		}

		public ArchiveBlockInfo(Int32 uncompressedSize, Int32 compressedSize, Int16 flags)
		{
			UncompressedSize = uncompressedSize;
			this.compressedSize = compressedSize;
			this.flags = flags;
		}

		bool isCompressed()
		{
			return CompressionType != CompressionType.None;
		}

		internal byte[] Decompress(byte[] data)
		{
			switch (CompressionType)
			{
				case CompressionType.None:
					return data;
				case CompressionType.Lz4:
				case CompressionType.Lz4hc:
					return LZ4Codec.Decode(data, 0, compressedSize, compressedSize);
				case CompressionType.Lzma:
				case CompressionType.Zlib:
				case CompressionType.Lzfse:
				case CompressionType.Lzham:
				default:
					throw new DecompressionException("Compression type" + CompressionType.ToString() +" is not yet implemented");
			}

		}

	}

	internal struct AssetDataInfo
	{
		internal long Offset;
		internal long Size;
		internal int Status;
		internal string Name;

		public AssetDataInfo(long offset, long size, int status, string name)
		{
			Offset = offset;
			Size = size;
			Status = status;
			Name = name;
		}
	}

	public class DecompressionException : Exception
	{
		public DecompressionException() : base() { }
		public DecompressionException(string message) : base(message) { }
		public DecompressionException(string message, Exception inner) : base(message, inner) { }
	}
}
