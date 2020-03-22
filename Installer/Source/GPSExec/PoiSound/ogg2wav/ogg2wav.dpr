program ogg2wav;

uses KOL, Windows, Messages, VorbisFile, Codec;

var
 Form, Panel, EditBox, btnOpen, btnOGG2WAV, PB : PControl;
 OD : POpenSaveDialog;

function ogg_to_wav(fs, fd : KOLString) : boolean;
const
 WAVE_FORMAT_PCM = 1;
 BufferSize = 4096;
type
 ogg_int64_t  = int64;
 TWaveHeader = record
   // RIFF file header
   RIFF: array [0..3] of Char;          // = 'RIFF' offset : 0000
   FileSize: Integer;                   // = FileSize - 8 offset : 0004
   RIFFType: array [0..3] of Char;      // = 'WAVE'  offset : 0008
   // Format chunk
   FmtChunkId: array [0..3] of Char;    // = 'fmt'   offset : 0012
   FmtChunkSize: Integer;               // = 16      offset : 0016
   FormatTag: Word;                     // One of WAVE_FORMAT_XXX constants    offset : 0020
   Channels: Word;                      // = 1 - mono = 2 - stereo             offset : 0022
   SampleRate: Integer;                                                     // offset : 0024
   BytesPerSecond: Integer;                                                 // offset : 0028
   BlockAlign: Word;                                                        // offset : 0032
   BitsPerSample: Word;                 // = 8, 16 or 32 Bits/sample           offset : 0034
   // Data Chunk
   DataChunkId: array [0..3] of Char;   // = 'data'                          // offset : 0036
   DataSize: Integer;   // Data size in bytes                                // offset : 0040
 end;

var
 WH : TWaveHeader;
 InFile, OutFile: PStream;
 VF: OggVorbis_File;
 Return : integer;
 BytesRead: Longword;
 Change: integer;           
 Buffer: array[0..BufferSize-1] of Byte;

 Size : DWORD;
 channels,
 samplerate,
 bits,
 link,
 chainsallowed : integer;   
 knownlength : ogg_int64_t;
begin
 Result := false;
 InFile := NewReadFileStream(fs);
 if InFile = nil then exit;
 Return := ov_open_callbacks(InFile, VF, nil, 0, ops_callbacks);
 if Return < 0 then
  begin
   Free_And_Nil(InFile);
   exit;
  end;
 OutFile := NewReadWriteFileStream(fd);
 if OutFile = nil then
  begin
   ov_clear(VF);
   Free_And_Nil(InFile);
   exit;
  end;

 knownlength := 0;
 bits := 16;
 size := $7fffffff;
 channels := ov_info(vf, 0).channels;
 samplerate := ov_info(vf, 0).rate;
 if ov_seekable(vf) <> 0 then
  begin
   chainsallowed := 0;
   for link := 0 to ov_streams(vf)-1 do
    begin
     if (ov_info(vf, link).channels = channels) and
        (ov_info(vf, link).rate = samplerate) then
      chainsallowed := 1;
    end;
   if chainsallowed = 0 then knownlength := ov_pcm_total(vf, -1)
    else knownlength := ov_pcm_total(vf, 0);
  end;
 if knownlength and knownlength * bits div 8 * channels < size then
  size := knownlength * bits div 8 * channels;

 WH.RIFF := 'RIFF';
 WH.FileSize := Size + 36;
 WH.RIFFType := 'WAVE';
 WH.FmtChunkId := 'fmt'#$20;
 WH.FmtChunkSize := 16;
 WH.FormatTag := WAVE_FORMAT_PCM;
 WH.Channels := Channels;
 WH.SampleRate := samplerate;
 WH.BytesPerSecond := Trunc(samplerate * channels * (bits / 8));
 WH.BlockAlign := Trunc(channels * (bits / 8));
 WH.BitsPerSample := bits;
 WH.DataChunkId := 'data';
 WH.DataSize := Size;
 OutFile.Write(WH, SizeOf(WH));

 repeat
  BytesRead := 0;
  repeat
   Change := ov_read(VF, Buffer[BytesRead], BufferSize - BytesRead, 0, 2, 1, nil);
   if Change >= 0 then BytesRead := BytesRead + Longword(Change);
  until (Change = 0) or (BytesRead = BufferSize);

  OutFile.Write(Buffer[0], BytesRead);
  PB.Progress := Round((OutFile.Position + BytesRead) / Size * 100);
 until BytesRead = 0;
 
 Free_And_Nil(OutFile);
 Free_And_Nil(InFile);
 ov_clear(VF);
 Result := true;
end;

(*===================== Using KOL (http://www.kolmck.net) ====================*)
procedure btnOpen_OnClick(Dummy : Pointer; Sender: PObj );
begin
 OD := NewOpenSaveDialog('', '', [OSFileMustExist,
                                  OSHideReadonly,
                                  OSOverwritePrompt,
                                  OSPathMustExist]);
 OD.Filter := 'All OGG files (*.ogg)|*.ogg';
 if OD.Execute then
  if OD.Filename <> '' then
   begin
    EditBox.Text := OD.Filename;
    btnOGG2WAV.Enabled := true;
   end;
 Free_And_Nil(OD);
end;

procedure btnFLAC2WAV_OnClick(Dummy : Pointer; Sender: PObj );
begin
 btnOGG2WAV.Enabled := false;
 if not ogg_to_wav(EditBox.Text, ChangeFileExt(EditBox.Text, '.wav')) then
  beep(100, 100)
   else PB.Progress := 0;
end;

begin
 Form := NewForm(Applet, 'OGG2WAV converter');
 Form.SetSize(350, 100);
 Form.Font.FontHeight := 13;
 Form.CenterOnParent;

 btnOGG2WAV := NewButton(Form, 'OGG -> WAV');
 btnOGG2WAV.Align := caBottom;
 btnOGG2WAV.Enabled := false;
 btnOGG2WAV.OnClick := TOnEvent(MakeMethod(nil, @btnFLAC2WAV_OnClick));

 PB := NewProgressBar(Form);
 PB.Align := caBottom;

 Panel := NewPanel(Form, esNone);
 Panel.Align := caClient;

 btnOpen := NewButton(Panel, 'Open');
 btnOpen.Width := 27;
 btnOpen.Align := caRight;
 btnOpen.Text := '...';
 btnOpen.OnClick := TOnEvent(MakeMethod(nil, @btnOpen_OnClick));

 EditBox := NewEditBox(Panel, []);
 EditBox.Align := caClient;

 Run(Form);
end.
(*===================== Using KOL (http://www.kolmck.net) ====================*)
