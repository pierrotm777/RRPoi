Public Class GeoSize
    Public Property Width() As Double
    Public Property Height() As Double

    Public Sub New()
    End Sub

    Public Sub New(width As Double, height As Double)
        _Width = width
        _Height = height
    End Sub

End Class
