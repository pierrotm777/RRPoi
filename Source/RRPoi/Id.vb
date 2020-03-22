Option Strict Off
Option Explicit On

Public Class Id
    Public name As String
    Public value As String

    Public Sub New(ByVal id_name As String, ByVal id_value As String)
        name = id_name
        value = id_value
    End Sub
End Class
