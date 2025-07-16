# ExternalResourceLoader
A godot C# plugin which attempts to safely load external resources, mitigating bad actors from arbitrary code execution at runtime.

**⚠️ DISCLAIMER: This plugin does not make any guarantee or assurance that bad actors cannot still arbitrarily execute code. This merely mitigates the issue by scanning external resources for embedded scripts.**

## What does this do?
This attempts to scan for code signatures in any external resource loaded by Godot. If a code signature is detected, it will return an empty resource object. Otherwise, it will load the resource as intended.

## How does it work?
The plugin registers a custom high-priority ResourceFormatLoader which only overloads resource loading when attempting to load an external resource or scene. From there, it scans external resource data for any embedded code within the resource or scene. If it finds a code signature, it will return an empty resource instance rather than the resource.

![flowchart](https://raw.githubusercontent.com/mdarkwell/ExternalResourceLoader/refs/heads/master/images/FlowChart.png)

## How do I use it?
Import the add-on, and build the project. Once you build the project, enable the plugin within the project settings.

![](https://github.com/user-attachments/assets/5b120b7e-31dd-4ca3-816f-c8084b9bdf12)

### That's it?
Yep! When the project runs, it will load the ExternalResourceLoader, which automatically overloads `ResourceLoader::Load()` internally. Whether the resource is loaded at runtime, or from a static reference, as long as it's an external resource, it will scan for embedded code.

## Can I detect when a bad resource is loaded?
Yes! Hook the signal `ExternalPluginManager.Instance.ExternalResourceLoaded(string path, bool success)` in order to receive a signal callback whenever an external resource fails to load. For example:

```cs
public override void _Ready()
{
	ExternalResourceManager.Instance.ExternalResourceLoaded += (path, success) => {
		GD.Print($"External resource {(success ? "failed to load" : "loaded")}: {path}");
	};
}
