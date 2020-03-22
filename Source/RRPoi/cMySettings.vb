Option Strict Off
Option Explicit On

Imports System.IO
Imports System.Xml
Imports System.Xml.Serialization
Imports System.Text

Public Class cMySettings

    Public RRpoiDebug As Boolean
    Public RRPoiDebugRstOnStart As Boolean
    Public RRPoiRunPluginOnStart As Boolean
    Public RRPoiLanguage As String

    Private Shared XMLFilename As String

    Public Sub New()
        SetToDefault(Me)
    End Sub

    Public Sub New(FileName As String)
        XMLFilename = FileName
        If Path.GetExtension(XMLFilename).ToLower() <> "xml" Then XMLFilename = XMLFilename + ".xml"
        If File.Exists(XMLFilename) = False Then SerializeToXML(New cMySettings())
    End Sub

    Public Sub New(ByRef Settings As cMySettings)
        Me.RRPoiDebug = Settings.RRPoiDebug
        Me.RRPoiDebugRstOnStart = Settings.RRPoiDebugRstOnStart
        Me.RRPoiRunPluginOnStart = Settings.RRPoiRunPluginOnStart
        Me.RRPoiLanguage = Settings.RRPoiLanguage

    End Sub

    Public Shared Sub SerializeToXML(ByRef Settings As cMySettings)
        Dim xmlSerializer As New XmlSerializer(GetType(cMySettings))
        Using xmlTextWriter As New XmlTextWriter(XMLFilename, Encoding.UTF8)
            xmlTextWriter.Formatting = Formatting.Indented
            xmlSerializer.Serialize(xmlTextWriter, Settings)
            xmlTextWriter.Close()
        End Using
    End Sub

    Public Shared Sub DeseralizeFromXML(ByRef Settings As cMySettings)
        Dim fs As FileStream = Nothing
        ' do i have settings?
        If File.Exists(XMLFilename) = True Then
            Try
                fs = New FileStream(XMLFilename, FileMode.Open, FileAccess.Read)
                Dim xmlSerializer As New XmlSerializer(GetType(cMySettings))
                Settings = xmlSerializer.Deserialize(fs)
            Catch
                'load error of some sort, or OBJECT deserialize error
                'do we tell anyone?
                Exit Sub
            Finally
                If Not fs Is Nothing Then fs.Close()
                fs = Nothing
            End Try
        End If

    End Sub

    Public Shared Sub Copy(ByRef SourceSettings As cMySettings, ByRef DestSettings As cMySettings)
        DestSettings.RRPoiDebug = SourceSettings.RRPoiDebug
        DestSettings.RRPoiDebugRstOnStart = SourceSettings.RRPoiDebugRstOnStart
        DestSettings.RRPoiRunPluginOnStart = SourceSettings.RRPoiRunPluginOnStart
        DestSettings.RRPoiLanguage = SourceSettings.RRPoiLanguage

    End Sub

    Public Shared Sub SetToDefault(ByRef Settings)
        Settings.RRPoiDebug = True
        Settings.RRPoiDebugRstOnStart = False
        Settings.RRPoiRunPluginOnStart = False
        Settings.RRPoiLanguage = "french"
    End Sub

    Public Shared Function Compare(ByRef Settings1 As cMySettings, ByRef Setting2 As cMySettings) As Boolean
        If Settings1.RRPoiDebug <> Setting2.RRPoiDebug Then Compare = False : Exit Function
        If Settings1.RRPoiDebugRstOnStart <> Setting2.RRPoiDebugRstOnStart Then Compare = False : Exit Function
        If Settings1.RRPoiRunPluginOnStart <> Setting2.RRPoiRunPluginOnStart Then Compare = False : Exit Function
        If Settings1.RRPoiLanguage <> Setting2.RRPoiLanguage Then Compare = False : Exit Function

        Compare = True

    End Function

End Class
