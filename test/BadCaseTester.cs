using System.IO;
using ExternalResourceLoader.addons.ExternalResourceLoader.Core;
using Godot;

namespace ExternalResourceLoader.test;

public partial class BadCaseTester : Node
{
	public override void _EnterTree()
	{
		var plugin = ExternalResourceManager.Instance;
		plugin.QueueFree();
	}

	public override async void _Ready()
	{
		// wait for plugin to be freed to force bad scene to load unprotected
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		
		var badScene = "./test/cases/bad/BadSceneBinary.scn";
		var scene = GD.Load<PackedScene>(Path.GetFullPath(badScene));
		AddChild(scene.Instantiate());
	}
}
