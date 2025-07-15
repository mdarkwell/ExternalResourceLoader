using System;
using System.Buffers;
using System.IO;

namespace ExternalResourceLoader.addons.ExternalResourceLoader.Utilities;

internal static class IOExtensions
{
	public static byte[] ReadAllBytesSafely(string path)
	{
		using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1,
			FileOptions.SequentialScan);
		long fileLength = 0;
		if (fs.CanSeek && (fileLength = fs.Length) > int.MaxValue)
		{
			throw new IOException($"File too large (more than {int.MaxValue:n0} bytes)");
		}

		if (fileLength == 0)
		{
			return ReadAllBytesUnknownLength(fs);
		}

		var index = 0;
		var count = (int)fileLength;
		var bytes = new byte[count];
		while (count > 0)
		{
			var n = fs.Read(bytes, index, count);
			if (n == 0)
			{
				throw new EndOfStreamException();
			}

			index += n;
			count -= n;
		}

		return bytes;
	}
	
	private static byte[] ReadAllBytesUnknownLength(FileStream fs)
	{
		byte[] rentedArray = null;
		Span<byte> buffer = stackalloc byte[512];
		try
		{
			var bytesRead = 0;
			while (true)
			{
				if (bytesRead == buffer.Length)
				{
					var newLength = (uint)buffer.Length * 2;
					if (newLength > Array.MaxLength)
					{
						newLength = (uint)Math.Max(Array.MaxLength, buffer.Length + 1);
					}

					var tmp = ArrayPool<byte>.Shared.Rent((int)newLength);
					buffer.CopyTo(tmp);
					var oldRentedArray = rentedArray;
					buffer = rentedArray = tmp;
					if (oldRentedArray != null)
					{
						ArrayPool<byte>.Shared.Return(oldRentedArray);
					}
				}

				var n = fs.Read(buffer[bytesRead..]);
				if (n == 0)
				{
					return buffer[..bytesRead].ToArray();
				}

				bytesRead += n;
			}
		}
		finally
		{
			if (rentedArray != null)
			{
				ArrayPool<byte>.Shared.Return(rentedArray);
			}
		}
	}
}