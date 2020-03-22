Imports System.Threading
Imports System.Text
Imports System.IO
Imports System.Xml
Imports System.Globalization

Module modGlobal

#Region "Variables"
    Dim SDK = CreateObject("RideRunner.SDK")
    Public PluginSettings As cMySettings
    Public TempPluginSettings As cMySettings
    Public MainPath As String
    Public SkinPath As String
    Public Language As String
    Public sArray() As String, dt() As String
    Public DebugLog As StreamWriter
    Public KmlFileLoaded As Boolean = False
    Public LastScreenLoaded As String = ""
    Public PopUpMenuIsOn As Boolean = False
    Public trd As Thread
    Public spliChar As Char()

    'RRpoi Variables
    Public CE_plagin_status As Boolean = False
    Public CE_plagin_e As String = ""
    Public log_file As String '= "c:\rrpoi.log"


    Public poi_file As New XmlDocument
    Public new_poi_kml_file As String = "<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?> <kml xmlns=""http://earth.google.com/kml/2.0""> </kml>"
    Public Is_SaveXML_run As Boolean = False
    Public Poi_Open As Thread
    Public points_list As New System.Collections.ArrayList()
    Public en As New CultureInfo("en-US")

    ' list is a reference type
    'public static IdList id_list = new IdList();
    'public static IdList width_list = new IdList();

    Public isStop As Boolean = False
    Public iStoped As Boolean = False
    Public isPause As Boolean = False
    Public PoiFileName As String
    Public locked As New Object()
    'Public en As New CultureInfo("en-US")
    Public Created As Boolean = False
    Public point_idWidth_list As System.Collections.ArrayList

    'Ov2
    Public Ov2Path As String '
    Public poiList As List(Of POI)
#End Region

#Region "Unicode conversion"
    'lister les dossiers d'un dossier (sous dossiers compris)
    Public Sub ListeDirectoriesIntoDirectory(ByVal MyDirectory As String, ByVal Icons As Boolean)
        Try
            Dim monStreamWriter As New StreamWriter(MyDirectory & ".txt", True, Encoding.Unicode)
            monStreamWriter.WriteLine(" 0")
            For Each ligneF In Directory.GetDirectories(MyDirectory, "*.*", SearchOption.AllDirectories)
                monStreamWriter.WriteLine("LST" & Path.GetFileName(ligneF) & "||" & Path.GetFileName(ligneF))
                If Icons = True Then monStreamWriter.WriteLine("ICO$PLUGINSPATH$RRPoi\Languages\" & Path.GetFileName(ligneF) & "\" & Path.GetFileName(ligneF) & ".gif")
            Next
            monStreamWriter.Close()
        Catch ex As Exception
            'TextBox1.Text = ex.Message
        End Try
    End Sub

    Public Function CheckIfTextIsUnicode(ByVal filepath As String) As String
        CheckIfTextIsUnicode = ""
        Dim enc As System.Text.Encoding = Nothing
        Dim file As System.IO.FileStream = New System.IO.FileStream(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
        If file.CanSeek Then
            Dim bom As Byte() = New Byte(3) {} ' Get the byte-order mark, if there is one
            file.Read(bom, 0, 4)

            If bom(0) = &HFF Then
                CheckIfTextIsUnicode = "Unicode"
            ElseIf bom(0) = &HEF Then
                CheckIfTextIsUnicode = "Utf8"
            Else
                CheckIfTextIsUnicode = "Ansi"
            End If
            ' Now reposition the file cursor back to the start of the file
            file.Seek(0, System.IO.SeekOrigin.Begin)
        Else
            enc = System.Text.Encoding.ASCII
        End If

        Return CheckIfTextIsUnicode
    End Function

    Public Sub EncodeInUnicode(ByVal Path As String, ByVal Text As String)
        ' read with the **local** system default ANSI page
        Text = File.ReadAllText(Path, Encoding.Default)
        ' ** I'm not sure you need to do this next bit - it sounds like
        '  you just want to read it? **
        ' write as Unicode (if you want to do this)
        File.WriteAllText(Path, Text, Encoding.Unicode)

    End Sub

    Public Sub TextToUnicode(ByVal filepath As String)
        'Dim encoding As System.Text.Encoding = System.Text.Encoding.BigEndianUnicode
        Dim encoding As System.Text.Encoding = System.Text.Encoding.Unicode
        Dim reader As New StreamReader(filepath, encoding)
        Dim line As String = reader.ReadLine()
        While Not (line Is Nothing)
            Console.WriteLine(line)
            line = reader.ReadLine()
        End While
    End Sub
#End Region

    Public Sub LoadVarsFromFile(ByVal DuinoFileName As String) 'lecture
        If File.Exists(DuinoFileName) Then
            Dim array As String()
            array = File.ReadAllLines(DuinoFileName)
            Dim line As String
            For Each line In array
                If Left(line, 1) <> "[" And Trim(line) <> "" Then
                    dt = Split(line, "=")
                    SDK.SetUserVar(dt(0), dt(1))
                End If
            Next
        Else
            'SDK.SetUserVar("DUINO_ERROR", "File " & DuinoFileName & " not found")
        End If

    End Sub

    Public Function SaveVarToFile(ByVal sFile As String, ByVal sFilter As String, ByVal replaceBy As String) As Boolean
        If File.Exists(sFile) Then
            Dim allLines = File.ReadAllLines(sFile)
            Dim found As Integer = -1
            For i As Integer = 0 To allLines.Length - 1
                If allLines(i).Contains(sFilter) Then
                    found = i
                    Exit For
                End If
            Next
            allLines(found) = replaceBy
            File.WriteAllLines(sFile, allLines)
        Else
            'SDK.SetUserVar("DUINO_ERROR", "File " & sFile & " not found")
        End If
    End Function


End Module
