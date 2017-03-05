using System;
using Gext;

class Program
{
	static void Main(string[] args)
	{
		// TODO: argument parser, etc.

		var hearthstoneInstallDir = "/Applications/Hearthstone/Data/OSX/";

		var assetFile = "dbf.unity3d";

		var assetBundle = new AssetBundle(hearthstoneInstallDir+assetFile);
	}
}
