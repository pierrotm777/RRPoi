Friend Class GeoRect
    Public BottomLeft As New GeoPoint()
    Public TopRight As New GeoPoint()
    Public POIs As New List(Of POI)

    Public Sub New()
    End Sub

    Public Sub New(bottomLft As GeoPoint, topRght As GeoPoint)
        BottomLeft = bottomLft
        TopRight = topRght
    End Sub
End Class
