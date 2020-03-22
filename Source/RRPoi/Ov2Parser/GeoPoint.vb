Friend Class GeoPoint
    Public x As Double = 0
    Public y As Double = 0

    Public ReadOnly Property TomTomXPos() As Integer
        Get
            Return CType(Math.Truncate(x * 100000), Integer)
        End Get
    End Property

    Public ReadOnly Property TomTomYPos() As Integer
        Get
            Return CType(Math.Truncate(x * 100000), Integer)
        End Get
    End Property

    Public Sub New()
    End Sub

    Public Sub New(xPos As Double, yPos As Double)
        x = xPos
        y = yPos
    End Sub
End Class
