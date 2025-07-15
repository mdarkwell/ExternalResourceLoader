using Godot;

namespace ExternalResourceLoader.addons.ExternalResourceLoader.Core;

public partial class ExternalResourceManager : Node
{
	/// <summary>
	/// Invoked whenever an external resource was attempted to be loaded. If the resource
	/// was properly loaded, <paramref name="success"/> will be <c>true</c>.
	/// </summary>
	[Signal] public delegate void ExternalResourceLoadedEventHandler(string path, bool success);
	
	public static ExternalResourceManager Instance { get; private set; }

	private ExternalResourceFormatLoader _loader;

	public override void _EnterTree()
	{
		Instance = this;
		_loader = new ExternalResourceFormatLoader();
		ResourceLoader.AddResourceFormatLoader(_loader, true);
	}

	public override void _ExitTree()
	{
		ResourceLoader.RemoveResourceFormatLoader(_loader);
		_loader = null;
	}
}