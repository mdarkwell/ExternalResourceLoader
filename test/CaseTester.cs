using System.Diagnostics;
using Godot;
using System.IO;
using ExternalResourceLoader.addons.ExternalResourceLoader.Core;

/// <summary>
/// Tests both bad and good cases, and their asserts their respective outcomes.
/// If tests pass successfully, the program should exit with code 0.
/// </summary>
internal partial class CaseTester : Node
{
	private string _lastExtResourceLoaded;
	private bool _lastExtSuccessful;
	
	public override void _EnterTree()
	{
		ExternalResourceManager.Instance.ExternalResourceLoaded += (path, state) =>
		{
			_lastExtResourceLoaded = path;
			_lastExtSuccessful = state;
		};
		if (!TestFiles("./test/cases/good", true))
		{
			Debug.Assert(false, "Safe external resource did NOT load successfully.");
			// ReSharper disable once HeuristicUnreachableCode
			GetTree().Quit(1);
			return;
		}

		if (!TestFiles("./test/cases/bad", false))
		{
			Debug.Assert(false, "Unsafe external resources were loaded when they should NOT have.");
			// ReSharper disable once HeuristicUnreachableCode
			GetTree().Quit(2);
			return;
		}
		
		GD.Print("All tests pass!");
		GetTree().Quit();
	}

	private bool TestFiles(string dir, bool shouldBeLoaded)
	{
		foreach (var file in Directory.GetFiles(dir))
		{
			var fullPath = Path.GetFullPath(file);
			GD.Print($"Testing to see if {fullPath} should {(shouldBeLoaded ? "" : "not ")}load");
			var res = ResourceLoader.Load(fullPath);
			// A resource instance will return regardless, so when doing bad tests, check signal callback data
			var isLoaded = shouldBeLoaded ? res != null : _lastExtSuccessful;
			if (isLoaded ^ shouldBeLoaded)
			{
				return false;
			}
		}
		return true;
	}
}
