Public Class POI
    Public Property Name As String
    Public Property ExtendedData As String
    Public Property Lat As Double
    Public Property [Long] As Double
    Public Property Telephone As String
	
	 Public Overrides Function ToString() As String
        If Me.ExtendedData IsNot Nothing Then
            Return String.Format("{0}, {1}, {2}, {3}, {4}", Lat.ToString(), [Long].ToString(), Name, Telephone, ExtendedData)
        Else
            Return String.Format("{0}, {1}, {2}, {3}", Lat.ToString(), [Long].ToString(), Name, Telephone)
        End If
    End Function
End Class
