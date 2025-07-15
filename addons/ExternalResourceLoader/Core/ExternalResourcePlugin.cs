using Godot;

namespace ExternalResourceLoader.addons.ExternalResourceLoader.Core;

#if TOOLS
[Tool]
public partial class ExternalResourcePlugin : EditorPlugin
#else
public partial class ExternalResourcePlugin : Node
#endif
{
	private const string PluginDir = "res://addons/ExternalResourceLoader";
	
#if TOOLS
	public override void _EnterTree()
	{
		AddAutoloadSingleton("ExternalResourceLoader", $"{PluginDir}/Core/ExternalResourceManager.cs");
	}

	public override void _ExitTree()
	{
		RemoveAutoloadSingleton("ExternalResourcePlugin");

	}

	public override string _GetPluginName()
	{
		return "External Resource Plugin";
	}
#endif
}