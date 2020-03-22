using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Globalization;

using System.Web; //
using System.Net; //
using System.Net.Mail; // 
using System.Net.Mime; //
using System.Net.Sockets;




using System.Xml;

/*
 * Remember every new plug in must have new GUIDS, there are 3:
 * The Interface class (IRRExtension.cs), the RRExtension class (RRExtsion.cs)
 * and in the properties for the ASSEMBLY (AssemblyInfo.cs)
*/

namespace RRPoi
{
	[Guid("4E7297A3-25D0-4e96-9A30-011588400011")]
	[ClassInterface(ClassInterfaceType.None)]
    public class RRExtension : RRPoi.IRRPoi
	{
        // Состояние плагина
        public static bool CE_plagin_status = false;
        public string CE_plagin_e = "";
        public static string log_file = @"c:\rrpoi.log";

        
        public static XmlDocument poi_file = new XmlDocument();
        public static string new_poi_kml_file = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?> <kml xmlns=""http://earth.google.com/kml/2.0""> </kml>";

        public static bool Is_SaveXML_run = false;

        Thread Poi_Open = new Thread(PoiOpen);

        public static System.Collections.ArrayList points_list = new System.Collections.ArrayList();  // list is a reference type
//        public static IdList id_list = new IdList();
//        public static IdList width_list = new IdList();


        public static bool isStop = false;
        public static bool iStoped = false;

        public static bool isPause = false;


        public static string PoiFileName;
//        public static string SkinPath;

        static object locked = new object();

        public static CultureInfo en = new CultureInfo("en-US");

		#region Private Fields
		
		#endregion

		#region Constructor

		public RRExtension()
		{
			if (SDK.Created == false) SDK.SetSDK();
            if (IdDistList.Created == false) IdDistList.SetIdList();
            if (IdWidthList.Created == false) IdWidthList.SetIdWidthList();
        }

		#endregion

		#region Public RRExtension API Methods (new ones)

		// this is called ONLY once, right after plug-in is loaded
		// use this to set up anything you need, including data storage
		public void Initialize(string pluginDataPath)
		{
//            PrevDistanceWent = Convert.ToDouble(SDK.GetInfo("=$CheckEngineDistanceWent$"));
//            PrevFuelConsumed = Convert.ToDouble(SDK.GetInfo("=$CheckEngineFuelConsumed$"));
//            PrevFullFuelVolume = Convert.ToDouble(SDK.GetInfo("=$CheckEngineFullFuelVolume$"));
//            PluginAutoStart = Convert.ToInt16(SDK.GetInfo("=$CheckEngineAutoStart$"));
//            if ( PluginAutoStart == 1 )
//            poi_file.LoadXml(System.IO.File.ReadAllText(@"c:\poi.kml"));
            if(!CE_plagin_status)
            {
               Poi_Open.Start();
            }
        }

        public void Terminate()
        {
            // clean up code goes here... 
            // close any file handles...free up any unmanaged resources
            // GC will take care of managed resources
            //CE_plagin_status = false;

        }
        public void Unload()
        {
            CE_plagin_status = false;
        }    

		public void Enabled(bool state)
		{
			SDK.Enabled = state;
		}

		public string Properties(string item)
		{
			string properties = "";

			switch (item)
			{
				case "version":
					properties = Assembly.GetExecutingAssembly().GetName().Version.ToString();
					break;
        case "category":
                    properties = "RR plugin";
					break;
        case "description":
					properties = "RR POI Plagin";
					break;
        case "supporturl":
                    properties = "http://www.pccar.ru/showthread.php?t=12602";
					break;
			}

			return properties;
		}

		#endregion

		#region Public RRExtension API Methods


        public static void PoiOpen()
        {
            bool f_exit = true;

           
            PoiFileName = SDK.GetInfo("=$skinpath$") + "GPSExec\\poi.kml";
            try
            {
                poi_file.LoadXml(System.IO.File.ReadAllText(@PoiFileName));
            }
            catch (System.Exception ex)
            {
                System.IO.File.AppendAllText(@log_file, ex.Message + "\r\n");
                poi_file.LoadXml(new_poi_kml_file);
            }

            XmlNodeList folder = poi_file.GetElementsByTagName("Point");

            CE_plagin_status = true;

            foreach (XmlNode x in folder)
            {
                points_list.Add(new Point(x)); //i++;
            }

            double Lat = 0;
            double Lon = 0;
            int Hdg = 0;
            int nn = 0;
            int prev_poi_count = 0;

            while (f_exit)
            {
                isPause = true;
                lock (locked)
                {
                    if (!isStop)
                    {
//                        System.IO.File.AppendAllText(@log_file, "------work----\r\n");
                        iStoped = false;
                        CE_plagin_status = true;

                        Lat = Convert.ToDouble(SDK.GetInfo("GPSLAT"));
                        Lon = Convert.ToDouble(SDK.GetInfo("GPSLON"));
                        Hdg = Convert.ToInt16(SDK.GetInfo("GPSHDG"));
                          
//                        Lat = Convert.ToDouble(SDK.GetInfo("=$GPS_LAT$"));
//                        Lon = Convert.ToDouble(SDK.GetInfo("=$GPS_LON$"));
//                        Hdg = Convert.ToInt16(SDK.GetInfo("=$GPS_HDG$"));
                        //                    SDK.Execute("-------------" + Lat + "-------------------------" + Lon + "---------------------------------" + Hdg );

                        bool IsEq = false;
                        int poi_count = 0;
                        bool IsFindPoi = false;

                        double delta_lat;
                        double delta_lon;

                        for (int i = 0; i < points_list.Count; i++)
                        {
                            delta_lat = Math.Abs(((Point)points_list[i]).lattitude - Lat);
                            delta_lon = Math.Abs(((Point)points_list[i]).longitude - Lon);
                            if (delta_lat < 2 && delta_lon < 2)
                            {
                                if (((Point)points_list[i]).hdg != 360)
                                {
                                    IsEq = ((Point)points_list[i]).IsEq(Lat, Lon, Hdg);
                                }
                                else
                                {
                                    if (((Point)points_list[i]).deltahdg != 360)
                                    {
                                        IsEq = ((Point)points_list[i]).IsEq2(Lat, Lon, Hdg);
                                    }
                                    else
                                    {
                                        IsEq = ((Point)points_list[i]).IsEq3(Lat, Lon);
                                    }
                                }
                            }
                            else
                            {
                                IsEq = false;
                            }
                            if (IsEq)
                            { // Если координаты подходят
                                if (((Point)points_list[i]).skincommand == "no" && ((Point)points_list[i]).skincommand_out == "no")
                                { // Если команда для выполнения не задана то
                                    poi_count++;
                                    SDK.Execute("SetVar;POI_id" + poi_count + ";" + ((Point)points_list[i]).id + "||" +                  // Номер точки
                                            "SetVar;POI_name" + poi_count + ";" + ((Point)points_list[i]).name + "||" +
                                            "SetVar;POI_lattitude" + poi_count + ";" + ((Point)points_list[i]).lattitude + "||" +        // Широта
                                            "SetVar;POI_longitude" + poi_count + ";" + ((Point)points_list[i]).longitude + "||" +        // Долгота
                                            "SetVar;POI_hdg" + poi_count + ";" + ((Point)points_list[i]).hdg + "||" +                    // Азимут
                                            "SetVar;POI_hdg_back" + poi_count + ";" + ((Point)points_list[i]).hdg_back + "||" +               // Обратный азимут
                                            "SetVar;POI_deltahdg" + poi_count + ";" + ((Point)points_list[i]).deltahdg + "||" +          // Угол проверки азимута
                                            "SetVar;POI_dist" + poi_count + ";" + ((Point)points_list[i]).dist + "||" +                  // Расстояние до точки в метрах
                                            "SetVar;POI_real_dist" + poi_count + ";" + Math.Round(((Point)points_list[i]).real_dist) + "||" +                  // Расстояние до точки в метрах
                                            "SetVar;POI_skincommand" + poi_count + ";" + ((Point)points_list[i]).skincommand + "||" +   // Команда передаваемая в Road Runner
                                            "SetVar;POI_skincommand_out" + poi_count + ";" + ((Point)points_list[i]).skincommand_out + "||"    // Команда передаваемая в Road Runner
                                            );
                                    if (((Point)points_list[i]).InSeach)
                                    {   // Если точка найдена первый раз устанавливаем POI_is_find№ как новая найденная (1)
                                        SDK.Execute("SetVar;POI_is_find" + poi_count + ";1||SetVar;POI_real_dist" + poi_count + ";" + Math.Round(((Point)points_list[i]).real_dist));
                                        IsFindPoi = true;
                                        ((Point)points_list[i]).InSeach = false; // nn = i;
                                    }
                                    else
                                    {   // Если точка была уже найдена до этого устанавливаем POI_is_find№ как новая уже найденная (0)
                                        SDK.Execute("SetVar;POI_is_find" + poi_count + ";0||SetVar;POI_real_dist" + poi_count + ";" + Math.Round(((Point)points_list[i]).real_dist));
                                        if (i == nn)
                                        {
                                            SDK.Execute("SetVar;POI_real_dist;" + Math.Round(((Point)points_list[i]).real_dist));
                                        }
                                    }
                                }
                                else
                                {
                                    if (((Point)points_list[i]).InSeach && (((Point)points_list[i]).skincommand != "no"))
                                    {
                                        SDK.Execute("SetVar;POI_cmd_id;" + ((Point)points_list[i]).id + "||" +                  // Номер точки
                                                "SetVar;POI_cmd_name;" + ((Point)points_list[i]).name + "||" +
                                                "SetVar;POI_cmd_lattitude;" + ((Point)points_list[i]).lattitude + "||" +        // Широта
                                                "SetVar;POI_cmd_longitude;" + ((Point)points_list[i]).longitude + "||" +        // Долгота
                                                ((Point)points_list[i]).skincommand);
                                        ((Point)points_list[i]).InSeach = false;
                                    }
                                }
                            }
                            else
                            { // Если координаты не подходят включаем точку в поиск
                                if ((!((Point)points_list[i]).InSeach) && (((Point)points_list[i]).skincommand_out != "no"))
                                {
                                    SDK.Execute("SetVar;POI_cmd_id;" + ((Point)points_list[i]).id + "||" +                  // Номер точки
                                            "SetVar;POI_cmd_name;" + ((Point)points_list[i]).name + "||" +
                                            "SetVar;POI_cmd_lattitude;" + ((Point)points_list[i]).lattitude + "||" +        // Широта
                                            "SetVar;POI_cmd_longitude;" + ((Point)points_list[i]).longitude + "||" +        // Долгота
                                            ((Point)points_list[i]).skincommand_out);
                                }
                                ((Point)points_list[i]).InSeach = true;
                            }
                        }                 
                        if (poi_count < prev_poi_count)
                        { // Если количество найденных точек за этот проход меньше чем за предыдущий, то очищаем значения освободившихся переменных
                            for (int i = poi_count+1; i <= prev_poi_count; i++)
                            {
                                SDK.Execute("SetVar;POI_id" + i + ";no||" +                  // Номер точки
                                        "SetVar;POI_name" + i + ";no||" +
                                        "SetVar;POI_lattitude" + i + ";0||" +        // Широта
                                        "SetVar;POI_longitude" + i + ";0||" +        // Долгота
                                        "SetVar;POI_hdg" + i + ";360||" +                    // Азимут
                                        "SetVar;POI_hdg_back" + i + ";360||" +               // Обратный азимут
                                        "SetVar;POI_deltahdg" + i + ";360||" +          // Угол проверки азимута
                                        "SetVar;POI_dist" + i + ";0||" +                  // Расстояние до точки в метрах
                                        "SetVar;POI_real_dist" + i + ";0||" +                  // Расстояние до точки в метрах
                                        "SetVar;POI_skincommand" + i + ";no||" +    // Команда передаваемая в Road Runner
                                        "SetVar;POI_skincommand_out" + i + ";no||" +    // Команда передаваемая в Road Runner
                                        "SetVar;POI_is_find" + i + ";0||SetVar;POI_real_dist" + i + ";"
                                        );
                            }
                            if(poi_count == 0) 
                            {
                                SDK.Execute("OnPoiExit");
                            }
                            else
                            {
                                SDK.Execute("OnPoiChange");
                            }
                        }
                        prev_poi_count = poi_count;
                        SDK.Execute("SETVAR;POI_COUNT;" + poi_count);
                        if (IsFindPoi)
                        {
                            SDK.Execute("OnPoiFind");
                        }
                    }
                    else
                    {
//                        System.IO.File.AppendAllText(@log_file, "------stop----\r\n");
                        iStoped = true;
                    }
                }
                
                isPause = true;
                System.Threading.Thread.Sleep(500);
            }
        }

        public int ProcessCommand(string CMD, object frm)
        {
            int result = 0;
            bool NoStoped = true;

            switch (CMD.ToLower())
            {
                case "poi_stop":
                    //
                    isStop = true;

                    break;

                case "poi_start":
                    //
                    isStop = false;
                    break;

                case "onsuspend":
                    //
                    break;

                case "onresume":
                    //
                    break;

                case "onskinend":
                    Poi_Open.Abort();
                    SDK.RRSDK = null;
                    GC.Collect();
                    break;

                case "poi_new":
                    lock (locked)
                    {
                        try
                        {
                            
                            string tmp_lattitude = SDK.GetInfo("=$POI_lattitude$");
                            if (tmp_lattitude == "") tmp_lattitude = "0";
                            string tmp_longitude = SDK.GetInfo("=$POI_longitude$");
                            if (tmp_longitude == "") tmp_longitude = "0";

                            points_list.Add(new Point());
                            int i = points_list.Count - 1;
                            ((Point)points_list[i]).id = SDK.GetInfo("=$POI_id$");                                       // Номер точки
                            ((Point)points_list[i]).name = SDK.GetInfo("=$POI_name$");                                       // Номер точки
                            ((Point)points_list[i]).lattitude = Convert.ToDouble(tmp_lattitude);       // Широта
                            ((Point)points_list[i]).longitude = Convert.ToDouble(tmp_longitude);       // Долгота
                            ((Point)points_list[i]).hdg = Convert.ToInt16(SDK.GetInfo("=$POI_hdg$"));                    // Азимут
                            ((Point)points_list[i]).hdg_back = Convert.ToInt16(SDK.GetInfo("=$POI_hdg_back$"));               // Обратный азимут
                            ((Point)points_list[i]).deltahdg = Convert.ToInt16(SDK.GetInfo("=$POI_deltahdg$"));          // Угол проверки азимута
                            ((Point)points_list[i]).dist = Convert.ToInt16(SDK.GetInfo("=$POI_dist$"));                  // Расстояние до точки в метрах
                            ((Point)points_list[i]).skincommand = SDK.GetInfo("=$POI_skincommand$");                     // Команда передаваемая в Road Runner
                            ((Point)points_list[i]).skincommand_out = SDK.GetInfo("=$POI_skincommand_out$");                     // Команда передаваемая в Road Runner
                            ((Point)points_list[i]).InSeach = true;
                            {
                                string tmp_w = IdWidthList.GetValue("PoiWidthId" + ((Point)points_list[i]).id);
                                if (tmp_w == "")
                                {
                                    ((Point)points_list[i]).width = 50;
                                }
                                else
                                {
                                    ((Point)points_list[i]).width = Convert.ToInt16(tmp_w);
                                }
                                if (((Point)points_list[i]).width == 0) ((Point)points_list[i]).width = 50;
                            }
                            (new Thread(SaveXml)).Start();
                        }
                        catch (System.Exception ex)
                        {
                            System.IO.File.AppendAllText(@log_file, ex.Message);
                        }
                    }
                    
                    break;

                case "poi_delete":

                    //
                    lock (locked)
                    {
                        string tmp_id = SDK.GetInfo("=$POI_id$");                                       // Номер точки
                        double tmp_lattitude = Convert.ToDouble(SDK.GetInfo("=$POI_lattitude$"));       // Широта
                        double tmp_longitude = Convert.ToDouble(SDK.GetInfo("=$POI_longitude$"));       // Долгота
                        bool isDelete = false;
                        for (int i = 0; i < points_list.Count; i++)
                        {
                            if (((Point)points_list[i]).id == tmp_id && ((Point)points_list[i]).lattitude == tmp_lattitude && ((Point)points_list[i]).longitude == tmp_longitude)
                            {
                                points_list.RemoveAt(i); isDelete = true;
                            }
                        }
                        if (isDelete) (new Thread(SaveXml)).Start();
                    }
                    break;

                case "poi_edit":
                    break;

                case "poi_save_to_kml":

                    lock (locked)
                    {
                        SaveToKml(SDK.GetInfo("=$kml_id$"),SDK.GetInfo("=$kml_name$"));
                    }
                    break;

                case "poi_add_kml":
                    //
                    lock (locked)
                    {
                        string KmlFileName = SDK.GetInfo("=$Poi_KmlFileName$");
                        XmlDocument kml_file = new XmlDocument();
                        kml_file.LoadXml(System.IO.File.ReadAllText(@KmlFileName));

                        XmlNodeList Placemark = kml_file.GetElementsByTagName("Placemark");
//                        CultureInfo en = new CultureInfo("en-US");

                        string poi_id = SDK.GetInfo("=$POI_id$");

                        foreach (XmlNode x in Placemark)
                        {
                            points_list.Add(new Point()); //i++;
                            int i = points_list.Count - 1;
                            ((Point)points_list[i]).id = poi_id;
                            /*                            ((Point)points_list[i]).id = SDK.GetInfo("=$POI_id$");                                       // Номер точки
                                                        ((Point)points_list[i]).lattitude = Convert.ToDouble(SDK.GetInfo("=$POI_lattitude$"));       // Широта
                                                        ((Point)points_list[i]).longitude = Convert.ToDouble(SDK.GetInfo("=$POI_longitude$"));       // Долгота
                                                        ((Point)points_list[i]).hdg = Convert.ToInt16(SDK.GetInfo("=$POI_hdg$"));                    // Азимут
                                                        ((Point)points_list[i]).hdg_back = Convert.ToInt16(SDK.GetInfo("=$POI_hdg_back$"));               // Обратный азимут
                                                        ((Point)points_list[i]).deltahdg = Convert.ToInt16(SDK.GetInfo("=$POI_deltahdg$"));          // Угол проверки азимута
                                                        ((Point)points_list[i]).dist = Convert.ToInt16(SDK.GetInfo("=$POI_dist$"));                  // Расстояние до точки в метрах
                                                        ((Point)points_list[i]).skincommand = SDK.GetInfo("=$POI_skincommand$");                     // Команда передаваемая в Road Runner
                                                        ((Point)points_list[i]).InSeach = true;
                            */
                            foreach (XmlNode xc in x.ChildNodes)
                            {
                                switch (xc.Name.ToLower())
                                {
                                    case "name":
                                        ((Point)points_list[i]).name = xc.InnerText;
                                        break;

                                    case "point":
                                        {
                                            foreach (XmlNode xcc in xc.ChildNodes)
                                            {
                                                if (xcc.Name.ToLower() == "coordinates")
                                                {
                                                    string[] coordinates = xcc.InnerText.Split((new Char[] { ',' }));
                                                    int ii = 0;
                                                    foreach (string s in coordinates)
                                                    {
                                                        switch (ii)
                                                        {
                                                            case 0:
                                                                ((Point)points_list[i]).longitude = Convert.ToDouble(s,en);
                                                                break;

                                                            case 1:
                                                                ((Point)points_list[i]).lattitude = Convert.ToDouble(s,en);
                                                                break;

                                                        }
                                                        ii++;
                                                    }

                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    (new Thread(SaveXml)).Start();
                    if (NoStoped) isStop = false;
                    break;

            }

            return result;
        }

        // 
        public void SaveXml()
        {
            try
            {
                while (Is_SaveXML_run) { }

                Is_SaveXML_run = true;

                XmlElement newPoint;
                XmlAttribute id_attr;
                XmlAttribute name_attr;
                XmlAttribute lattitude_attr;
                XmlAttribute longitude_attr;
                XmlAttribute hdg_attr;
                XmlAttribute hdg_back_attr;
                XmlAttribute deltahdg_attr;
                XmlAttribute dist_attr;
                XmlAttribute skincommand_attr;
                XmlAttribute skincommand_out_attr;
//                CultureInfo en = new CultureInfo("en-US");

                poi_file.DocumentElement.RemoveAll();

                for (int i = 0; i < points_list.Count; i++)
                {
                    newPoint = poi_file.CreateElement("Point");

                    id_attr = poi_file.CreateAttribute("id");
                    id_attr.Value = ((Point)points_list[i]).id;
                    newPoint.SetAttributeNode(id_attr);

                    name_attr = poi_file.CreateAttribute("name");
                    name_attr.Value = ((Point)points_list[i]).name;
                    newPoint.SetAttributeNode(name_attr);

                    lattitude_attr = poi_file.CreateAttribute("lattitude");
                    lattitude_attr.Value = ((Point)points_list[i]).lattitude.ToString("N6",en);
                    newPoint.SetAttributeNode(lattitude_attr);

                    longitude_attr = poi_file.CreateAttribute("longitude");
                    longitude_attr.Value = ((Point)points_list[i]).longitude.ToString("N6",en);
                    newPoint.SetAttributeNode(longitude_attr);

                    hdg_attr = poi_file.CreateAttribute("hdg");
                    hdg_attr.Value = ((Point)points_list[i]).hdg.ToString();
                    newPoint.SetAttributeNode(hdg_attr);

                    hdg_back_attr = poi_file.CreateAttribute("hdg_back");
                    hdg_back_attr.Value = ((Point)points_list[i]).hdg_back.ToString();
                    newPoint.SetAttributeNode(hdg_back_attr);

                    deltahdg_attr = poi_file.CreateAttribute("deltahdg");
                    deltahdg_attr.Value = ((Point)points_list[i]).deltahdg.ToString();
                    newPoint.SetAttributeNode(deltahdg_attr);

                    dist_attr = poi_file.CreateAttribute("dist");
                    dist_attr.Value = ((Point)points_list[i]).dist.ToString();
                    newPoint.SetAttributeNode(dist_attr);

                    skincommand_attr = poi_file.CreateAttribute("skincommand");
                    skincommand_attr.Value = ((Point)points_list[i]).skincommand;
                    newPoint.SetAttributeNode(skincommand_attr);

                    skincommand_out_attr = poi_file.CreateAttribute("skincommand_out");
                    skincommand_out_attr.Value = ((Point)points_list[i]).skincommand_out;
                    newPoint.SetAttributeNode(skincommand_out_attr);

                    poi_file.DocumentElement.AppendChild(newPoint);
                }
                poi_file.Save(@PoiFileName);
                Is_SaveXML_run = false;
            }
            catch (System.Exception ex)
            {
                System.IO.File.AppendAllText(@log_file, "SaveXml - " + ex.Message + "\r\n");
            }
        }

        public void SaveToKml(string Id, string Name)
        {
            try
            {
                XmlElement newFolder;
                XmlElement newFolderName;
//                CultureInfo en = new CultureInfo("en-US");

                poi_file.DocumentElement.RemoveAll();
                newFolder = poi_file.CreateElement("Folder");
                newFolderName = poi_file.CreateElement("name");
                newFolderName.InnerText = Name;
                newFolder.AppendChild(newFolderName);

                for (int i = 0; i < points_list.Count; i++)
                {
                    if (((Point)points_list[i]).id == Id)
                    {
                        XmlElement newPlacemark;
                        XmlElement newName;
                        XmlElement newPoint;
                        XmlElement newCoordinates;

                        newPlacemark = poi_file.CreateElement("Placemark");

                        newName = poi_file.CreateElement("name");
                        newName.InnerText = ((Point)points_list[i]).name;

                        newPoint = poi_file.CreateElement("Point");

                        newCoordinates = poi_file.CreateElement("coordinates");
                        newCoordinates.InnerText = ((Point)points_list[i]).longitude.ToString("N6",en) + "," + ((Point)points_list[i]).lattitude.ToString("N6",en);

                        newFolder.AppendChild(newPlacemark);
                        newPlacemark.AppendChild(newName);
                        newPlacemark.AppendChild(newPoint);
                        newPoint.AppendChild(newCoordinates);
                    }
                }
                poi_file.DocumentElement.AppendChild(newFolder);
                poi_file.Save(Name);
            }
            catch (System.Exception ex)
            {
                System.IO.File.AppendAllText(@log_file, "SaveToXml - " + ex.Message + "\r\n");
            }
        }
                

        public string ReturnLabel(string LBL, string FMT)
		{
			string s = "";
//            if (!CE_plagin_status) return s;

            switch (LBL.ToLower())
            {
                case "poi_status":
                    s = "load";
                    break;
            }
            
			return s;
		}

		public string ReturnIndicator(string Ind)
		{
			return "";
		}

		public string ReturnIndicatorEx(string Ind)
		{
			return "";
		}

		public long ReturnSlider(string SLD)
		{
			return -1;
		}

		#endregion

		#region Logging

		[ComVisible(false)]
		static private void log(string message)
		{
			if (isAssemblyDebugBuild() == true)
			{
				SDK.DoLog(message);
			}
		}

		[ComVisible(false)]
		static private bool isAssemblyDebugBuild()
		{
			System.Reflection.Assembly assemb = System.Reflection.Assembly.GetExecutingAssembly();

			foreach (object att in assemb.GetCustomAttributes(false))
			{
				if (att.GetType() == System.Type.GetType("System.Diagnostics.DebuggableAttribute"))
				{
					return ((System.Diagnostics.DebuggableAttribute)att).IsJITTrackingEnabled;
				}
			}

			return false;
		}

		#endregion

		#region Private Methods

		#endregion
	}
	
	#region RR SDK

	[ComVisible(false)]
	static public class SDK
	{
		static bool _Created;
		public static object RRSDK;
		static Type objAddType;
		static bool _Enabled = true;

		static public void SetSDK()
		{
			try
			{
				objAddType = Type.GetTypeFromProgID("RideRunner.SDK");
				RRSDK = Activator.CreateInstance(objAddType);
				if (RRSDK != null)
				{
					_Created = true;

					_Enabled = true;
				}
			}
			catch 
			{
				// "Failed to create RideRunner SDK Com Interface"
			}
		}

		static public bool Created
		{
			get { return _Created; }
		}

		static public bool Enabled
		{
			 set { _Enabled = value; }
		}

		static public void DoLog(string message)
		{
			if (RRSDK == null)
				return;

			if (_Enabled == false)
				return;

			objAddType.InvokeMember("RRLog", BindingFlags.InvokeMethod, null, RRSDK, new object[] { message });
		}

		static public void Execute(string CMD)
		{
			if (RRSDK == null)
				return;

			if (_Enabled == false)
				return;

			objAddType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, RRSDK, new object[] { CMD, false });
		}

		static public void Execute(string CMD, bool Wait)
		{
			if (RRSDK == null)
				return;

			if (_Enabled == false)
				return;

			objAddType.InvokeMember("Execute", BindingFlags.InvokeMethod, null, RRSDK, new object[] { CMD, Wait });
		}

		static public string GetInfo(string inf)
		{
			if (RRSDK == null)
				return null;

			return (string)objAddType.InvokeMember("GetInfo", BindingFlags.InvokeMethod, null, RRSDK, new object[] { inf, "" });
		}

		static public string GetInfo(string inf, string FMT)
		{
			if (RRSDK == null)
				return null;

			if (_Enabled == false)
				return null;

			return (string)objAddType.InvokeMember("GetInfo", BindingFlags.InvokeMethod, null, RRSDK, new object[] { inf, FMT });
		}

		static public string GetInd(string ind)
		{
			if (RRSDK == null)
				return null;

			if (_Enabled == false)
				return null;

			return (string)objAddType.InvokeMember("GetInd", BindingFlags.InvokeMethod, null, RRSDK, new object[] { ind, "" });
		}

		static public string GetInd(string ind, string SCR)
		{
			if (RRSDK == null)
				return null;

			if (_Enabled == false)
				return null;

			return (string)objAddType.InvokeMember("GetInd", BindingFlags.InvokeMethod, null, RRSDK, new object[] { ind, SCR });
		}

		static public void ErrScrn(string Title, string Subject, string Message, int Timeout)
		{
			if (RRSDK == null)
				return;

			if (_Enabled == false)
				return;

			objAddType.InvokeMember("ErrScrn", BindingFlags.InvokeMethod, null, RRSDK, new object[] { Title, Subject, Message, Timeout });
		}

		static public void ErrScrn(string Title, string Subject, string Message)
		{
			SDK.ErrScrn(Title, Subject, Message, (int)-1);
		}
	}
	#endregion

    /// <summary>
    /// Создание нового INI-файла для хранения данных
    /// </summary>
    public class Point
    {
        public string   id;         // Номер точки
        public string name;
        public double   lattitude;  // Широта
        public double   longitude;  // Долгота
        public int      hdg;        // Азимут
        public int      hdg_back;   // Обратный азимут
        public int      deltahdg;   // Угол проверки азимута
        public int      dist;       // Расстояние до точки в метрах
        public int      width;       // Расстояние до точки в метрах
        public string skincommand;// Команда передаваемая в iCar при входе в зону действия точки
        public string skincommand_out;// Команда передаваемая в iCar при выходе из зоны действия точки
        public bool InSeach;
        public double   real_dist;

        // Конструктор класса

        public Point()
        {
            id="0";           // Номер точки
            name = "noname";
            lattitude=0;    // Широта
            longitude=0;    // Долгота
            hdg = 360;          // Азимут
            hdg_back = 360;     // Обратный азимут
            deltahdg = 360;   // Угол проверки азимута
            dist=150;         // Расстояние до точки в метрах
            width = 50;
            skincommand="no"; // Команда передаваемая в Road Runner
            skincommand_out = "no";
            InSeach = true;
            real_dist = 0;
         }

        public Point(XmlNode x)
        {
            id="no";           // Номер точки
            name = "noname";
            lattitude = 0;    // Широта
            longitude=0;    // Долгота
            hdg=360;          // Азимут
            hdg_back = 360;     // Обратный азимут
            deltahdg = 360;   // Угол проверки азимута
            dist=150;         // Расстояние до точки в метрах
            width = 50;
            skincommand="no"; // Команда передаваемая в Road Runner
            skincommand_out = "no";
            InSeach = true;
            real_dist = 0;
            
            CultureInfo en = new CultureInfo("en-US");

            for (int i = 0; i < x.Attributes.Count; i++)
            {
                switch (x.Attributes[i].Name.ToLower())
                {
                    case "id":
                        id = x.Attributes[i].Value;
                        break;

                    case "name":
                        name = x.Attributes[i].Value;
                        {
                            string tmp_w = IdWidthList.GetValue("PoiWidthId" + id);
                            if (tmp_w == "")
                            {
                                width = 50;
                            }
                            else
                            {
                                width = Convert.ToInt16(tmp_w);
                            }
                            if (width == 0) width = 50;
                        }
                        break;

                    case "lattitude":
                        lattitude = Convert.ToDouble(x.Attributes[i].Value,en);
                        break;

                    case "longitude":
                        longitude = Convert.ToDouble(x.Attributes[i].Value,en);
                        break;

                    case "hdg":
                        hdg = Convert.ToInt16(x.Attributes[i].Value);
                        break;

                    case "hdg_back":
                        hdg_back = Convert.ToInt16(x.Attributes[i].Value);
                        break;

                    case "deltahdg":
                        deltahdg = Convert.ToInt16(x.Attributes[i].Value);
                        break;

                    case "dist":
                        {
                            string tmp_w = IdDistList.GetValue("PoiDistId" + id);
                            if (tmp_w == "")
                            {
                                dist = 0;
                            }
                            else
                            {
                                dist = Convert.ToInt16(tmp_w);
                            }
                            if (dist == 0) dist = Convert.ToInt16(x.Attributes[i].Value);
                        }
                        break;

                    case "skincommand":
                        skincommand = x.Attributes[i].Value;
                        break;

                    case "skincommand_out":
                        skincommand_out = x.Attributes[i].Value;
                        break;
                        
                }
            }
         }

        public bool IsEq(double Lat, double Lon, int Hdg)
        {
            if ((HdgCompare(Hdg, hdg, deltahdg) || HdgCompare(Hdg, hdg_back, deltahdg)) && HdgCompare(Hdg, this.Hdg(Lat, Lon), deltahdg))
            {
                real_dist = LatLonDistance(Lat, Lon, lattitude, longitude);
                if (real_dist <= dist) return true; else return false;
            }
            else return false;
        }

        public bool IsEq2(double Lat, double Lon, int Hdg)
        {
            double current_width;
            double a_hdg_rad;
            int current_hdg = this.Hdg(Lat, Lon);
            real_dist = LatLonDistance(Lat, Lon, lattitude, longitude);
            int a_hdg = Math.Abs(current_hdg - Hdg) ;
            if (a_hdg > 180) a_hdg = 360 - a_hdg;
            if (a_hdg <= 90)
            {
                a_hdg_rad = a_hdg / 57.295779513;
                current_width = real_dist * Math.Sin(a_hdg_rad);
                if ((real_dist * Math.Cos(a_hdg_rad) <= (double)dist) && (current_width <= (double)width)) return true; else return false;
             }
            else return false;
        }

        public bool IsEq3(double Lat, double Lon)
        {
             real_dist = LatLonDistance(Lat, Lon, lattitude, longitude);
             if (real_dist <= dist) return true; else return false;
        }

        private bool HdgCompare(int Hdg, int CurrentHdg, int DeltaHdg)
        {
            int tmp_hdg = Math.Abs(CurrentHdg - Hdg);
            if (tmp_hdg > 180) tmp_hdg = 360 - tmp_hdg;
            if (tmp_hdg <= DeltaHdg) return true; else return false;
        }

        // Возвращает расстояние между двумя точками
        private double LatLonDistance(double dbLat1, double dbLon1, double dbLat2, double dbLon2)
        {

            long loRadiusOfEarth = 6367000;
            double dbDeltaLat;
            double dbDeltaLon;
            double dbTemp;
            double dbTemp2;

            if (dbLon2 == 0 & dbLat2 == 0)
            {
                return 0;
            }

            dbDeltaLon = AsRadians(dbLon2) - AsRadians(dbLon1);
            dbDeltaLat = AsRadians(dbLat2) - AsRadians(dbLat1);

            dbTemp = Sin2(dbDeltaLat / 2) + Math.Cos(AsRadians(dbLat1)) * Math.Cos(AsRadians(dbLat2)) * Sin2(dbDeltaLon / 2);
            dbTemp2 = 2 * Arcsin(Math.Min(1, Math.Sqrt(dbTemp)));
            return loRadiusOfEarth * dbTemp2;
        }
        private double Arcsin(double x)
        {
            return Math.Atan(x / Math.Sqrt(-x * x + 1));
        }
        private double AsRadians(double pDb_Degrees)
        {
            return pDb_Degrees * (3.14159265358979 / 180);
        }
        private double Sin2(double x)
        {
            return (1 - Math.Cos(2 * x)) / 2;
        }
        
        // Озвращает азимут от точки Lat,Lon  на объект Point
        public int Hdg(double Lat, double Lon)
        {
            const  double PI = 3.14159265358979;
            double dY = Lat - lattitude; // 
            double dX = Lon - longitude; //
            double Hdg_tmp=0;

            if ( dX == 0 && dY < 0 )
            {
                Hdg_tmp = 0;
            }
            else
            {
                if ( dX < 0 && dY == 0 )
                {
                    Hdg_tmp = PI * 0.5;
                }
                else
                {
                    if ( dX == 0 && dY > 0 )
                    {
                        Hdg_tmp = PI;
                    }
                    else
                    {
                        if (dX > 0 && dY == 0)
                        {
                            Hdg_tmp = PI * 1.5;
                        }
                        else
                        {
                            if ( dX < 0 && dY < 0 )
                            {
                                Hdg_tmp = PI * 0.5 - Math.Atan(Math.Abs(dY/dX));
                            }
                            else
                            {
                                if ( dX < 0 && dY > 0 )
                                {
                                    Hdg_tmp = PI * 0.5 + Math.Atan(Math.Abs(dY/dX));
                                }
                                else
                                {
                                    if ( dX > 0 && dY > 0 )
                                    {
                                        Hdg_tmp = PI * 1.5 - Math.Atan(Math.Abs(dY/dX));
                                    }
                                    else
                                    {
                                        if ( dX > 0 && dY < 0 )
                                        {
                                            Hdg_tmp = PI * 1.5 + Math.Atan(Math.Abs(dY/dX));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Hdg_tmp = Hdg_tmp * 57.295779513;
            return (int)Math.Round(Hdg_tmp);
        }
    }

    public class Id
    {
        public string name;
        public string value;

        public Id(string id_name, string id_value)
        {
            name = id_name; value = id_value;
        }
    }
    static public class IdDistList
    {
        static public bool Created = false;
        static public System.Collections.ArrayList point_idDist_list;

        static public void SetIdList()
        {
            point_idDist_list = new System.Collections.ArrayList(); Created = true;
        }

        static public string GetValue(string id_name)
        {

            for (int i = 0; i < point_idDist_list.Count; i++)
            {
                if (((Id)point_idDist_list[i]).name == id_name) 
                {
                    return ((Id)point_idDist_list[i]).value;
                }
            }
            string id_value = SDK.GetInfo("=$" + id_name + "$");
            point_idDist_list.Add(new Id(id_name, id_value));
            return id_value;
        }
    }
    static public class IdWidthList
    {
        static public bool Created = false;
        static public System.Collections.ArrayList point_idWidth_list;

        static public void SetIdWidthList()
        {
            point_idWidth_list = new System.Collections.ArrayList(); Created = true;
        }

        static public string GetValue(string id_name)
        {

            for (int i = 0; i < point_idWidth_list.Count; i++)
            {
                if (((Id)point_idWidth_list[i]).name == id_name)
                {
                    return ((Id)point_idWidth_list[i]).value;
                }
            }
            string id_value = SDK.GetInfo("=$" + id_name + "$");
            point_idWidth_list.Add(new Id(id_name, id_value));
            return id_value;
        }
    }
    public class IniFile
    {
        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                 string key, string def, StringBuilder retVal,
            int size, string filePath);

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <PARAM name="INIPath">Путь к INI-файлу</PARAM>
        public IniFile(string INIPath)
        {
            path = INIPath;
        }
        /// <summary>
        /// Запись данных в INI-файл
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// Название секции
        /// <PARAM name="Key"></PARAM>
        /// Имя ключа
        /// <PARAM name="Value"></PARAM>
        /// Значение
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        /// <summary>
        /// Чтение данных из INI-файла
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <returns>Значение заданного ключа</returns>
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this.path);
            return temp.ToString();
        }
    }
}
