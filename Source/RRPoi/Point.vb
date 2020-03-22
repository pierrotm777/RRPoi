Option Strict Off
Option Explicit On


Imports System.Globalization
Imports System.Xml

''' <summary>
''' Создание нового INI-файла для хранения данных
''' </summary>
Public Class Point
    Public id As String' Номер точки
    Public name As String
    Public lattitude As Double' Широта
    Public longitude As Double ' Долгота
    Public hdg As Integer' Азимут
    Public hdg_back As Integer' Обратный азимут
    Public deltahdg As Integer' Угол проверки азимута
    Public dist As Integer' Расстояние до точки в метрах
    Public width As Integer' Расстояние до точки в метрах
    Public skincommand As String' Команда передаваемая в iCar при входе в зону действия точки
    Public skincommand_out As String' Команда передаваемая в iCar при выходе из зоны действия точки
    Public InSeach As Boolean
    Public real_dist As Double

    ' Конструктор класса

    Public Sub New()
        id = "0"' Номер точки
        name = "noname"
        lattitude = 0' Широта
        longitude = 0' Долгота
        hdg = 360' Азимут
        hdg_back = 360' Обратный азимут
        deltahdg = 360' Угол проверки азимута
        dist = 150' Расстояние до точки в метрах
        width = 50
        skincommand = "no"' Команда передаваемая в Road Runner
        skincommand_out = "no"
        InSeach = True
        real_dist = 0
    End Sub

    Public Sub New(x As XmlNode)
        id = "no"' Номер точки
        name = "noname"
        lattitude = 0' Широта
        longitude = 0' Долгота
        hdg = 360' Азимут
        hdg_back = 360' Обратный азимут
        deltahdg = 360' Угол проверки азимута
        dist = 150' Расстояние до точки в метрах
        width = 50
        skincommand = "no"' Команда передаваемая в Road Runner
        skincommand_out = "no"
        InSeach = True
        real_dist = 0

        'Dim en As New CultureInfo("en-US")

        For i As Integer = 0 To x.Attributes.Count - 1
            Select Case x.Attributes(i).Name.ToLower()
                Case "id"
                    id = x.Attributes(i).Value
                    Exit Select

                Case "name"
                    name = x.Attributes(i).Value
                    If True Then
                        Dim tmp_w As String = IdWidthList.GetValue(Convert.ToString("PoiWidthId") & id)
                        If tmp_w = "" Then
                            width = 50
                        Else
                            width = Convert.ToInt16(tmp_w)
                        End If
                        If width = 0 Then
                            width = 50
                        End If
                    End If
                    Exit Select

                Case "lattitude"
                    lattitude = Convert.ToDouble(x.Attributes(i).Value, en)
                    'lattitude = Convert.ToDouble(x.Attributes(i).Value)
                    Exit Select

                Case "longitude"
                    longitude = Convert.ToDouble(x.Attributes(i).Value, en)
                    'longitude = Convert.ToDouble(x.Attributes(i).Value)
                    Exit Select

                Case "hdg"
                    hdg = Convert.ToInt16(x.Attributes(i).Value)
                    Exit Select

                Case "hdg_back"
                    hdg_back = Convert.ToInt16(x.Attributes(i).Value)
                    Exit Select

                Case "deltahdg"
                    deltahdg = Convert.ToInt16(x.Attributes(i).Value)
                    Exit Select

                Case "dist"
                    If True Then
                        Dim tmp_w As String = IdDistList.GetValue(Convert.ToString("PoiDistId") & id)
                        If tmp_w = "" Then
                            dist = 0
                        Else
                            dist = Convert.ToInt16(tmp_w)
                        End If
                        If dist = 0 Then
                            dist = Convert.ToInt16(x.Attributes(i).Value)
                        End If
                    End If
                    Exit Select

                Case "skincommand"
                    skincommand = x.Attributes(i).Value
                    Exit Select

                Case "skincommand_out"
                    skincommand_out = x.Attributes(i).Value
                    Exit Select

            End Select
        Next
    End Sub

    Public Function IsEq(Lat As Double, Lon As Double, Hdg__1 As Integer) As Boolean
        If (HdgCompare(Hdg__1, hdg, deltahdg) OrElse HdgCompare(Hdg__1, hdg_back, deltahdg)) AndAlso HdgCompare(Hdg__1, Me.Header(Lat, Lon), deltahdg) Then
            real_dist = LatLonDistance(Lat, Lon, lattitude, longitude)
            If real_dist <= dist Then
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function

    Public Function IsEq2(Lat As Double, Lon As Double, Hdg As Integer) As Boolean
        Dim current_width As Double
        Dim a_hdg_rad As Double
        Dim current_hdg As Integer = Me.Header(Lat, Lon)
        real_dist = LatLonDistance(Lat, Lon, lattitude, longitude)
        Dim a_hdg As Integer = Math.Abs(current_hdg - Hdg)
        If a_hdg > 180 Then
            a_hdg = 360 - a_hdg
        End If
        If a_hdg <= 90 Then
            a_hdg_rad = a_hdg / 57.295779513
            current_width = real_dist * Math.Sin(a_hdg_rad)
            If (real_dist * Math.Cos(a_hdg_rad) <= CDbl(dist)) AndAlso (current_width <= CDbl(width)) Then
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function

    Public Function IsEq3(Lat As Double, Lon As Double) As Boolean
        real_dist = LatLonDistance(Lat, Lon, lattitude, longitude)
        If real_dist <= dist Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Function HdgCompare(Hdg As Integer, CurrentHdg As Integer, DeltaHdg As Integer) As Boolean
        Dim tmp_hdg As Integer = Math.Abs(CurrentHdg - Hdg)
        If tmp_hdg > 180 Then
            tmp_hdg = 360 - tmp_hdg
        End If
        If tmp_hdg <= DeltaHdg Then
            Return True
        Else
            Return False
        End If
    End Function

    ' Возвращает расстояние между двумя точками
    Private Function LatLonDistance(dbLat1 As Double, dbLon1 As Double, dbLat2 As Double, dbLon2 As Double) As Double

        Dim loRadiusOfEarth As Long = 6367000
        Dim dbDeltaLat As Double
        Dim dbDeltaLon As Double
        Dim dbTemp As Double
        Dim dbTemp2 As Double

        If dbLon2 = 0 And dbLat2 = 0 Then
            Return 0
        End If

        dbDeltaLon = AsRadians(dbLon2) - AsRadians(dbLon1)
        dbDeltaLat = AsRadians(dbLat2) - AsRadians(dbLat1)

        dbTemp = Sin2(dbDeltaLat / 2) + Math.Cos(AsRadians(dbLat1)) * Math.Cos(AsRadians(dbLat2)) * Sin2(dbDeltaLon / 2)
        dbTemp2 = 2 * Arcsin(Math.Min(1, Math.Sqrt(dbTemp)))
        Return loRadiusOfEarth * dbTemp2
    End Function
    Private Function Arcsin(x As Double) As Double
        Return Math.Atan(x / Math.Sqrt(-x * x + 1))
    End Function
    Private Function AsRadians(pDb_Degrees As Double) As Double
        Return pDb_Degrees * (3.14159265358979 / 180)
    End Function
    Private Function Sin2(x As Double) As Double
        Return (1 - Math.Cos(2 * x)) / 2
    End Function

    ' Озвращает азимут от точки Lat,Lon  на объект Point
    Public Function Header(Lat As Double, Lon As Double) As Integer
        Const PI As Double = 3.14159265358979
        Dim dY As Double = Lat - lattitude
        ' 
        Dim dX As Double = Lon - longitude
        '
        Dim Hdg_tmp As Double = 0

        If dX = 0 AndAlso dY < 0 Then
            Hdg_tmp = 0
        Else
            If dX < 0 AndAlso dY = 0 Then
                Hdg_tmp = PI * 0.5
            Else
                If dX = 0 AndAlso dY > 0 Then
                    Hdg_tmp = PI
                Else
                    If dX > 0 AndAlso dY = 0 Then
                        Hdg_tmp = PI * 1.5
                    Else
                        If dX < 0 AndAlso dY < 0 Then
                            Hdg_tmp = PI * 0.5 - Math.Atan(Math.Abs(dY / dX))
                        Else
                            If dX < 0 AndAlso dY > 0 Then
                                Hdg_tmp = PI * 0.5 + Math.Atan(Math.Abs(dY / dX))
                            Else
                                If dX > 0 AndAlso dY > 0 Then
                                    Hdg_tmp = PI * 1.5 - Math.Atan(Math.Abs(dY / dX))
                                Else
                                    If dX > 0 AndAlso dY < 0 Then
                                        Hdg_tmp = PI * 1.5 + Math.Atan(Math.Abs(dY / dX))
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End If
        Hdg_tmp = Hdg_tmp * 57.295779513
        Return CInt(Math.Round(Hdg_tmp))
    End Function
End Class


