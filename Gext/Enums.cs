using System;
namespace Gext
{
	public enum FileSignature
	{
		UnityFS,
    	UnityWeb,
    	UnityRaw
	}

	public enum CompressionType
	{
    	None = 0,
    	Lzma = 1,
    	Lz4 = 2,
    	Lz4hc = 3,
    	Lzham = 4,
    // not in unity defined
    	Lzfse = 10,
    	Zlib = 11
	}
}
