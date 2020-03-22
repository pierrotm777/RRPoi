Public Class IdDistList
    Private Sub New()
    End Sub
    Public Shared Created As Boolean = False
    Public Shared point_idDist_list As System.Collections.ArrayList

    Public Shared Sub SetIdList()
        point_idDist_list = New System.Collections.ArrayList()
        Created = True
    End Sub

    Public Shared Function GetValue(id_name As String) As String
        Dim SDK = CreateObject("RideRunner.SDK")
        For i As Integer = 0 To point_idDist_list.Count - 1
            If DirectCast(point_idDist_list(i), Id).name = id_name Then
                Return DirectCast(point_idDist_list(i), Id).value
            End If
        Next
        Dim id_value As String = SDK.GetInfo((Convert.ToString("=$") & id_name) + "$")
        point_idDist_list.Add(New Id(id_name, id_value))
        Return id_value
    End Function
End Class
