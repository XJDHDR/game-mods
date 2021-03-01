; Recommended settings for performance
#NoEnv
#SingleInstance force
ListLines, Off


If (%0% == 0)
{
	PickFile:
	FileSelectFile, sBsaPaths, M3, , Please select the XnGine BSA you would like to extract, XnGine BSAs (*.bsa)
	
	If (ErrorLevel > 0)
	{
		ExitApp,
	}
	Else If (sBsaPaths == "")
	{
		MsgBox, 4, No BSAs selected, You did not select any BSAs. Either click Yes to retry or no to exit.
		IfMsgBox, Yes
		{
			Goto, PickFile
		}
		Else
		{
			ExitApp,
		}
	}
	
	Loop, Parse, sBsaPaths, `n
	{
		If (A_Index == 1)
			sFolderPath := A_LoopField
		Else
		{
			bExtractionSuccess := ExtractBSA(A_ScriptDir, sFolderPath . "\" . A_LoopField, True)
			If (bExtractionSuccess == False)
			{
				bErrorsOccurred := True
			}
		}
	}
}
Else
{
	iNumberOfLoops = %0%
	While(iCurrentLoop < iNumberOfLoops)
	{
		iCurrentLoop += 1
		
		; %% is deliberate. iCurrentLoop is equal to a number. When using %%, the contents of the variable named after that number 
		; (i.e. the current command line argument) are stored in sBsaPath.
		sBsaPath := %iCurrentLoop%

		bExtractionSuccess := ExtractBSA(A_ScriptDir, sBsaPath, False)
		If (bExtractionSuccess == False)
		{
			bErrorsOccurred := True
		}
	}
}

If (bErrorsOccurred == True)
{
	MsgBox, Every BSA has been extracted but errors occurred on some of them.
}
Else
{
	MsgBox, Every BSA has been extracted successfully.
}

ExitApp,


ExtractBSA(sPassedScriptDir, sPassedBsaPath, bPassedShowProgress)
{
	SplitPath, sPassedBsaPath, sBsaName, , , ,

	; Create first pointer for reading BSA's header and footer data.
	objBsaRecordIndexReader := FileOpen(sPassedBsaPath, "r")
	objBsaRecordIndexReader.Seek(0 , 0)							; Origin (2nd parameter): 0 = file start, 1 = current pointer position, 2 = file end.

	; Read data from the BSA's header.
	iBsaRecordCount := objBsaRecordIndexReader.ReadUShort()		; BsaRecordCount is a 16-bit unsigned integer.
	iBsaType := objBsaRecordIndexReader.ReadUShort()			; BsaType is a 16-bit unsigned integer.
	If (iBsaType == 256)		; NameRecord BSA
	{
		iStartOfFooter := 0 - ( iBsaRecordCount * 18 )			; Get number of bytes to move backwards from end of file. Each RecordIndex is 18 bytes long.
	}
	Else If (iBsaType == 512)	; NumberRecord BSA
	{
		iStartOfFooter := 0 - ( iBsaRecordCount * 8 )			; Get number of bytes to move backwards from end of file. Each RecordIndex is 8 bytes long.
	}
	Else
	{
		MsgBox, Fatal error encountered. The 3rd and 4th bytes of the BSA are supposed to contain the BSA's type. This is supposed to be equal to 256 or 512. %iBsaType% was read instead. Aborting remaining extraction of this BSA.
		Return, False
	}

	objBsaRecordIndexReader.Seek(iStartOfFooter , 2)			; Move Record Index Reader's pointer to the start of the footer.

	; Create second pointer for retrieving Record data itself. Then move it to after the header.
	objBsaRecordDataRetriever := FileOpen(sPassedBsaPath, "r")
	objBsaRecordDataRetriever.Seek(4 , 0)

	If Not InStr(FileExist(sPassedScriptDir . "\Extracted files\" . sBsaName), "D")
	{
		FileCreateDir, %sPassedScriptDir%\Extracted files\%sBsaName%
	}

	If (bPassedShowProgress == True)
	{
		fExtractionProgress := 0
		Static CurrentExtractionText
		Static ProgressBar
		Gui, New, +MinSize300x90 -MaximizeBox -Resize				, BSA Extraction progress
		Gui, Add, Text		, 										, Extraction progress for %sBsaName%`n
		Gui, Add, Progress	, w300 h24 vProgressBar +Center -Smooth	, 0
		Gui, Add, Text		, w300 vCurrentExtractionText +Center	, %Space%
		Gui, Show
	}
	While (iCurrentBsaRecord < iBsaRecordCount)
	{
		If (bPassedShowProgress == True)
		fExtractionProgress := iCurrentBsaRecord / iBsaRecordCount * 100
		iCurrentBsaRecord 	+= 1
		
		If (iBsaType == 256)		; NameRecord BSA
		{
			sRecordNameOrNumber	:= objBsaRecordIndexReader.Read(12)				; First 12 bytes of RecordIndex is Record's Name.
		}
		Else If (iBsaType == 512)	; NumberRecord BSA
		{
			sRecordNameOrNumber := objBsaRecordIndexReader.ReadUShort()			; First 2 bytes of RecordIndex is Record's ID Number.
		}
		
		If (bPassedShowProgress == True)
		{
			GuiControl, , ProgressBar, %fExtractionProgress%
			GuiControl, Text, CurrentExtractionText, Extracting %sRecordNameOrNumber%
		}
		
		bIsRecordCompressed := objBsaRecordIndexReader.ReadShort()					; Signed 16-bit integer. 256 = uncompressed, 512 = compressed.
		If ((bIsRecordCompressed != 256) && (bIsRecordCompressed != 0))
		{
			MsgBox, Fatal error encountered. The 2nd parameter of each record in a BSA's footer is supposed to indicate if a record is compressed. This is supposed to be equal to 256 or 0. %bIsRecordCompressed% was read instead. Aborting remaining extraction of this BSA.
			If (bPassedShowProgress == True)
			{
				Gui, Destroy
			}
			Return, False
		}
		
		iRecordSize 		:= objBsaRecordIndexReader.ReadInt()					; Size of Record itself. No idea why signed 32-bit integer was used. Records can't have a negative size and unsigned would have allowed for double the Record size.
		objBsaRecordDataRetriever.RawRead(binRecordData, iRecordSize)				; Read iRecordSize bytes from the Record data.
		
		If (bIsRecordCompressed != 0)
		{
			objRecordToWriteOut := FileOpen(sPassedScriptDir . "\Extracted files\" . sBsaName . "\Record " . iCurrentBsaRecord . " - (Compressed) " . sRecordNameOrNumber, "w")
			objRecordToWriteOut.RawWrite(binRecordData, iRecordSize)
			objRecordToWriteOut.Close()
		}
		Else
		{
			objRecordToWriteOut := FileOpen(sPassedScriptDir . "\Extracted files\" . sBsaName . "\Record " . iCurrentBsaRecord . " - " . sRecordNameOrNumber, "w")
			objRecordToWriteOut.RawWrite(binRecordData, iRecordSize)
			objRecordToWriteOut.Close()
		}
	}

	objBsaRecordIndexReader.Close()
	objBsaRecordDataRetriever.Close()
	
	If (bPassedShowProgress == True)
	{
		Gui, Destroy
	}
	Return, True
}
