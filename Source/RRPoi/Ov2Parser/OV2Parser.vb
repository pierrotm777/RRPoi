Imports System.IO
Imports System.Text

Public Class OV2Parser

    Private Const SignedBit As Byte = &H80
    Private Const SimplePOIStructSize As Integer = 14
    Private Const ExtendedPOIStructSize As Integer = 14
    Private Const SkipperStructSize As Integer = 21
    Private Const DeletedStructSize As Integer = 10

    'Private ReadOnly _minRectSize As New GeoSize(0.00113, 0.00139)
    Private _minRectSize As New GeoSize(0.00113, 0.00139)
    Public Property MinRectSize() As GeoSize
        Get
            Return _minRectSize
        End Get
        Set(value As GeoSize)
            If _minRectSize Is value Then Return
            _minRectSize = value
        End Set
    End Property

    Public Property MaxPOIsInRectangle() As Integer = 16

    Public Sub CreateOV2(filePath As String, POIs As List(Of POI))
        Using bw As New BinaryWriter(New FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            Dim boundingRect As GeoRect = CalculateBoundingRect(POIs)
            Dim rectangleList As List(Of GeoRect) = ProcessPOIs(boundingRect)

            bw.Write(GenerateOV2Header(boundingRect, rectangleList, POIs))

            For Each rect In rectangleList
                bw.Write(SerializeRectangle(rect))
                For Each POI In rect.POIs
                    bw.Write(If(String.IsNullOrEmpty(POI.ExtendedData), SerializeSimplePOI(POI), SerializeExtendedPOI(POI)))
                Next
            Next
        End Using
    End Sub

    Public Shared Function ReadOV2(filePath As String) As List(Of POI)
        Dim isReading As Boolean = True
        Dim pos As Integer = SkipperStructSize
        Dim bin As Byte() = New Byte(3) {}
        Dim blockSize As Integer = 0
        Dim poiList As New List(Of POI)()

        Using br As New BinaryReader(New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            Dim bytesRead As Byte() = New Byte(br.BaseStream.Length - 1) {}
            br.Read(bytesRead, 0, br.BaseStream.Length)

            While isReading
                Dim recordType As POIRecordTypes = CType(bytesRead(pos), POIRecordTypes)
                Select Case recordType
                    Case POIRecordTypes.Skipper
                        pos += SkipperStructSize
                    Case POIRecordTypes.SimplePOI
                        bin(0) = bytesRead(pos + 1)
                        bin(1) = bytesRead(pos + 2)
                        bin(2) = bytesRead(pos + 3)
                        bin(3) = bytesRead(pos + 4)
                        blockSize = BitConverter.ToInt32(bin, 0)
                        Dim block As Byte() = New Byte(blockSize - 1) {}
                        Array.Copy(bytesRead, pos, block, 0, blockSize)
                        Dim newPOI As POI = ConvertToPOI(block)
                        poiList.Add(newPOI)
                        pos += SimplePOIStructSize + newPOI.Name.Length
                        If Not String.IsNullOrEmpty(newPOI.Telephone) Then pos += newPOI.Telephone.Length + 1
                    Case POIRecordTypes.ExtendedPOI
                        bin(0) = bytesRead(pos + 1)
                        bin(1) = bytesRead(pos + 2)
                        bin(2) = bytesRead(pos + 3)
                        bin(3) = bytesRead(pos + 4)
                        blockSize = BitConverter.ToInt32(bin, 0)
                        Dim block As Byte() = New Byte(blockSize - 1) {}
                        Array.Copy(bytesRead, pos, block, 0, blockSize)
                        Dim newPOI As POI = ConvertToPOI(block)
                        pos += ExtendedPOIStructSize + newPOI.Name.Length + newPOI.ExtendedData.Length
                        If Not String.IsNullOrEmpty(newPOI.Telephone) Then pos += newPOI.Telephone.Length + 1
                        Exit Select
                    Case POIRecordTypes.Deleted
                        If True Then pos += DeletedStructSize
                    Case Else
                        If True Then Throw New ApplicationException("Invalid byte read, file may be corrupt.")
                End Select
                isReading = pos < bytesRead.Length
            End While
            Return poiList
        End Using
    End Function

    Private Shared Function ConvertToPOI(POIData As Byte()) As POI
        Dim returnPOI As New POI()
        Dim pos As Integer = 0
        Dim bin As Byte() = New Byte(3) {}
        Dim byteRead As Byte = &H1

        'Lon
        bin(pos) = POIData(pos + 5)
        bin(pos + 1) = POIData(pos + 6)
        bin(pos + 2) = POIData(pos + 7)
        bin(pos + 3) = POIData(pos + 8)
        returnPOI.Long = CDbl(BitConverter.ToInt32(bin, 0)) / 100000

        'Lat
        bin(pos) = POIData(pos + 9)
        bin(pos + 1) = POIData(pos + 10)
        bin(pos + 2) = POIData(pos + 11)
        bin(pos + 3) = POIData(pos + 12)
        returnPOI.Lat = CDbl(BitConverter.ToInt32(bin, 0)) / 100000

        'Name
        pos += 13
        byteRead = POIData(pos)
        While byteRead <> &H0
            returnPOI.Name += ChrW(byteRead)
            pos += 1
            byteRead = POIData(pos)
        End While

        'Has phone num?
        If returnPOI.Name.Contains(">") Then
            Dim GTPos As Integer = returnPOI.Name.LastIndexOf(">"c)
            returnPOI.Telephone = returnPOI.Name.Substring(GTPos + 1)
            returnPOI.Name = returnPOI.Name.Substring(0, GTPos)
        End If

        If POIData(0) = CByte(POIRecordTypes.ExtendedPOI) Then
            pos += 1
            byteRead = POIData(pos)
            While byteRead <> &H0
                returnPOI.ExtendedData += ChrW(byteRead)
                pos += 1
                byteRead = POIData(pos)
            End While
        End If

        Return returnPOI

    End Function

    Private Function ProcessPOIs(boundingRect As GeoRect) As List(Of GeoRect)
        Dim rectWasSplit As Boolean = True
        Dim processRectsList As New List(Of GeoRect)
        Dim processedRectsList As List(Of GeoRect) = Nothing
        processRectsList.Add(boundingRect)

        Dim rectList As New List(Of GeoRect)
        While rectWasSplit
            rectWasSplit = False
            processedRectsList = New List(Of GeoRect)

            For i = 0 To processRectsList.Count - 1
                Dim currentRect As GeoRect = processedRectsList(i)
                If currentRect.POIs.Count > _MaxPOIsInRectangle AndAlso Not RectangleIsMinSize(currentRect) Then
                    Dim splitRectsList As List(Of GeoRect) = SplitRectangle(currentRect)
                    DeterminePOIsInSplitRectangles(splitRectsList, currentRect.POIs)
                    processedRectsList.AddRange(splitRectsList)
                    rectWasSplit = True
                ElseIf currentRect.POIs.Count <> 0 Then
                    rectList.Add(currentRect)
                End If
            Next

            processRectsList = New List(Of GeoRect)(processedRectsList)
        End While

        Return rectList
    End Function

    Private Function RectangleIsMinSize(rect As GeoRect) As Boolean

        Return If(GeoDifference(rect.BottomLeft.x, rect.TopRight.x) <= _minRectSize.Width _
                  AndAlso GeoDifference(rect.BottomLeft.y, rect.TopRight.y) <= _minRectSize.Height, True, False)

    End Function

    Private Shared Sub DeterminePOIsInSplitRectangles(rectList As List(Of GeoRect), poiList As List(Of POI))
        Dim rect1 As GeoRect = rectList(0)
        Dim rect2 As GeoRect = rectList(1)

        For Each currentPOI In poiList
            If currentPOI.Long >= rect1.BottomLeft.x _
                AndAlso currentPOI.Long < rect1.TopRight.x _
                AndAlso currentPOI.Lat >= rect1.BottomLeft.y _
                AndAlso currentPOI.Lat < rect1.TopRight.y Then
                rect1.POIs.Add(currentPOI)
            Else
                rect2.POIs.Add(currentPOI)
            End If
        Next
    End Sub

    Private Shared Function GetPOISize(poi As POI) As Integer
        Dim poiSize As Integer = 0
        poiSize += If(String.IsNullOrEmpty(poi.ExtendedData),
                      SimplePOIStructSize + poi.Name.Length,
                      ExtendedPOIStructSize + poi.Name.Length + poi.ExtendedData.Length)

        If Not String.IsNullOrEmpty(poi.Telephone) Then poiSize += poi.Telephone.Length + 1

        Return poiSize
    End Function

    Private Shared Function SplitRectangle(rect As GeoRect) As List(Of GeoRect)
        Dim returnList As New List(Of GeoRect)
        Dim northSouthDiff As Double = GeoDifference(rect.BottomLeft.y, rect.TopRight.y)
        Dim eastWestDiff As Double = GeoDifference(rect.BottomLeft.x, rect.TopRight.x)

        If northSouthDiff >= eastWestDiff Then
            Dim topRect As New GeoRect(New GeoPoint(rect.BottomLeft.x, rect.BottomLeft.y + (northSouthDiff / 2)),
                                       New GeoPoint(rect.TopRight.x, rect.TopRight.y))
            returnList.Add(topRect)
            Dim bottomRect As New GeoRect(New GeoPoint(rect.BottomLeft.x, rect.BottomLeft.y),
                                          New GeoPoint(rect.TopRight.x, rect.TopRight.y - (northSouthDiff / 2)))
            returnList.Add(bottomRect)
        Else
            Dim leftRect As New GeoRect(New GeoPoint(rect.BottomLeft.x, rect.BottomLeft.y),
                                        New GeoPoint(rect.TopRight.x - (eastWestDiff / 2), rect.TopRight.y))
            returnList.Add(leftRect)
            Dim rightRect As New GeoRect(New GeoPoint(rect.BottomLeft.x + (eastWestDiff / 2), rect.BottomLeft.y),
                                         New GeoPoint(rect.TopRight.x, rect.TopRight.y))
            returnList.Add(rightRect)
        End If

        Return returnList
    End Function

    Private Shared Function GeoDifference(pt1 As Double, pt2 As Double) As Double
        Dim returnDifference As Double = 0

        If pt1 <= 0 AndAlso pt2 <= 0 Then
            returnDifference = pt1 - pt2
        ElseIf pt1 >= 0 AndAlso pt2 <= 0 Then
            returnDifference = pt1 + (-pt2)
        ElseIf pt1 <= 0 AndAlso pt2 >= 0 Then
            returnDifference = (-pt1) + pt2
        Else
            returnDifference = pt1 - pt2
        End If

        Return If(returnDifference < 0, -returnDifference, returnDifference)
    End Function

    Private Shared Function CalculateBoundingRect(POIs As List(Of POI)) As GeoRect
        Dim returnRect As New GeoRect()
        With returnRect
            .BottomLeft.x = 90
            .BottomLeft.y = 180
            .TopRight.x = -90
            .TopRight.y = -180
        End With

        For Each currentPOI In POIs
            If currentPOI.Long < returnRect.BottomLeft.x Then returnRect.BottomLeft.x = currentPOI.Long
            If currentPOI.Long > returnRect.TopRight.x Then returnRect.TopRight.x = currentPOI.Long
            If currentPOI.Lat < returnRect.BottomLeft.y Then returnRect.BottomLeft.y = currentPOI.Lat
            If currentPOI.Lat > returnRect.TopRight.y Then returnRect.TopRight.y = currentPOI.Lat
        Next

        returnRect.POIs = POIs
        Return returnRect

    End Function

    Private Shared Function GenerateOV2Header(boundingRect As GeoRect, rectList As List(Of GeoRect), allPOIs As List(Of POI)) As Byte()
        Dim returnByte() = New Byte(SkipperStructSize - 1) {}
        Dim sz As Integer = SkipperStructSize

        For Each currentPOI In allPOIs
            sz += GetPOISize(currentPOI)
        Next

        sz += (rectList.Count * SkipperStructSize)

        returnByte(0) = &H1
        ConvertToLEBytes(sz).CopyTo(returnByte, 1)
        ConvertToLEBytes(CType(boundingRect.TopRight.TomTomXPos, Integer)).CopyTo(returnByte, 5)
        ConvertToLEBytes(CType(boundingRect.TopRight.TomTomYPos, Integer)).CopyTo(returnByte, 9)
        ConvertToLEBytes(CType(boundingRect.BottomLeft.TomTomXPos, Integer)).CopyTo(returnByte, 13)
        ConvertToLEBytes(CType(boundingRect.BottomLeft.TomTomYPos, Integer)).CopyTo(returnByte, 17)

        Return returnByte
    End Function

    Private Shared Function SerializeRectangle(rect As GeoRect) As Byte()
        Dim returnByte() = New Byte(SkipperStructSize - 1) {}
        Dim allPOIsSize As Integer = SkipperStructSize

        For Each currentPOI In rect.POIs
            allPOIsSize += GetPOISize(currentPOI)
        Next

        returnByte(0) = &H1
        ConvertToLEBytes(allPOIsSize).CopyTo(returnByte, 1)
        ConvertToLEBytes(CType(rect.TopRight.TomTomXPos, Int32)).CopyTo(returnByte, 5)
        ConvertToLEBytes(CType(rect.TopRight.TomTomYPos, Int32)).CopyTo(returnByte, 9)
        ConvertToLEBytes(CType(rect.BottomLeft.TomTomXPos, Int32)).CopyTo(returnByte, 13)
        ConvertToLEBytes(CType(rect.BottomLeft.TomTomYPos, Int32)).CopyTo(returnByte, 17)

    End Function

    Private Shared Function SerializeExtendedPOI(rec As POI) As Byte()
        Dim pos As Integer = 0
        Dim sz As Integer = ExtendedPOIStructSize + rec.Name.Length + rec.ExtendedData.Length
        If Not String.IsNullOrEmpty(rec.Telephone) Then sz += rec.Telephone.Length
        Dim returnByte() = New Byte(sz - 1) {}
        returnByte(pos) = &H3
        pos += 1
        ConvertToLEBytes(sz).CopyTo(returnByte, pos)
        pos += 4
        ConvertToLEBytes(CType(rec.Long * 100000, Integer)).CopyTo(returnByte, pos)
        pos += 4
        ConvertToLEBytes(CType(rec.Lat * 100000, Integer)).CopyTo(returnByte, pos)
        pos += 4
        Encoding.ASCII.GetBytes(rec.Name).CopyTo(returnByte, pos)
        pos += rec.Name.Length
        If Not String.IsNullOrEmpty(rec.Telephone) Then
            returnByte(pos) = &H3E
            pos += 1
            Encoding.ASCII.GetBytes(rec.Telephone).CopyTo(returnByte, pos)
            pos += rec.Telephone.Length
        End If
        returnByte(pos) = &H0
        pos += 1
        Encoding.ASCII.GetBytes(rec.ExtendedData).CopyTo(returnByte, pos)
        pos += 1
        returnByte(pos) = &H0
        pos += 1
        returnByte(pos) = &H0
        Return returnByte
    End Function

    Private Shared Function SerializeSimplePOI(rec As POI) As Byte()
        Dim pos As Integer = 0
        Dim sz As Integer = SimplePOIStructSize + rec.Name.Length
        If Not String.IsNullOrEmpty(rec.Telephone) Then sz += rec.Telephone.Length + 1
        Dim returnByte() = New Byte(sz - 1) {}
        returnByte(pos) = &H2
        pos += 1
        ConvertToLEBytes(sz).CopyTo(returnByte, pos)
        pos += 4
        ConvertToLEBytes(CType(rec.Long * 100000, Integer)).CopyTo(returnByte, pos)
        pos += 4
        ConvertToLEBytes(CType(rec.Lat * 100000, Integer)).CopyTo(returnByte, pos)
        pos += 4
        Encoding.ASCII.GetBytes(rec.Name).CopyTo(returnByte, pos)
        pos += rec.Name.Length

        If Not String.IsNullOrEmpty(rec.Telephone) Then
            returnByte(pos) = &H3E
            pos += 1
            Encoding.ASCII.GetBytes(rec.Telephone).CopyTo(returnByte, pos)
            pos += rec.Telephone.Length
        End If
    End Function

    Private Shared Function ConvertToLEBytes(BEVal As Integer) As Byte()
        Return BitConverter.GetBytes(BEVal)
    End Function

    Private Shared Function SignedLeLatLongToInteger(littleEndianBytes As Byte()) As Integer

        Dim bigEndian As Integer = SwapEndian(BitConverter.ToInt32(littleEndianBytes, 0))
        Return If((littleEndianBytes(3) And SignedBit) = SignedBit, (Not bigEndian) + 1, bigEndian)

    End Function

    Private Shared Function SwapEndian(value As Integer) As Integer
        Return CType((value << 24) And &HFF000000UI, Integer) _
            + ((value << 8) And &HFF0000) _
            + ((value >> 8) And &HFF00) _
            + ((value >> 24) And &HFF)
    End Function

End Class
