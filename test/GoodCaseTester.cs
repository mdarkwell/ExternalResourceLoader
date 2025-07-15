using System.IO;
using ExternalResourceLoader.addons.ExternalResourceLoader.Core;
using Godot;

namespace ExternalResourceLoader.test;

public partial class GoodCaseTester : Node
{
	public override void _Ready()
	{
		var goodScene = "./test/cases/good/GoodSceneBinary.scn";
		var scene = GD.Load<PackedScene>(Path.GetFullPath(goodScene));
		AddChild(scene.Instantiate());
	}
}
