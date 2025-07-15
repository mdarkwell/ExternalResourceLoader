using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ExternalResourceLoader.addons.ExternalResourceLoader.Utilities;
using Godot;

namespace ExternalResourceLoader.addons.ExternalResourceLoader.Core;

public partial class ExternalResourceFormatLoader : ResourceFormatLoader
{
	private static readonly byte[] BinaryHeader =
	{
		0x52, 0x53, 0x43, 0x43, 0x02, 0x00
	};

	private static readonly byte[] ScriptSignature =
	{
		0x01, 0x0, 0x0, 0x28, 0xb5, 0x2f, 0xfd, 0x60
	};

	private static readonly Regex SectionPattern = new(@"\[(?'section'[a-zA-Z0-9\-_]+)(?'params'.*?)\]",
			RegexOptions.Compiled | RegexOptions.Multiline),
		TagPattern = new(@"(?'key'[a-zA-Z0-9\-_]+)\=(?'value'[\""""].+?[\""""]|[^ ]+)",
			RegexOptions.Compiled | RegexOptions.Multiline);

	private static readonly HashSet<string> ResourceTypeBlacklist = new()
	{
		"script", "gdscript", "csharpscript"
	};

	private static readonly HashSet<string> RecognizedExtensions = new()
	{
		"tres", "res", "scn", "tscn"
	};

	/// <summary>
	/// Returns true if the file has no embedded code. Otherwise, it returns false.
	/// </summary>
	private static bool ValidateFile(string path, ref byte[] buffer)
	{
		var bin = ValidateBinaryFile(path, ref buffer);
		return bin switch
		{
			Error.Unauthorized => false,
			Error.Ok => true,
			
			// File is not binary file; scan for embedded code in .tres file
			_ => ValidateTextFile(path, ref buffer) == Error.Ok
		};
	}

	private static Error ValidateTextFile(string path, ref byte[] buffer)
	{
		try
		{
			var text = Encoding.UTF8.GetString(buffer);
			
			foreach (Match match in SectionPattern.Matches(text))
			{
				var section = match.Groups["section"].Value;
				if (!section.Contains("resource", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				// Validate parameters in section params
				var @params = new Dictionary<string, string>();
				foreach (Match param in TagPattern.Matches(match.Groups["params"].Value))
				{
					var key = param.Groups["key"].Value.ToLower();
					var value = param.Groups["value"].Value.Trim('\"', ' ', '\t');
					@params.Add(key, value);
				}

				// Validate ext_resource doesn't point to script outside PCK
				if (@params.TryGetValue("path", out var paramPath) &&
				    !paramPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
				{
					return Error.Unauthorized;
				}

				// Validate sub_resource doesn't have an invalid type
				if (string.IsNullOrEmpty(paramPath) && @params.TryGetValue("type", out var paramType) &&
				    ResourceTypeBlacklist.Contains(paramType.ToLower()))
				{
					return Error.Unauthorized;
				}
			}

			// Any embedded source code should be flagged. This might be redundant.
			if (text.Contains("script/source", StringComparison.OrdinalIgnoreCase))
			{
				return Error.Unauthorized;
			}

			return Error.Ok;
		}
		catch (Exception ex)
		{
			GD.PushError(new object[] { $"Received error while validating resource file \"{path}\":\r\n{ex}" });
		}

		return Error.Unauthorized;
	}
	
	private static Error ValidateBinaryFile(string path, ref byte[] buffer)
	{
		try
		{
			// File too small, can't be binary file
			if (buffer.Length < BinaryHeader.Length)
			{
				return Error.FileUnrecognized;
			}
            
			// Validate header matches signature, otherwise file is not binary resource
			for (var i = 0; i < BinaryHeader.Length; i++)
			{
				if (buffer[i] != BinaryHeader[i])
				{
					return Error.FileUnrecognized;
				}
			}
			
			// if script signature is found in byte array, return error
			var script = Search(buffer, ScriptSignature);
			return script > -1 ? Error.Unauthorized : Error.Ok;
		}
		catch (Exception ex)
		{
			GD.PushError(new object[] { $"Received error while validating binary file \"{path}\":\r\n{ex}" });
			return Error.Unauthorized;
		}
	}
	
	public override Variant _Load(string path, string originalPath, bool useSubThreads, int cacheMode)
	{
		var buffer = IOExtensions.ReadAllBytesSafely(path);
		if (!ValidateFile(path, ref buffer))
		{
			// Return new resource instance if validation failed; this should only happen
			// if the file had code signatures detected within the resource.
			ExternalResourceManager.Instance?.EmitSignal(ExternalResourceManager.SignalName.ExternalResourceLoaded, path, false);
			return new Resource();
		}

		// Otherwise, load file as normal.
		var res = base._Load(path, originalPath, useSubThreads, cacheMode);
		if (!path.StartsWith("res://"))
		{
			ExternalResourceManager.Instance?.EmitSignal(ExternalResourceManager.SignalName.ExternalResourceLoaded, path, true);
		}
		return res;
	}

	public override bool _RecognizePath(string path, StringName type)
	{
		var ext = path.GetExtension().ToLower();
		// Only process resources outside the PCK
		return RecognizedExtensions.Contains(ext) && !path.StartsWith("res://");
	}

	public override bool _HandlesType(StringName type)
	{
		// TODO: Move to configurable type collection in project settings
		return type == "Resource" || type == "GDScript" || type == "CSharpScript" || type == "Script" ||
		       type == "Scene" || type == "PackedScene";
	}
	
	private static int Search(byte[] haystack, byte[] needle)
	{
		var len = needle.Length;
		var limit = haystack.Length - len;
		for (var i = 0; i <= limit; i++)
		{
			var k = 0;
			for (; k < len; k++)
			{
				if (needle[k] != haystack[i + k]) break;
			}

			if (k == len) return i;
		}

		return -1;
	}
}