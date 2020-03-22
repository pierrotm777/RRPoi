Option Strict Off
Option Explicit On

Imports System
Imports System.IO
Imports System.Threading
Imports System.Globalization

Imports System.Xml
Imports System.Timers
Imports System.Reflection

'*****************************************************************
' Every Plugin MUST have its OWN set of GUIDs. use Tools->Create GUID
'*****************************************************************
<ComClass("8F08B1BA-C4EB-4CCD-8601-24FE454653CF", "19004F25-2683-433A-A0BE-AA74AEC76E72")> _
Public Class RRExtension

    Dim RunOnce As Boolean ' set to prevent a double execution of code
    Friend SDK As RRSDK ' set type of var to the subclass
    Dim WithEvents TimerPopup As System.Timers.Timer
    'Public Shared Ilist As IdDistList
    'Dim DecimalSeparator As String = Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberDecimalSeparator



    '*****************************************************************
    '* This is an interface to add commands/labels/indicators/sliders
    '* to RideRunner without needing a whole new application for such.
    '*
    '* You can monitor commands executed in RR by checking the CMD
    '* paramter of ProcessCommand and similarly monitor labels and
    '* indicators of the current screen. The idea is so you can create
    '* new commands, labels, indicators and sliders without having
    '* to re-compile or understand the code in RideRunner.
    '*
    '* Furthermore, it should be possible to intercept commands and
    '* modify them to your interst, say "AUDIO" to "LOAD;MYAUDIO.SKIN"
    '* for this all you need to do is modify CMD and return 3 on the
    '* processcommand call so that RR executes the command you return.
    '*
    '* You're free to use this code in any way you see fit.
    '*
    '*****************************************************************

    '*****************************************************************
    '* This sub is called when pluginmgr;about command is used
    '*
    '*****************************************************************
    Public Sub About(ByRef frm As Object)

        MsgBox("RideRunner RRPoi.NET Plugin", vbOKOnly, "By pierrotm777")

    End Sub

    '*****************************************************************
    '* This sub is called when pluginmgr;settings command is used
    '*
    '*****************************************************************
    Public Sub Settings(ByRef frm As Object)
        SDK.Execute("POI_SETTINGS")
    End Sub

    '*****************************************************************
    '* This function is called immediatly after plugin is loaded and
    '* when ever RR is changing the plugin status (enabled/disabled)
    '* when True its enabled, False its disabled
    '* calls to the SDK methods should not be made when the plugin is
    '* disabled. When plugin is DISABLED no calls into the plugin will
    '* be made. This is all handled by the Sub-Class I have created
    '* (RRSDK.cls)
    '*
    '*****************************************************************
    Public Sub Enabled(ByRef state As Boolean)

        ' set sub class state, which will handle all processing to the
        ' real RR SDK
        SDK.Enabled = state

    End Sub

    '*****************************************************************
    '* This sub is called immediatly after plugin is loaded and
    '* enabled, its only called once.
    '* pluginDataPath = contains where the plugin should store any of
    '* its WRITEABLE\SETTINGS data to.
    '*
    '* NOTE: The plugin is required to create this directory if needed.
    '*
    '*****************************************************************
    Public Sub Initialize(ByRef pluginDataPath As String)

        On Error Resume Next

        ' pluginDataPath will contain a USER Profile (my documents) folder path
        ' suitable for storing WRITEABLE settings to
        ' this would make your plugin OS compliant (VISTA and onward)
        ' not to mention, its proper programming, user data should NOT be stored in "Program Files"
        '
        ' example (typical vista): "C:\Users\Username\Documents\RideRunner\Plugins\MyPlugin\"
        '
        ' App.path will be the path of the ACTUALL LOADED .dll (not recomend for any writes)
        '
        ' uncomment code below if u need the directory
        '

        If Directory.Exists(pluginDataPath) = False Then Directory.CreateDirectory(pluginDataPath)
        MainPath = pluginDataPath


        '''''''''''''''''
        ''' Settings  '''
        '''''''''''''''''
        PluginSettings = New cMySettings(Path.Combine(pluginDataPath, "RRPoi"))
        'read in defaults
        cMySettings.DeseralizeFromXML(PluginSettings)
        'copy to temp
        TempPluginSettings = New cMySettings(PluginSettings)

        log_file = MainPath & "RRPoi.log"
        If TempPluginSettings.RRPoiDebugRstOnStart = True And File.Exists(log_file) = True Then File.Delete(log_file)

        'creation de la liste des languages
        If Not File.Exists(MainPath & "Languages.txt") Then ListeDirectoriesIntoDirectory(MainPath & "Languages", True)

        'TempPluginSettings.RRPoiLanguage
        ReadLanguageVars()

        Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = "."

        'TimerPopup
        TimerPopup = New System.Timers.Timer
        TimerPopup.Enabled = False
        TimerPopup.Interval = 1000
        TimerPopup.AutoReset = True

        If IdDistList.Created = False Then
            IdDistList.SetIdList()
        End If
        If IdWidthList.Created = False Then
            IdWidthList.SetIdWidthList()
        End If


        Poi_Open = New Thread(AddressOf PoiOpen)
        Poi_Open.IsBackground = True
        Poi_Open.Start()
        If TempPluginSettings.RRPoiRunPluginOnStart = True Then
            isStop = False
        Else
            isStop = True
        End If

    End Sub

    '*****************************************************************
    '* This sub is called on unload of plugin by RR
    '*
    '*****************************************************************
    Public Sub Terminate()
        TimerPopup.Enabled = False
        TimerPopup.Dispose()
        CE_plagin_status = False
    End Sub


    '*****************************************************************
    '* This function provides the metadata
    '*
    '* a string containing a "item" is passed into the function
    '*
    '*
    '*****************************************************************
    Public Function Properties(ByRef item As String) As String

        Properties = ""
        Select Case item
            Case "version"
                Properties = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            Case "author"
                Properties = "pierrotm777"
            Case "category"
                Properties = "TBD"
            Case "description"
                Properties = "RRPoi (Danger Alarm)"
            Case "supporturl"
                Properties = "pierrotm777@gmail.com"
            Case "menuitem"
                Properties = Chr(34) + "poi_run" + Chr(34) + ",Poi,Icons\RRPoi.png,RRPoi,RRPoi is selected"
        End Select

    End Function

#Region "ProcessCommand"
    '*****************************************************************
    '* This Function will be called with the current command string
    '* The return parameter of this function determines the handling
    '* To be taken upon returning to RR:
    '*
    '* 0 = Command not processed here
    '* 1 = Command completed + return to previous screen
    '* 2 = Command completed, stay on current screen
    '* 3 = Command has been changed/modified, execute returned one
    '*
    '* frm is the form object which generated the current command. Be
    '* VERY VERY careful when using it.
    '*
    '* frm.tag contains the screen name for the same screen.
    '* you can poll other propperties/methods from the screen but you
    '* will need to look at RR's frmskin to know what you can use/do.
    '*****************************************************************

    Public Function ProcessCommand(ByVal CMD As String, ByVal frm As Object) As Integer
        Dim NoStoped As Boolean = True

        Select Case LCase(CMD)
            Case "poi_ov2_read"
                Dim Ov2Path As String = MainPath & "Poi.ov2"
                poiList = OV2Parser.ReadOV2(Ov2Path)
                For i = 0 To poiList.Count - 1 '51,87709, 4,17875, Vierpolders , vv, ZH, 31-181-414644
                    'sHtml.WriteLine("  ['" & Replace(Replace(poiList(i).Name, "'", " "), ",", "-") & "', " & Replace(poiList(i).Lat, ",", ".") & ", " & Replace(poiList(i).Long, ",", ".") & ", " & poiList(i).ExtendedData & ", " & poiList(i).Telephone.Replace("-", "").Replace(".", "") & "],")
                Next
                ProcessCommand = 2

            Case "poi_run"
                SDK.Execute("MENU;POI_SELECT.SKIN")
                ProcessCommand = 2
                'rrmenuitem5 = "ONSetPoiTIMER",NEW POI,Icons\RRPOI.png,,RRPoi New is selected
            Case "poi_onsetpoitimer"
                SDK.Execute("ONSetPoiTIMER")
                ProcessCommand = 2

            Case "poi_onoff"
                isStop = Not isStop
                ProcessCommand = 2

            Case "poi_stop"
                isStop = True
                ProcessCommand = 2

            Case "poi_start"
                isStop = False
                ProcessCommand = 2

            Case "onsuspend"
                ProcessCommand = 2

            Case "onresume"
                ProcessCommand = 2

            Case "onskinend"
                Poi_Open.Abort()
                'SDK.RRSDK = Nothing
                GC.Collect()
                ProcessCommand = 2

            Case "poi_new"
                'SyncLock locked
                Try

                    Dim tmp_lattitude As String = SDK.GetInfo("=$POI_lattitude$")
                    If tmp_lattitude = "" Then
                        tmp_lattitude = "0"
                    End If
                    Dim tmp_longitude As String = SDK.GetInfo("=$POI_longitude$")
                    If tmp_longitude = "" Then
                        tmp_longitude = "0"
                    End If

                    points_list.Add(New Point())
                    Dim i As Integer = points_list.Count - 1

                    DirectCast(points_list(i), Point).id = SDK.GetInfo("=$POI_id$") 'Номер точки
                    DirectCast(points_list(i), Point).name = SDK.GetInfo("=$POI_name$") 'Номер точки
                    DirectCast(points_list(i), Point).lattitude = Convert.ToDouble(tmp_lattitude) 'Широта
                    DirectCast(points_list(i), Point).longitude = Convert.ToDouble(tmp_longitude) 'Долгота
                    DirectCast(points_list(i), Point).hdg = Convert.ToInt16(SDK.GetInfo("=$POI_hdg$")) 'Азимут
                    DirectCast(points_list(i), Point).hdg_back = Convert.ToInt16(SDK.GetInfo("=$POI_hdg_back$")) 'Обратный азимут
                    DirectCast(points_list(i), Point).deltahdg = Convert.ToInt16(SDK.GetInfo("=$POI_deltahdg$")) 'Угол проверки азимута
                    DirectCast(points_list(i), Point).dist = Convert.ToInt16(SDK.GetInfo("=$POI_dist$")) 'Расстояние до точки в метрах
                    DirectCast(points_list(i), Point).skincommand = SDK.GetInfo("=$POI_skincommand$") 'Команда передаваемая в Road Runner
                    DirectCast(points_list(i), Point).skincommand_out = SDK.GetInfo("=$POI_skincommand_out$") 'Команда передаваемая в Road Runner
                    DirectCast(points_list(i), Point).InSeach = True
                    If True Then
                        Dim tmp_w As String = IdWidthList.GetValue("PoiWidthId" & DirectCast(points_list(i), Point).id)
                        If tmp_w = "" Then
                            DirectCast(points_list(i), Point).width = 50
                        Else
                            DirectCast(points_list(i), Point).width = Convert.ToInt16(tmp_w)
                        End If
                        If DirectCast(points_list(i), Point).width = 0 Then
                            DirectCast(points_list(i), Point).width = 50
                        End If
                    End If
                    trd = New Thread(AddressOf SaveXml)
                    trd.IsBackground = True
                    trd.Start()
                    'MsgBox("poi_new a été lancé !")
                    ToLog("poi_new is added !")
                Catch ex As System.Exception
                    'System.IO.File.AppendAllText(log_file, ex.Message)
                    ToLog("poi_new error: " & ex.Message)
                End Try
                'End SyncLock

                ProcessCommand = 2

            Case "poi_delete"
                'SyncLock locked
                Dim tmp_id As String = SDK.GetInfo("=$POI_id$") 'numéro du point
                Dim tmp_lattitude As Double = Convert.ToDouble(SDK.GetInfo("=$POI_lattitude$")) 'latitude
                Dim tmp_longitude As Double = Convert.ToDouble(SDK.GetInfo("=$POI_longitude$")) 'longitude
                Dim isDelete As Boolean = False
                For i As Integer = 0 To points_list.Count - 1
                    If DirectCast(points_list(i), Point).id = tmp_id AndAlso DirectCast(points_list(i), Point).lattitude = tmp_lattitude AndAlso DirectCast(points_list(i), Point).longitude = tmp_longitude Then
                        points_list.RemoveAt(i)
                        isDelete = True
                    End If
                Next
                If isDelete Then
                    trd = New Thread(AddressOf SaveXml)
                    trd.IsBackground = True
                    trd.Start()
                End If
                'End SyncLock
                ProcessCommand = 2

            Case "poi_edit"
                ProcessCommand = 2

            Case "poi_save_to_kml"
                'SyncLock locked
                SaveToKml(SDK.GetInfo("=$kml_id$"), SDK.GetInfo("=$kml_name$"))
                'End SyncLock
                ProcessCommand = 2

            Case "poi_add_kml"
                'SyncLock locked
                Dim KmlFileName As String = SDK.GetInfo("=$Poi_KmlFileName$")
                Dim kml_file As New XmlDocument()
                kml_file.LoadXml(System.IO.File.ReadAllText(KmlFileName))

                Dim Placemark As XmlNodeList = kml_file.GetElementsByTagName("Placemark")
                'CultureInfo en = new CultureInfo("en-US");

                Dim poi_id As String = SDK.GetInfo("=$POI_id$")

                For Each x As XmlNode In Placemark
                    points_list.Add(New Point())
                    'i++;
                    Dim i As Integer = points_list.Count - 1
                    DirectCast(points_list(i), Point).id = poi_id                                                       ' Номер точки
                    DirectCast(points_list(i), Point).lattitude = Convert.ToDouble(SDK.GetInfo("=$POI_lattitude$"))     ' Широта
                    DirectCast(points_list(i), Point).longitude = Convert.ToDouble(SDK.GetInfo("=$POI_longitude$"))     ' Долгота
                    DirectCast(points_list(i), Point).hdg = Convert.ToInt16(SDK.GetInfo("=$POI_hdg$"))                  ' Азимут
                    DirectCast(points_list(i), Point).hdg_back = Convert.ToInt16(SDK.GetInfo("=$POI_hdg_back$"))        ' Обратный азимут
                    DirectCast(points_list(i), Point).deltahdg = Convert.ToInt16(SDK.GetInfo("=$POI_deltahdg$"))        ' Угол проверки азимута
                    DirectCast(points_list(i), Point).dist = Convert.ToInt16(SDK.GetInfo("=$POI_dist$"))                ' Расстояние до точки в метрах
                    DirectCast(points_list(i), Point).skincommand = SDK.GetInfo("=$POI_skincommand$")                   'Команда передаваемая в Road Runner
                    'DirectCast(points_list(i), Point).skincommand_out = SDK.GetInfo("=$POI_skincommand_out$")          'Команда передаваемая в Road Runner
                    DirectCast(points_list(i), Point).InSeach = True

                    For Each xc As XmlNode In x.ChildNodes
                        Select Case xc.Name.ToLower()
                            Case "name"
                                DirectCast(points_list(i), Point).name = xc.InnerText
                                ProcessCommand = 2

                            Case "point"
                                If True Then
                                    For Each xcc As XmlNode In xc.ChildNodes
                                        If xcc.Name.ToLower() = "coordinates" Then
                                            Dim coordinates As String() = xcc.InnerText.Split((New [Char]() {","}))
                                            Dim ii As Integer = 0
                                            For Each s As String In coordinates
                                                Select Case ii
                                                    Case 0
                                                        DirectCast(points_list(i), Point).longitude = Convert.ToDouble(s, en)
                                                        'DirectCast(points_list(i), Point).longitude = Convert.ToDouble(s)
                                                        ProcessCommand = 2

                                                    Case 1
                                                        DirectCast(points_list(i), Point).lattitude = Convert.ToDouble(s, en)
                                                        'DirectCast(points_list(i), Point).lattitude = Convert.ToDouble(s)
                                                        ProcessCommand = 2

                                                End Select
                                                ii += 1

                                            Next
                                        End If
                                    Next
                                End If
                                Exit Select
                        End Select
                    Next
                Next
                'End SyncLock
                trd = New Thread(AddressOf SaveXml)
                trd.IsBackground = True
                trd.Start()
                If NoStoped Then
                    isStop = False
                End If
                ProcessCommand = 2


            Case "poi_settings_choose"
                SDK.Execute("MENU;RRPoi_Setting_Choose.skin")
                ProcessCommand = 2
                'rrmenuitem4 = "menu;RRPoi_setting_choose.skin",RRPOI Settings,Icons\RRPOI.png,,RRPoi Settings is selected
            Case "poi_settings"
                'Timer1.Enabled = False
                SDK.SetUserVar("RR_GmailInfo", SDK.GetUserVar("l_set_ActualRRPoiLanguage") & " '" & TempPluginSettings.RRPoiLanguage & "'")
                SDK.Execute("LOAD;RRPOI_SETTINGS.skin||SETVAR;RRPoiInfo;" & SDK.GetUserVar("l_set_ActualRRPoiLanguage") & " '" & TempPluginSettings.RRPoiLanguage & "'")
                If File.Exists(MainPath & "Languages.txt") Then
                    SDK.Execute("CLCLEAR;ALL||CLLOAD;" & MainPath & "Languages.txt" & "||CLFIND;" & TempPluginSettings.RRPoiLanguage) 'selectionne la ligne du language actuel
                Else
                    SDK.ErrScrn("!! Info !!", MainPath & "'Languages.txt' file is not found !!!", "")
                End If
                ProcessCommand = 2

            Case "poi_debugtoggle"
                TempPluginSettings.RRpoiDebug = Not TempPluginSettings.RRpoiDebug
                ProcessCommand = 2
            Case "poi_debugresettoggle"
                TempPluginSettings.RRPoiDebugRstOnStart = Not TempPluginSettings.RRPoiDebugRstOnStart
                ProcessCommand = 2
            Case "poi_runonstart"
                TempPluginSettings.RRPoiRunPluginOnStart = Not TempPluginSettings.RRPoiRunPluginOnStart
                ProcessCommand = 2

            Case "poi_settings_apply"
                cMySettings.Copy(TempPluginSettings, PluginSettings)
                cMySettings.SerializeToXML(PluginSettings)
                'Timer1.Enabled = True
                ProcessCommand = 2
            Case "poi_settings_default"
                cMySettings.SetToDefault(PluginSettings)
                ' copy to temp (skin views temp)
                cMySettings.Copy(PluginSettings, TempPluginSettings)
                cMySettings.SerializeToXML(PluginSettings)
                'Timer1.Enabled = False
                ProcessCommand = 2
            Case "poi_settings_cancel"
                cMySettings.Copy(PluginSettings, TempPluginSettings)
                ProcessCommand = 2

            Case "poi_language_select"
                SDK.Execute("SETVAR;RRPoiInfo;" & SDK.GetUserVar("l_set_NewRRPoiLanguage") & " '" & SDK.GetUserVar("LISTTEXT") & "'")
                TempPluginSettings.RRPoiLanguage = SDK.GetUserVar("LISTTEXT")
                ProcessCommand = 2

            Case "poi_language_updatelist"
                If File.Exists(MainPath & "Languages.txt") Then File.Delete(MainPath & "Languages.txt")
                ListeDirectoriesIntoDirectory(MainPath & "Languages", True)
                SDK.Execute("CLCLEAR;ALL||CLLOAD;" & MainPath & "Languages.txt||CLFIND;" & TempPluginSettings.RRPoiLanguage)
                ProcessCommand = 2

                'Replace commands into exectbl.ini file
                'Case "Poi_ActionNN_Start"
                '    SDK.Execute("SetVarFromVar;PoiAction_sound;Action$ActionN$_sound||" & _
                '                "PlayerVolAtt_mem||" & _
                '                "PlaySound;$PLUGINSPATH$GPSExec\PoiSound\$language$\$PoiAction_sound$||" & _
                '                "StartTimer;RRPoi_Poi_Action;1000||" & _
                '                "SetVarFromVar;Poi_ActionTime;Action$ActionN$_Wait||" & _
                '                "SetVarFromVar;PoiActionCMD;Action$ActionN$_CMD||" & _
                '                "SetVarFromVar;PoiAction_CMD_img;Action$ActionN$_CMD_img||" & _
                '                "SetVarFromVar;PoiAction_CMD_txt;Action$ActionN$_CMD_txt||" & _
                '                "SetVarFromVar;PoiAction_txt;Action$ActionN$_txt||" & _
                '                "SetVar;IsRRPoiWork;1||" & _
                '                "MENU;RRPoi_Home.skin")
                '    ProcessCommand = 2

        End Select



        'dt = Split(LCase(CMD), ";")
        'Select Case LCase(dt(0))
        '    Case "loadvarsfromfile" 'LoadVarsFromFile;C:\Program Files\Ride Runner\Skins\iCarDuino\DuinoKey\duino_relay_button_1.txt
        '        LoadVarsFromFile(dt(1))
        '        ProcessCommand = 2
        '    Case "savevartofile" 'SaveVarToFile;$skinpath$DuinoKey\duino_relay_button_$relay_button_N$.txt;duino_relay_1;$duino_relay_1$
        '        SaveVarToFile(dt(1), dt(2), dt(2) & "=" & dt(3))
        '        ProcessCommand = 2
        '        'End If

        'End Select
        If LCase(Left$(CMD, 17)) = "loadvarsfromfile;" Then
            LoadVarsFromFile(Mid$(CMD, 18))
            ProcessCommand = 2
        End If
        If LCase(Left$(CMD, 14)) = "savevartofile;" Then
            Dim varSplit() As String = CMD.Split(";")
            SaveVarToFile(varSplit(1), varSplit(2), varSplit(2) & "=" & varSplit(3))
            ProcessCommand = 2
        End If

        If LCase(Left$(CMD, 6)) = "popup;" Then
            Dim varSplit() As String = CMD.Split(";")
            SDK.Execute("MENU;" & varSplit(1))
            LastScreenLoaded = varSplit(1)
            TimerPopup.Enabled = True
            TimerPopup.Interval = CInt(varSplit(2)) * 1000
            PopUpMenuIsOn = True
            ProcessCommand = 2
        End If

        'If LCase(CMD).Contains("endtimer;") Then
        '    sp = LCase(CMD).Split(";")
        '    SDK.Execute("MENU;" & dt(1))
        '    Timer1.Enabled = False
        '    ProcessCommand = 2
        'End If

        If LCase(Left$(CMD, 10)) = "closepopup" Then
            If frm.Tag = LastScreenLoaded AndAlso PopUpMenuIsOn = True Then
                SDK.Execute("ESC")
            End If
            ProcessCommand = 2
        End If

        'Replace commands into exectbl.ini
        '"Poi_Action1_Start","SetVar;ActionN;1||Poi_ActionNN_Start"
        'For i As Integer = 1 To 34
        '    If LCase(CMD) = "poi_action" & i.ToString & "_start" Then
        '        SDK.Execute("SetVar;ActionN;" & i.ToString & "||Poi_ActionNN_Start")
        '        ProcessCommand = 2
        '    End If
        'Next

    End Function

#End Region

#Region "ReturnLabel"
    '*****************************************************************
    '* This Function will be called with a requested label code and
    '* format specified at the skin file. Simply return any text to
    '* be displayed for the specified format.
    '*****************************************************************
    Public Function ReturnLabel(ByRef LBL As String, ByRef FMT As String) As String

        ReturnLabel = ""
        Select Case LCase(LBL)
            Case "poi_plugindesc"
                ReturnLabel = "RRPoi (Danger Alarm)"
            Case "poi_pluginver"
                ReturnLabel = Assembly.GetExecutingAssembly().GetName().Version.ToString()
            Case "poi_mylabel"
                ReturnLabel = ""
            Case "poi_status"
                ReturnLabel = "load"

            Case "poi_debugmode"
                ReturnLabel = TempPluginSettings.RRpoiDebug
            Case "poi_debugrstonstart"
                ReturnLabel = TempPluginSettings.RRPoiDebugRstOnStart
            Case "poi_runonstart"
                ReturnLabel = TempPluginSettings.RRPoiRunPluginOnStart
            Case "poi_language"
                ReturnLabel = TempPluginSettings.RRPoiLanguage

            Case "poi_list_count"
                ReturnLabel = points_list.Count.ToString

            Case "poi_gpsdate"
                ReturnLabel = DateTime.ParseExact(SDK.GetInfo("GPSDATE"), "ddMMyy", en)

            Case "poi_gpstime"
                ReturnLabel = CDate(DateTime.ParseExact(SDK.GetInfo("GPSTIME"), "hhmmss", en)).TimeOfDay.ToString

        End Select

    End Function

#End Region

#Region "ReturnIndicatorEx"
    '*****************************************************************
    '* This Function will be called with requested indicator code
    '* specified at the skin file. Simply return "True" or "False" to
    '* displayed the respective ON or OFF layer of the skin images.
    '* alternatively you can specify a path to a file to be displayed
    '* as the indicator specified. Return "False" to erase the image.
    '* ONLY return something else IF AND ONLY IF you process that code
    '*****************************************************************
    Public Function ReturnIndicatorEx(ByRef IND As String) As String
        'Default (No Action)
        'ONLY return something else IF AND ONLY IF you process that code
        ReturnIndicatorEx = ""
        'Dim s As String = ""
        'if (!CE_plagin_status) return s;

        Select Case LCase(IND)
            Case "poi"
                ReturnIndicatorEx = IIf(isStop = False, "True", "False")

            Case "poi_actual_language"
                ReturnIndicatorEx = MainPath & "Languages\" & TempPluginSettings.RRPoiLanguage & "\" & TempPluginSettings.RRPoiLanguage & ".gif"

            Case "poi_settings_changed"
                ReturnIndicatorEx = IIf(cMySettings.Compare(PluginSettings, TempPluginSettings) = False, "True", "False")

            Case "poi_kmlready"
                ReturnIndicatorEx = KmlFileLoaded.ToString

            Case "poi_smallpopup"
                ReturnIndicatorEx = IIf(SDK.GetUserVar("SMALLPOPUP") = "1", "True", "False")

            Case "poi_gpsheading"
                ReturnIndicatorEx = SDK.GetUserVar("SKINPATH") & "Indicators\" & Path.GetFileNameWithoutExtension(SDK.GetInd("RRGPSHeading")) & ".PNG"
        End Select

    End Function


    '*****************************************************************
    '* This Sub will be called with an indicator code "CLICKED"
    '* specified at the skin file. This "event" so to speak can be used
    '* to toggle indicators or execute any code you desire when clicking
    '* on a specifig indicator in the skin. You can also modify IND and
    '* monitor the IND parameter as to detect/alter the behaviour of
    '* how RR will process the indicator code being clicked.
    '*****************************************************************
    Public Sub IndicatorClick(ByRef IND As String)

        'If one of our indicators
        Select Case LCase(IND)
            Case "poi"
                isStop = Not isStop

        End Select

    End Sub

#End Region

#Region "Slider"
    '*****************************************************************
    '* This Function will be called with requested slider code
    '* specified at the skin file. Simply return the value of the
    '* slider to be displayed. Values should range from 0 to 65536.
    '* It is also possible to intercept/change the slider code before
    '* it is processed in RideRunner (to overwrite existing codes).
    '*****************************************************************
    Public Function ReturnSlider(ByRef SLD As String) As Integer

        'This tells RR that the Slider was not processed in this plugin
        ReturnSlider = -1

        Select Case LCase(SLD)
            'Case "myslider"
            'ReturnSlider = 1000.0! * Val(VB6.Format(TimeOfDay, "SS"))

            'case "myslider2"
            'Insert code here to return your slider value

        End Select

    End Function

    '*****************************************************************
    '* This Function will be called with requested slider code
    '* specified at the skin file. Simply return the value of the
    '* slider to be displayed. Values should range from 0 to 65536.
    '* It is also possible to intercept/change the slider code before
    '* it is processed in RideRunner (to overwrite existing codes).
    '*****************************************************************
    Public Sub SetSlider(ByRef SLD As String, ByRef Value As Integer, ByRef Direction As Boolean)

        Select Case LCase(SLD)
            'Case "myslider"
            'MsgBox("Myslider Clicked to set value to:" & CStr(Value) & " Direction: " + IIf(Direction, "UP", "DOWN"))

            'Case "myslider2"
            'Insert code to process/set slider value here

        End Select

    End Sub
#End Region

    Public Sub New()
        MyBase.New()

        If RunOnce = False Then ' only want to do once
            RunOnce = True
            'Code here is executed when loading the Extension plugin
            ' set RRSDK (this is the sub class)
            SDK = New RRSDK

            ' run any one time code here

        End If

    End Sub

    Private Sub ReadLanguageVars()
        If File.Exists(MainPath & "Languages\" & TempPluginSettings.RRPoiLanguage & "\" & TempPluginSettings.RRPoiLanguage & ".txt") Then 'ajout test présence du fichier fichier langue défini dans le fichier .ini
            sArray = File.ReadAllLines(MainPath & "Languages\" & TempPluginSettings.RRPoiLanguage & "\" & TempPluginSettings.RRPoiLanguage & ".txt")
            For Each line In sArray
                If Left(line, 1) <> "[" And Trim(line) <> "" Then
                    dt = Split(line, "=")
                    SDK.SetUserVar(dt(0), dt(1)) 'lecture des variables de langue
                End If
            Next
            ToLog("RRPoi '" & TempPluginSettings.RRPoiLanguage & "' language is loaded.")
            SDK.SetUserVar("RRPoi_Language", TempPluginSettings.RRPoiLanguage)
            SDK.SetUserVar("RRPoiInfo", "***")
        Else
            'SDK.ErrScrn("!! Info !!", MainPath & "Languages\" & Language & "\RRGMT.txt file is not found !!!", "")
            SDK.Execute("SETVAR;RRPoiInfo;" & MainPath & "Languages\" & TempPluginSettings.RRPoiLanguage & "\" & TempPluginSettings.RRPoiLanguage & ".txt" & SDK.GetUserVar("l_set_CheckRRGoogleMapsToolstxt") & "||menu;RRGoogleMapsTools_info.skin")
        End If
    End Sub

#Region "POI Open"
    Public Sub PoiOpen()
        Dim f_exit As Boolean = True

        PoiFileName = MainPath & "GPSExec\poi.kml" 'SDK.GetInfo("=$skinpath$") & "GPSExec\poi.kml"
        Try
            poi_file.LoadXml(System.IO.File.ReadAllText(PoiFileName))
            KmlFileLoaded = True
            ToLog("POI.kml file is loaded")
        Catch ex As System.Exception
            KmlFileLoaded = False
            'System.IO.File.AppendAllText(log_file, ex.Message & vbCr & vbLf)
            ToLog(ex.Message)
            poi_file.LoadXml(new_poi_kml_file)
        End Try

        Dim folder As XmlNodeList = poi_file.GetElementsByTagName("Point")

        CE_plagin_status = True

        For Each x As XmlNode In folder
            points_list.Add(New Point(x)) 'i++;
        Next

        Dim Lat As Double = 0
        Dim Lon As Double = 0
        Dim Hdg As Integer = 0
        Dim nn As Integer = 0
        Dim prev_poi_count As Integer = 0

        While f_exit
            isPause = True
            'SyncLock locked
            If Not isStop Then
                'System.IO.File.AppendAllText(@log_file, "------work----\r\n");
                iStoped = False
                CE_plagin_status = True

                Lat = Convert.ToDouble(SDK.GetInfo("GPSLAT"))
                Lon = Convert.ToDouble(SDK.GetInfo("GPSLON"))
                Hdg = Convert.ToInt16(SDK.GetInfo("GPSHDG"))

                'Lat = Convert.ToDouble(SDK.GetInfo("=$GPS_LAT$"));
                'Lon = Convert.ToDouble(SDK.GetInfo("=$GPS_LON$"));
                'Hdg = Convert.ToInt16(SDK.GetInfo("=$GPS_HDG$"));
                'SDK.Execute("-------------" + Lat + "-------------------------" + Lon + "---------------------------------" + Hdg );

                Dim IsEq As Boolean = False
                Dim poi_count As Integer = 0
                Dim IsFindPoi As Boolean = False

                Dim delta_lat As Double
                Dim delta_lon As Double

                For i As Integer = 0 To points_list.Count - 1
                    delta_lat = Math.Abs(DirectCast(points_list(i), Point).lattitude - Lat)
                    delta_lon = Math.Abs(DirectCast(points_list(i), Point).longitude - Lon)
                    If delta_lat < 2 AndAlso delta_lon < 2 Then
                        If DirectCast(points_list(i), Point).hdg <> 360 Then
                            IsEq = DirectCast(points_list(i), Point).IsEq(Lat, Lon, Hdg)
                        Else
                            If DirectCast(points_list(i), Point).deltahdg <> 360 Then
                                IsEq = DirectCast(points_list(i), Point).IsEq2(Lat, Lon, Hdg)
                            Else
                                IsEq = DirectCast(points_list(i), Point).IsEq3(Lat, Lon)
                            End If
                        End If
                    Else
                        IsEq = False
                    End If
                    If IsEq Then
                        ' Если координаты подходят
                        If DirectCast(points_list(i), Point).skincommand = "no" AndAlso DirectCast(points_list(i), Point).skincommand_out = "no" Then
                            ' Если команда для выполнения не задана то
                            poi_count += 1
                            ' Номер точки

                            ' Широта
                            ' Долгота
                            ' Азимут
                            ' Обратный азимут
                            ' Угол проверки азимута
                            ' Расстояние до точки в метрах
                            ' Расстояние до точки в метрах
                            ' Команда передаваемая в Road Runner
                            ' Команда передаваемая в Road Runner
                            SDK.Execute("SetVar;POI_id" & poi_count & ";" & DirectCast(points_list(i), Point).id & "||" &
                                        "SetVar;POI_name" & poi_count & ";" & DirectCast(points_list(i), Point).name & "||" &
                                        "SetVar;POI_lattitude" & poi_count & ";" & DirectCast(points_list(i), Point).lattitude & "||" &
                                        "SetVar;POI_longitude" & poi_count & ";" & DirectCast(points_list(i), Point).longitude & "||" &
                                        "SetVar;POI_hdg" & poi_count & ";" & DirectCast(points_list(i), Point).hdg & "||" &
                                        "SetVar;POI_hdg_back" & poi_count & ";" & DirectCast(points_list(i), Point).hdg_back & "||" &
                                        "SetVar;POI_deltahdg" & poi_count & ";" & DirectCast(points_list(i), Point).deltahdg & "||" &
                                        "SetVar;POI_dist" & poi_count & ";" & DirectCast(points_list(i), Point).dist & "||" &
                                        "SetVar;POI_real_dist" & poi_count & ";" & Math.Round(DirectCast(points_list(i), Point).real_dist) & "||" &
                                        "SetVar;POI_skincommand" & poi_count & ";" & DirectCast(points_list(i), Point).skincommand & "||" &
                                        "SetVar;POI_skincommand_out" & poi_count & ";" & DirectCast(points_list(i), Point).skincommand_out & "||")
                            If DirectCast(points_list(i), Point).InSeach Then
                                ' Если точка найдена первый раз устанавливаем POI_is_find№ как новая найденная (1)
                                SDK.Execute("SetVar;POI_is_find" & poi_count & ";1||SetVar;POI_real_dist" & poi_count & ";" & Math.Round(DirectCast(points_list(i), Point).real_dist))
                                IsFindPoi = True
                                DirectCast(points_list(i), Point).InSeach = False ' nn = i
                            Else
                                ' Если точка была уже найдена до этого устанавливаем POI_is_find№ как новая уже найденная (0)
                                SDK.Execute("SetVar;POI_is_find" & poi_count & ";0||SetVar;POI_real_dist" & poi_count & ";" & Math.Round(DirectCast(points_list(i), Point).real_dist))
                                If i = nn Then
                                    SDK.Execute("SetVar;POI_real_dist;" & Math.Round(DirectCast(points_list(i), Point).real_dist))
                                End If
                            End If
                        Else
                            If DirectCast(points_list(i), Point).InSeach AndAlso (DirectCast(points_list(i), Point).skincommand <> "no") Then
                                ' Номер точки

                                ' Широта
                                ' Долгота
                                SDK.Execute("SetVar;POI_cmd_id;" & DirectCast(points_list(i), Point).id & "||" &
                                            "SetVar;POI_cmd_name;" & DirectCast(points_list(i), Point).name & "||" &
                                            "SetVar;POI_cmd_lattitude;" & DirectCast(points_list(i), Point).lattitude & "||" &
                                            "SetVar;POI_cmd_longitude;" & DirectCast(points_list(i), Point).longitude & "||" &
                                            DirectCast(points_list(i), Point).skincommand)
                                DirectCast(points_list(i), Point).InSeach = False
                            End If
                        End If
                    Else ' Если координаты не подходят включаем точку в поиск
                        If (Not DirectCast(points_list(i), Point).InSeach) AndAlso (DirectCast(points_list(i), Point).skincommand_out <> "no") Then
                            ' Номер точки

                            ' Широта
                            ' Долгота
                            SDK.Execute("SetVar;POI_cmd_id;" & DirectCast(points_list(i), Point).id & "||" &
                                        "SetVar;POI_cmd_name;" & DirectCast(points_list(i), Point).name & "||" &
                                        "SetVar;POI_cmd_lattitude;" & DirectCast(points_list(i), Point).lattitude & "||" &
                                        "SetVar;POI_cmd_longitude;" & DirectCast(points_list(i), Point).longitude & "||" &
                                        DirectCast(points_list(i), Point).skincommand_out)
                        End If
                        DirectCast(points_list(i), Point).InSeach = True
                    End If
                Next
                If poi_count < prev_poi_count Then ' Если количество найденных точек за этот проход меньше чем за предыдущий, то очищаем значения освободившихся переменных
                    For i As Integer = poi_count + 1 To prev_poi_count
                        'SDK.Execute("SetVar;POI_id" + i + ";no||" +                  // Номер точки
                        '                "SetVar;POI_name" + i + ";no||" +
                        '                "SetVar;POI_lattitude" + i + ";0||" +        // Широта
                        '                "SetVar;POI_longitude" + i + ";0||" +        // Долгота
                        '                "SetVar;POI_hdg" + i + ";360||" +                    // Азимут
                        '                "SetVar;POI_hdg_back" + i + ";360||" +               // Обратный азимут
                        '                "SetVar;POI_deltahdg" + i + ";360||" +          // Угол проверки азимута
                        '                "SetVar;POI_dist" + i + ";0||" +                  // Расстояние до точки в метрах
                        '                "SetVar;POI_real_dist" + i + ";0||" +                  // Расстояние до точки в метрах
                        '                "SetVar;POI_skincommand" + i + ";no||" +    // Команда передаваемая в Road Runner
                        '                "SetVar;POI_skincommand_out" + i + ";no||" +    // Команда передаваемая в Road Runner
                        '                "SetVar;POI_is_find" + i + ";0||SetVar;POI_real_dist" + i + ";"
                        '           );
                        SDK.Execute("SetVar;POI_id" & i & ";no||" &
                                    "SetVar;POI_name" & i & ";no||" &
                                    "SetVar;POI_lattitude" & i & ";0||" &
                                    "SetVar;POI_longitude" & i & ";0||" &
                                    "SetVar;POI_hdg" & i & ";360||" &
                                    "SetVar;POI_hdg_back" & i & ";360||" &
                                    "SetVar;POI_deltahdg" & i & ";360||" &
                                    "SetVar;POI_dist" & i & ";0||" &
                                    "SetVar;POI_real_dist" & i & ";0||" &
                                    "SetVar;POI_skincommand" & i & ";no||" &
                                    "SetVar;POI_skincommand_out" & i & ";no||" &
                                    "SetVar;POI_is_find" & i & ";0||SetVar;POI_real_dist" & i & ";")
                    Next
                    If poi_count = 0 Then
                        SDK.Execute("OnPoiExit")
                    Else
                        SDK.Execute("OnPoiChange")
                    End If
                End If
                prev_poi_count = poi_count
                SDK.SetUserVar("POI_COUNT", poi_count) 'SDK.Execute("SETVAR;POI_COUNT;" & poi_count) '
                If IsFindPoi Then
                    SDK.Execute("OnPoiFind")
                    RaiseEvent ONPoiFound()
                End If
            Else
                'System.IO.File.AppendAllText(@log_file, "------stop----\r\n");
                iStoped = True
            End If
            'End SyncLock

            isPause = True
            System.Threading.Thread.Sleep(500)
        End While
    End Sub
#End Region

#Region "Save Xml"
    Public Sub SaveXml()
        Try
            While Is_SaveXML_run
            End While

            Is_SaveXML_run = True

            Dim newPoint As XmlElement
            Dim id_attr As XmlAttribute
            Dim name_attr As XmlAttribute
            Dim lattitude_attr As XmlAttribute
            Dim longitude_attr As XmlAttribute
            Dim hdg_attr As XmlAttribute
            Dim hdg_back_attr As XmlAttribute
            Dim deltahdg_attr As XmlAttribute
            Dim dist_attr As XmlAttribute
            Dim skincommand_attr As XmlAttribute
            Dim skincommand_out_attr As XmlAttribute
            'CultureInfo en = new CultureInfo("en-US");

            poi_file.DocumentElement.RemoveAll()

            For i As Integer = 0 To points_list.Count - 1
                newPoint = poi_file.CreateElement("Point")

                id_attr = poi_file.CreateAttribute("id")
                id_attr.Value = DirectCast(points_list(i), Point).id
                newPoint.SetAttributeNode(id_attr)

                name_attr = poi_file.CreateAttribute("name")
                name_attr.Value = DirectCast(points_list(i), Point).name
                newPoint.SetAttributeNode(name_attr)

                lattitude_attr = poi_file.CreateAttribute("lattitude")
                lattitude_attr.Value = DirectCast(points_list(i), Point).lattitude.ToString("N6", en)
                'lattitude_attr.Value = DirectCast(points_list(i), Point).lattitude.ToString("N6")
                newPoint.SetAttributeNode(lattitude_attr)

                longitude_attr = poi_file.CreateAttribute("longitude")
                longitude_attr.Value = DirectCast(points_list(i), Point).longitude.ToString("N6", en)
                'longitude_attr.Value = DirectCast(points_list(i), Point).longitude.ToString("N6")
                newPoint.SetAttributeNode(longitude_attr)

                hdg_attr = poi_file.CreateAttribute("hdg")
                hdg_attr.Value = DirectCast(points_list(i), Point).hdg.ToString()
                newPoint.SetAttributeNode(hdg_attr)

                hdg_back_attr = poi_file.CreateAttribute("hdg_back")
                hdg_back_attr.Value = DirectCast(points_list(i), Point).hdg_back.ToString()
                newPoint.SetAttributeNode(hdg_back_attr)

                deltahdg_attr = poi_file.CreateAttribute("deltahdg")
                deltahdg_attr.Value = DirectCast(points_list(i), Point).deltahdg.ToString()
                newPoint.SetAttributeNode(deltahdg_attr)

                dist_attr = poi_file.CreateAttribute("dist")
                dist_attr.Value = DirectCast(points_list(i), Point).dist.ToString()
                newPoint.SetAttributeNode(dist_attr)

                skincommand_attr = poi_file.CreateAttribute("skincommand")
                skincommand_attr.Value = DirectCast(points_list(i), Point).skincommand
                newPoint.SetAttributeNode(skincommand_attr)

                skincommand_out_attr = poi_file.CreateAttribute("skincommand_out")
                skincommand_out_attr.Value = DirectCast(points_list(i), Point).skincommand_out
                newPoint.SetAttributeNode(skincommand_out_attr)

                poi_file.DocumentElement.AppendChild(newPoint)
            Next
            poi_file.Save(PoiFileName)
            Is_SaveXML_run = False
        Catch ex As System.Exception
            'System.IO.File.AppendAllText(log_file, "SaveXml - " & ex.Message & vbCr & vbLf)
            ToLog("SaveXml Error: " & ex.Message)
        End Try
    End Sub
#End Region

#Region "Save to Kml"
    Public Sub SaveToKml(ByVal Id As String, ByVal Name As String)
        Try
            Dim newFolder As XmlElement
            Dim newFolderName As XmlElement
            'CultureInfo en = new CultureInfo("en-US")

            poi_file.DocumentElement.RemoveAll()
            newFolder = poi_file.CreateElement("Folder")
            newFolderName = poi_file.CreateElement("name")
            newFolderName.InnerText = Name
            newFolder.AppendChild(newFolderName)

            For i As Integer = 0 To points_list.Count - 1
                If DirectCast(points_list(i), Point).id = Id Then
                    Dim newPlacemark As XmlElement
                    Dim newName As XmlElement
                    Dim newPoint As XmlElement
                    Dim newCoordinates As XmlElement

                    newPlacemark = poi_file.CreateElement("Placemark")

                    newName = poi_file.CreateElement("name")
                    newName.InnerText = DirectCast(points_list(i), Point).name

                    newPoint = poi_file.CreateElement("Point")

                    newCoordinates = poi_file.CreateElement("coordinates")
                    newCoordinates.InnerText = DirectCast(points_list(i), Point).longitude.ToString("N6", en) & "," & DirectCast(points_list(i), Point).lattitude.ToString("N6", en)
                    'newCoordinates.InnerText = DirectCast(points_list(i), Point).longitude.ToString("N6") & "," & DirectCast(points_list(i), Point).lattitude.ToString("N6")

                    newFolder.AppendChild(newPlacemark)
                    newPlacemark.AppendChild(newName)
                    newPlacemark.AppendChild(newPoint)
                    newPoint.AppendChild(newCoordinates)
                End If
            Next
            poi_file.DocumentElement.AppendChild(newFolder)
            poi_file.Save(Name)
        Catch ex As System.Exception
            'System.IO.File.AppendAllText(log_file, "SaveToXml - " & ex.Message & vbCr & vbLf)
            ToLog("SaveToXml - " & ex.Message)
        End Try
    End Sub
#End Region

#Region "Debug Log"
    Public Sub ToLog(ByVal TheMessage As String)
        If TempPluginSettings.RRpoiDebug = True Then
            If Not File.Exists(log_file) Then
                DebugLog = New StreamWriter(log_file)
            Else
                DebugLog = File.AppendText(log_file)
            End If
            ' Write to the file:
            DebugLog.WriteLine(DateTime.Now + "-->" & TheMessage)
            ' Close the stream:
            DebugLog.Close()
        End If
    End Sub

#End Region

#Region "RR Events"
    'Events
    Private Event ONPoiFound()
    Private Event ONPoiSaved()
    Private Event ONPoiDeleted()
    Private Sub PoiFound() Handles Me.ONPoiFound
        SDK.Execute("*ONPOIFOUND", True)
    End Sub
    Private Sub PoiSaved() Handles Me.ONPoiSaved
        SDK.Execute("*ONPOISAVED", True)
    End Sub
    Private Sub PoiDeleted() Handles Me.ONPoiDeleted
        SDK.Execute("*ONPOIDELETED", True)
    End Sub
#End Region

#Region "Timers"
    'Dim timer As New System.Timers.Timer(10000)
    Private Sub TimerPopup_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles TimerPopup.Elapsed
        TimerPopup.Enabled = False
        PopUpMenuIsOn = False
    End Sub

    'Public Sub CreateNewTimer(ByVal TimerName As System.Timers.Timer, ByVal newInterval As Integer)
    '    'MsgBox(TheTimer.ToString) 'this gives the name/tag of timer
    '    TimerName.Interval = newInterval * 1000
    'End Sub
    'Private Sub OnTimedEvent(sender As Object, e As ElapsedEventArgs)
    '    CreateNewTimer(sender, 5)
    'End Sub

#End Region


End Class




