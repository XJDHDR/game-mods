// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.IO;
using System.Linq;

namespace BSA_Extractor_and_Packer.Models.BSA_format
{
	internal readonly struct BsaRecordData
	{
		internal readonly BsaRecord[] AllBsaRecords;

		internal BsaRecordData(BinaryReader RecordDataBinaryReader, in BsaHeader Header, in BsaNameRecordFooter Footer)
		{
			AllBsaRecords = new BsaRecord[Header.RecordCount];

			for (int i = 0; i < Header.RecordCount; ++i)
				AllBsaRecords[i] = new BsaRecord(RecordDataBinaryReader, i, in Header, in Footer);
		}
	}

	internal readonly struct BsaRecord
	{
		internal readonly byte[] UncompressedRecordData;

		internal BsaRecord(BinaryReader RecordDataBinaryReader, int RecordIndex, in BsaHeader Header, in BsaNameRecordFooter Footer)
		{
			bool isRecordDataCompressed = false;
			int recordSize = 0;
			switch (Header._BsaType)
			{
				case BsaHeader.BsaType.NameRecord:
					isRecordDataCompressed = Footer.NameRecords[RecordIndex].IsCompressed;
					recordSize = Footer.NameRecords[RecordIndex].RecordSize;
					break;

				case BsaHeader.BsaType.NumberRecord:
					isRecordDataCompressed = Footer.NumberRecords[RecordIndex].IsCompressed;
					recordSize = Footer.NumberRecords[RecordIndex].RecordSize;
					break;
			}

			switch (isRecordDataCompressed)
			{
				case true:
					UncompressedRecordData = decompressRecordData(RecordDataBinaryReader, recordSize);
					break;

				case false:
					UncompressedRecordData = RecordDataBinaryReader.ReadBytes(recordSize);
					break;
			}
		}

		private static byte[] decompressRecordData(BinaryReader RecordDataBinaryReader, int RecordSize)
		{
			byte[] compressedRecordData = RecordDataBinaryReader.ReadBytes(RecordSize);


			return Array.Empty<byte>();
		}

		// This method is called like this:
		//		data = memoryview(fd.read())
		//		filedata = data[offset:offset + size]
		//		filedata = decompress(BytesIO(filedata))
		//
		// def decompress(fd_inner):
		private static void decompress(Stream fd_inner, Stream Output)
		{
			// window = array('B', (b' ' * 4078) + (b'\x00' * 18))		- 'B' = unsigned char
			byte[] window = new byte[4096];
			for (int i = 0; i < 4096; ++i)
			{
				if (i < 4078)
					window[i] = 0x20;

				else
					window[i] = 0x00;
			}

			// pos = 4078
			int pos = 4078;

			// _out = BytesIO()
			using BinaryWriter _out = new(Output);

			// def out_inner(byte):
			void out_inner(byte byteIn)
			{
				// nonlocal window, pos
				// Not needed in C#. Local methods already have access to parent's fields.

				// window[pos] = byte
				window[pos] = byteIn;

				// pos = (pos + 1) & 0xFFF
				pos = (pos + 1) & 0xFFF;

				// _out.write(bytes([byte]))
				_out.Write(byteIn);
			}

			// def bits(byte):
			bool[] bits(byte byteIn)
			{
				// return ((byte >> 0) & 1,
				// 	(byte >> 1) & 1,
				// 	(byte >> 2) & 1,
				// 	(byte >> 3) & 1,
				// 	(byte >> 4) & 1,
				// 	(byte >> 5) & 1,
				// 	(byte >> 6) & 1,
				// 	(byte >> 7) & 1)
				return new[]
				{
					(((byteIn >> 0) & 1) != 0),
					(((byteIn >> 1) & 1) != 0),
					(((byteIn >> 2) & 1) != 0),
					(((byteIn >> 3) & 1) != 0),
					(((byteIn >> 4) & 1) != 0),
					(((byteIn >> 5) & 1) != 0),
					(((byteIn >> 6) & 1) != 0),
					(((byteIn >> 7) & 1) != 0)
				};
			}

			// try:
			try
			{
				// Allocate this span outside the loop
				Span<byte> code = stackalloc byte[2];
				// Required since C# streams don't throw exceptions when end of stream is reached. They just return incorrect values.
				bool mustBreak = false;

				// while True:
				while (true)
				{
					// for encoded in bits(fd_inner.read(1)[0]):
					int byteRead = fd_inner.ReadByte();
					if (byteRead < 0)
						break;
					foreach (bool encoded in bits((byte)byteRead))
					{
						// if encoded:
						if (encoded)
						{
							// out_inner(fd_inner.read(1)[0])
							int byteRead2 = fd_inner.ReadByte();
							if (byteRead2 < 0)
							{
								mustBreak = true;
								break;
							}
							out_inner((byte)byteRead2);
						}

						// else:
						else
						{
							// code = fd_inner.read(2)
							int bytesRead = fd_inner.Read(code);
							if (bytesRead < 2)
							{
								mustBreak = true;
								break;
							}

							// offset_inner = code[0] | (code[1] & 0xF0) << 4
							int offset_inner = code[0] | ((code[1] & 0xF0) << 4);

							// length = (code[1] & 0xF) + 3
							int length = (code[1] & 0xF) + 3;

							// for x in range(offset_inner, offset_inner + length):
							foreach (int x in Enumerable.Range(offset_inner, offset_inner + length))
							{
								// out_inner(window[x & 0xFFF])
								out_inner(window[x & 0xFFF]);
							}
						}
					}

					if (mustBreak)
						break;
				}
			}
			// except IndexError:
			catch (IndexOutOfRangeException)
			{
				// pass
				;
			}

			// return _out.getbuffer()
			// Not required. Data has already been written to output stream.
		}
	}
}
