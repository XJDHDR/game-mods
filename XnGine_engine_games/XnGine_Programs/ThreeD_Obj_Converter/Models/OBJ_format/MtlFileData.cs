// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.Enums;

namespace ThreeD_Obj_Converter.Models.OBJ_format
{
	internal readonly partial struct MtlFileData
	{
		/// <summary> The header's comments, including newlines and the starting # for each line. </summary>
		internal readonly string HeaderComments;

		internal readonly string[] InlineCommentStrings;
		internal readonly int[] InlineCommentStartIndex;

		internal readonly MaterialData[] AllMaterials;


		// ==== Constructors ====
		internal MtlFileData(Stream MtlDataStream)
		{
			HeaderComments = string.Empty;
			InlineCommentStrings = Array.Empty<string>();
			InlineCommentStartIndex = Array.Empty<int>();
			AllMaterials = Array.Empty<MaterialData>();

			List<string> inlineCommentStrings = new();
			List<int> inlineCommentStartIndex = new();
			List<MaterialData> allMaterialsList = new();

			int lineNumber = 0;
			StringBuilder commonStringsBuilder = new();
			StringBuilder messageStringBuilder = new();

			using (StreamReader mtlDataStreamReader = new(MtlDataStream))
			{
				string? readString;

				// Read the comments in the header.
				for (int i = 0; i < int.MaxValue; ++i)
				{
					readString = mtlDataStreamReader.ReadLine()?.Trim();
					++lineNumber;

					// Check if the end of the stream has been reached.
					if (readString == null)
						break;

					// Check if this is an empty line.
					if (string.IsNullOrWhiteSpace(readString))
						continue;

					// Check if this is a comment
					if (readString.StartsWith("#"))
					{
						// This line is a comment
						commonStringsBuilder.Append(readString);
						continue;
					}

					// Since this line is not a comment, move on to reading the other file data.
					break;
				}

				if (commonStringsBuilder.Length > 0)
				{
					HeaderComments = commonStringsBuilder.ToString();
					commonStringsBuilder.Clear();
				}

				// Skip past all lines between the header comments and first material definition.
				while (true)
				{
					readString = mtlDataStreamReader.ReadLine()?.Trim();
					++lineNumber;

					// Check if the end of the stream has been reached.
					if (readString == null)
						break;

					// Check if this is an empty line.
					if (string.IsNullOrWhiteSpace(readString))
						continue;

					// If this line is not a comment, empty, or a material definition, note the presence of it and skip to the next line.
					if (!readString.StartsWith("newmtl ", StringComparison.OrdinalIgnoreCase))
					{
						messageStringBuilder.AppendLine($"Line {lineNumber} contains data that is not part of a material and will be skipped:");
						messageStringBuilder.AppendLine($"{readString}");
						messageStringBuilder.AppendLine();
						continue;
					}

					// Otherwise, since a material definition has been found, break out of loop and proceed to material construction loop.
					break;
				}

				// Read all Material definitions and associated data.
				while (true)
				{
					// Ignore null warning here. ReadString will only be null if the end of file is reached, which is already handled.
					allMaterialsList.Add(new MaterialData(
						mtlDataStreamReader, messageStringBuilder, in readString!, ref lineNumber, ref inlineCommentStrings,
						ref inlineCommentStartIndex, out readString, out bool endOfFileReached)
					);

					if (endOfFileReached)
						break;
				}
			}

			if (inlineCommentStrings.Count > 0)
				InlineCommentStrings = inlineCommentStrings.ToArray();

			if (inlineCommentStartIndex.Count > 0)
				InlineCommentStartIndex = inlineCommentStartIndex.ToArray();

			if (allMaterialsList.Count > 0)
				AllMaterials = allMaterialsList.ToArray();

			if (messageStringBuilder.Length > 0)
			{
				IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error in MTL File Data Constructor",
					messageStringBuilder.ToString());
				messageBox.Show();
			}
		}

		internal MtlFileData(string HeaderComments, string[] InlineCommentStrings,
			int[] InlineCommentStartIndex, MaterialData[] AllMaterials)
		{
			this.HeaderComments = HeaderComments;
			this.InlineCommentStrings = InlineCommentStrings;
			this.InlineCommentStartIndex = InlineCommentStartIndex;
			this.AllMaterials = AllMaterials;
		}


		// ==== Internal methods ====
		internal void _Write(Stream OutputStream)
		{
			ReadOnlySpan<byte> crlf = stackalloc byte[] {0x0d, 0x0a};
			CultureInfo invar = CultureInfo.InvariantCulture;
			StringBuilder outputStringBuilder = new();

			int bytesRequiredForCharConversion =  Encoding.UTF8.GetByteCount(HeaderComments);
			Span<byte> intermediateByteSpan = stackalloc byte[bytesRequiredForCharConversion];
			Encoding.UTF8.GetBytes(HeaderComments, intermediateByteSpan);
			OutputStream.Write(intermediateByteSpan);
			OutputStream.Write(crlf);

			for (int i = 0; i < AllMaterials.Length; ++i)
			{
				AllMaterials[i]._Write(OutputStream);
			}
		}
	}
}
