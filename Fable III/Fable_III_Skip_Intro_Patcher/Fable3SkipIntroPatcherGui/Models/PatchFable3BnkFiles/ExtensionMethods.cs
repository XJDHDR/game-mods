// This file is or was originally a part of the Fable III Skip Intro Patcher project, which can be found here: https://github.com/XJDHDR/game-mods/blob/master/Fable%20III/Fable_III_Skip_Intro_Patcher/License.txt
//
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// This Source Code Form is "Incompatible With Secondary Licenses", as
// defined by the Mozilla Public License, v. 2.0.
//
//  List of this Source Code Form's contributors:
//  - Xavier "XJDHDR" du Hecquet de Rauville
//


using System;
using System.IO;

namespace Fable3SkipIntroPatcherGui.Models.PatchFable3BnkFiles;

public static class ExtensionMethods
{
	extension(Stream Stream)
	{
		public int ReadBigEndianInt32()
		{
			Span<byte> uintBytes = stackalloc byte[sizeof(int)];
			readBytesFromBinaryReaderInBigEndianOrder(Stream, ref uintBytes);

			return BitConverter.ToInt32(uintBytes);
		}

		public uint ReadBigEndianUInt32()
		{
			Span<byte> uintBytes = stackalloc byte[sizeof(uint)];
			readBytesFromBinaryReaderInBigEndianOrder(Stream, ref uintBytes);

			return BitConverter.ToUInt32(uintBytes);
		}

		public void WriteBigEndianInt32(int Value)
		{
			Span<byte> intBytes = stackalloc byte[sizeof(int)];
			BitConverter.TryWriteBytes(intBytes, Value);

			writeBytesToStreamInBigEndianOrder(Stream, ref intBytes);
		}

		public void WriteBigEndianUInt32(uint Value)
		{
			Span<byte> uintBytes = stackalloc byte[sizeof(uint)];
			BitConverter.TryWriteBytes(uintBytes, Value);

			writeBytesToStreamInBigEndianOrder(Stream, ref uintBytes);
		}
	}

	private static void readBytesFromBinaryReaderInBigEndianOrder(Stream DataStream, ref Span<byte> ReadBytes)
	{
		int bytesRead = DataStream.Read(ReadBytes);

		if (bytesRead < ReadBytes.Length)
		{
			throw new EndOfStreamException();
		}

		if (BitConverter.IsLittleEndian)
		{
			ReadBytes.Reverse();
		}
	}

	private static void writeBytesToStreamInBigEndianOrder(Stream DataStream, ref Span<byte> Bytes)
	{
		if (BitConverter.IsLittleEndian)
		{
			Bytes.Reverse();
		}

		DataStream.Write(Bytes);
	}
}
