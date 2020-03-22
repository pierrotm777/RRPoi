'Public NotInheritable Class IdWidthList
Public Class IdWidthList
    Private Sub New()
    End Sub
    Public Shared Created As Boolean = False
    Public Shared point_idWidth_list As System.Collections.ArrayList

    Public Shared Sub SetIdWidthList()
        point_idWidth_list = New System.Collections.ArrayList()
        Created = True
    End Sub

    Public Shared Function GetValue(id_name As String) As String
        Dim SDK = CreateObject("RideRunner.SDK")
        For i As Integer = 0 To point_idWidth_list.Count - 1
            If DirectCast(point_idWidth_list(i), Id).name = id_name Then
                Return DirectCast(point_idWidth_list(i), Id).value
            End If
        Next
        Dim id_value As String = SDK.GetInfo((Convert.ToString("=$") & id_name) + "$")
        point_idWidth_list.Add(New Id(id_name, id_value))
        Return id_value
    End Function
End Class
