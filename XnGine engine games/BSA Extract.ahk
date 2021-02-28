


FileSelectFile, sBsaPath, 3, , Please select the XnGine BSA you would like to extract, XnGine BSAs (*.bsa)
SplitPath, sBsaPath, sBsaName, sBsaFolder, , ,

MsgBox, 35, Do safety checks?, 
(
Do you want to perform all safety checks present on read BSA data?

It is recommended that you select "Yes" unless you know what you are doing.

"Cancel" will cancel BSA extracton.

Note that error checks in the BSA's header will not be disabled by clicking "No".
),

IfMsgBox, Cancel
{
	ExitApp,
}
Else IfMsgBox, No
{
	bDisableMostSafetyChecks := true
}
else
{
	bDisableMostSafetyChecks := false
}

; Create first pointer for reading BSA's header and footer data.
objBsaRecordIndexReader := FileOpen(sBsaPath, "r")
objBsaRecordIndexReader.Seek(0 , 0)							; Origin (2nd parameter): 0 = file start, 1 = current pointer position, 2 = file end.

; Read data from the BSA's header.
iBsaRecordCount := objBsaRecordIndexReader.ReadUShort()		; BsaRecordCount is a 16-bit unsigned integer.
iBsaType := objBsaRecordIndexReader.ReadUShort()			; BsaType is a 16-bit unsigned integer.
if (iBsaType == 256)		; NameRecord BSA
{
	iStartOfFooter := 0 - ( iBsaRecordCount * 18 )			; Get number of bytes to move backwards from end of file. Each RecordIndex is 18 bytes long.
}
else if (iBsaType == 512)	; NumberRecord BSA
{
	iStartOfFooter := 0 - ( iBsaRecordCount * 8 )			; Get number of bytes to move backwards from end of file. Each RecordIndex is 8 bytes long.
}
else
{
	MsgBox, Fatal error encountered. The 3rd and 4th bytes of the BSA are supposed to contain the BSA's type. This is supposed to be equal to 256 or 512. %iBsaType% was read instead. This program will now exit.
	ExitApp,
}

objBsaRecordIndexReader.Seek(iStartOfFooter , 2)			; Move Record Index Reader's pointer to the start of the footer.

; Create second pointer for retrieving Record data itself. Then move it to after the header.
objBsaRecordDataRetriever := FileOpen(sBsaPath, "r")
objBsaRecordDataRetriever.Seek(4 , 0)

If Not InStr(FileExist(A_ScriptDir . "\Extracted files"), "D")
{
	FileCreateDir, %A_ScriptDir%\Extracted files
}

;while (iCurrentBsaRecord < iBsaRecordCount)
;{
	iCurrentBsaRecord 	+= 1
	
	if (iBsaType == 256)		; NameRecord BSA
	{
		sRecordNameOrNumber	:= objBsaRecordIndexReader.Read(12)				; First 12 bytes of RecordIndex is Record's Name.
	}
	else if (iBsaType == 512)	; NumberRecord BSA
	{
		sRecordNameOrNumber := objBsaRecordIndexReader.ReadUShort()			; First 2 bytes of RecordIndex is Record's ID Number.
	}
	
	bIsRecordCompressed := objBsaRecordIndexReader.ReadShort()					; Boolean despite being stored as a signed 16-bit integer. 0 = uncompressed, 1 = compressed.
	if ((bDisableMostSafetyChecks == false) && (bIsRecordCompressed != 1) && (bIsRecordCompressed != 0))
	{
		MsgBox, Fatal error encountered. The 2nd parameter of each BSA footer's records are supposed to indicate if a record is compressed. This is supposed to be equal to 1 or 0. %bIsRecordCompressed% was read instead. This program will now exit.
		ExitApp,
	}
	
	iRecordSize 		:= objBsaRecordIndexReader.ReadInt()					; Size of Record itself. No idea why signed 32-bit integer was used. Records can't have a negative size and unsigned would have allowed for double the Record size.
	objBsaRecordDataRetriever.RawRead(binRecordData, iRecordSize)				; Read iRecordSize bytes from the Record data.
	
	if (bIsRecordCompressed != 0)
	{
		objRecordToWriteOut := FileOpen(A_ScriptDir . "\Extracted files\" . sBsaName . " - Record " . iCurrentBsaRecord . " - (Compressed) " . sRecordNameOrNumber, "w")
		objRecordToWriteOut.RawWrite(binRecordData, iRecordSize)
		objRecordToWriteOut.Close()
	}
	else
	{
		objRecordToWriteOut := FileOpen(A_ScriptDir . "\Extracted files\" . sBsaName . " - Record " . iCurrentBsaRecord . " - " . sRecordNameOrNumber, "w")
		objRecordToWriteOut.RawWrite(binRecordData, iRecordSize)
		objRecordToWriteOut.Close()
	}
;}
Pause

objBsaRecordIndexReader.Close()
objBsaRecordDataRetriever.Close()

ExitApp,
