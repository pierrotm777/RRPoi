Option Strict Off
Option Explicit On

Imports System.Text
Imports System.Runtime.InteropServices

Public Class IniFile
    Public path As String

    <DllImport("kernel32")> _
    Private Shared Function WritePrivateProfileString(ByVal section As String, ByVal key As String, ByVal val As String, ByVal filePath As String) As Long
    End Function
    <DllImport("kernel32")> _
    Private Shared Function GetPrivateProfileString(ByVal section As String, ByVal key As String, ByVal def As String, ByVal retVal As StringBuilder, ByVal size As Integer, ByVal filePath As String) As Integer
    End Function

    ' <summary>
    ' Конструктор класса
    ' </summary>
    ' <PARAM name="INIPath">Путь к INI-файлу</PARAM>
    Public Sub New(ByVal INIPath As String)
        path = INIPath
    End Sub
    ''' <summary>
    ''' Запись данных в INI-файл
    ''' </summary>
    ''' <PARAM name="Section"></PARAM>
    ''' Название секции
    ''' <PARAM name="Key"></PARAM>
    ''' Имя ключа
    ''' <PARAM name="Value"></PARAM>
    ''' Значение
    Public Sub IniWriteValue(ByVal Section As String, ByVal Key As String, ByVal Value As String)
        WritePrivateProfileString(Section, Key, Value, Me.path)
    End Sub

    ''' <summary>
    ''' Чтение данных из INI-файла
    ''' </summary>
    ''' <PARAM name="Section"></PARAM>
    ''' <PARAM name="Key"></PARAM>
    ''' <returns>Значение заданного ключа</returns>
    Public Function IniReadValue(ByVal Section As String, ByVal Key As String) As String
        Dim temp As New StringBuilder(255)
        Dim i As Integer = GetPrivateProfileString(Section, Key, "", temp, 255, Me.path)
        Return temp.ToString()
    End Function
End Class
