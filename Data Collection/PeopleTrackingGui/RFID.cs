﻿using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Timers;
using Impinj.OctaneSdk;
using System.Windows;
using System.Reflection;
using System.Collections.Generic;
using PeopleTrackingGui;
using NodaTime;
using MathNet.Numerics.LinearAlgebra;



namespace RFID_Beta_5
{
    class RFID
    {
        public static bool IsRFIDOpen = false;

        public static int antennaNum = 4;

        public readonly static long TIME_DIFFERENCE = 4 * 3600 * 1000 + 18 * 60 * 1000 + 16 * 1000;



        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();
        public static int[] tags = new int[4];
        Boolean START_TIME_FLAG = false;
        static double startTime;

        Timer RFID_Update_Timer = new Timer();
        static double Ant_1 = 0;
        static double Ant_2 = 0;
        static double Ant_3 = 0;
        static double Ant_4 = 0;
        
        static double Ant_count_1 = 0;
        static double Ant_count_2 = 0;
        static double Ant_count_3 = 0;
        static double Ant_count_4 = 0;

        // Double for previous phase angle
        static double phaseAngle_1_Past = 0;
        static double phaseAngle_2_Past = 0;
        static double phaseAngle_3_Past = 0;
        static double phaseAngle_4_Past = 0;

        // double for previous timestamp
        static double timestamp_1_Past = 0;
        static double timestamp_2_Past = 0;
        static double timestamp_3_Past = 0;
        static double timestamp_4_Past = 0;

        // double for default API value
        static double API_DFS_1 = 0;
        static double API_DFS_2 = 0;
        static double API_DFS_3 = 0;
        static double API_DFS_4 = 0;

        // double for default API value
        static double channel_1_Past = 0;
        static double channel_2_Past = 0;
        static double channel_3_Past = 0;
        static double channel_4_Past = 0;

        //for test
        static double delta_Pahse_Angle_1_tmp; static double delta_Pahse_Angle_2_tmp; static double delta_Pahse_Angle_3_tmp; static double delta_Pahse_Angle_4_tmp;
        static double delta_Timestamp_1_tmp; static double delta_Timestamp_2_tmp; static double delta_Timestamp_3_tmp; static double delta_Timestamp_4_tmp;
        static double tmp_phase;
        static double timeWriteFile;
        static double Ant_1_previous; static double Ant_2_previous; static double Ant_3_previous; static double Ant_4_previous;


        static int Row_count = 1;

        public static string StartTime;
        public static string fileName;

        public static Dictionary<string, RfidVelocity> RfidVelocityList;

        public static Dictionary<string, DateTime> tagLastTime;

        public static int frameCounter = 0;

        public static int totalCounter = 0;
        

        public static DateTime lastTagTime;

        public List<Matrix<double>> tagMatrix;
        public Matrix<double> matrix;
        public Dictionary<String,double[]> holdingTags;

        private System.Timers.Timer generateRssMap;
        private static readonly double GENERATE_RSSMAP_INTERVAL = 1000 * 3; // Millisecond
        public PeopleTrackingGui.MainWindow m;
        public int rxSensitivity = -80;
        public int maxdbm = -30;



        public RFID(MainWindow m)
        {
            
            string dir = @"" + PeopleTrackingGui.Properties.Resources.DirectoryRFID;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            this.m = m;
            StartTime = DateTime.Now.ToString("M-d-yyyy_HH-mm-ss");
            fileName = @dir + "RFID_" + StartTime + ".txt";

            RfidVelocityList = new Dictionary<string, RfidVelocity>();
            tagLastTime = new Dictionary<string, DateTime>();

            tagMatrix = new List<Matrix<double>>();
            initializeRss();


            generateRssMap = new System.Timers.Timer();
            generateRssMap.AutoReset = true;
            generateRssMap.Elapsed += generateRssMap_Elapsed;
            generateRssMap.Interval = GENERATE_RSSMAP_INTERVAL;
            generateRssMap.Enabled = true;

            Console.WriteLine("RFID_constructed");

            
        }

        private Matrix<double> rssMapGenerater (double[] arr) {
            Matrix<double> rssMap = Matrix<double>.Build.Dense(TagIds.width, TagIds.height, 0);
            for (int j = 0; j < arr.Length; j++)
            {

                for (int k = 0; k < rssMap.RowCount; k++)
                {

                    for (int w = 0; w < rssMap.ColumnCount; w++)
                    {

                        if (Math.Sqrt(Math.Pow((w - TagIds.antennaLocationsX[j]), 2) + Math.Pow((k - TagIds.antennaLocationsY[j]), 2)) < 170)
                        {
                            rssMap[k, w] += arr[j];
                        }
                    }

                }
            }
            rssMap *= (255.0 / (maxdbm - rxSensitivity));
            return rssMap;
        }

        private void generateRssMap_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {

            Console.WriteLine("start generating");

            List<Matrix<double>> RSSMaps = new List<Matrix<double>>();

            foreach (KeyValuePair<string, double[]> hold in holdingTags) {

                var rssMap = rssMapGenerater(hold.Value);
                ActivityRecognition.Record.RecodrdHoldingRss(rssMap,hold.Key);

            }

            

            for (int i = 0; i < matrix.RowCount; i++) {

                //Matrix<double> rssMap=Matrix<double>.Build.Dense(TagIds.width,TagIds.height, 0);
                //for (int j = 0; j < matrix.ColumnCount; j++) {

                //    for (int k = 0; k < rssMap.RowCount; k++) {

                //        for (int w = 0; w < rssMap.ColumnCount; w++) {

                //            if (Math.Sqrt(Math.Pow((w-TagIds.antennaLocationsX[j]),2)+ Math.Pow((k - TagIds.antennaLocationsY[j]), 2)) < 170) {
                //                rssMap[k, w] += matrix[i, j];
                //            }
                //        }

                //    }
                //}
                //rssMap *= (255.0 / (maxdbm-rxSensitivity));

                double[] arr=ActivityRecognition.Transformation.getMatrixRow(matrix, i);

                var rssMap = rssMapGenerater(arr);
                RSSMaps.Add(rssMap);
                
            }



            Matrix<double> normalizeRssMap = Matrix<double>.Build.Dense(TagIds.width, TagIds.height, 0);

            foreach(var map in RSSMaps)
            {
                normalizeRssMap += map;
            }

            ActivityRecognition.Record.RecordRssMapFiles(RSSMaps);
            //m.drawRssMap_(RSSMaps[3]);
            m.drawRssMap_(MatrixUtils.NormalizeMatrix(normalizeRssMap) * 255.0);



            initializeRss();

        }
        private void initializeRss() {

            matrix = Matrix<double>.Build.Dense(TagIds.recordedTags.Length, antennaNum, 0); 
            holdingTags = new Dictionary<string, double[]>();

        }

        public void run()
        {
            try
            {
                reader.Connect(SolutionConstants.ReaderHostname);

                FeatureSet features = reader.QueryFeatureSet();
                Settings settings = reader.QueryDefaultSettings();
                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludePeakRssi = true;
                settings.Report.IncludeDopplerFrequency = true;
                settings.Report.IncludePhaseAngle = true;
                settings.Report.IncludeLastSeenTime = true;
                settings.Report.IncludeChannel = true;

                settings.ReaderMode = ReaderMode.DenseReaderM4;
                settings.SearchMode = SearchMode.DualTarget;
                settings.Session = 2;
                settings.TagPopulationEstimate = 30;
                settings.Antennas.DisableAll(); 
                settings.Antennas.GetAntenna(1).IsEnabled = true;
                settings.Antennas.GetAntenna(2).IsEnabled = true;
                settings.Antennas.GetAntenna(3).IsEnabled = true;
                settings.Antennas.GetAntenna(4).IsEnabled = true;

                settings.Antennas.GetAntenna(1).MaxTxPower = true;
                settings.Antennas.GetAntenna(1).MaxRxSensitivity = true;
                settings.Antennas.GetAntenna(2).MaxTxPower = true;
                settings.Antennas.GetAntenna(2).MaxRxSensitivity = true;
                settings.Antennas.GetAntenna(3).MaxTxPower = true;
                settings.Antennas.GetAntenna(3).MaxRxSensitivity = true;
                settings.Antennas.GetAntenna(4).MaxTxPower = true;
                settings.Antennas.GetAntenna(4).MaxRxSensitivity = true;

                settings.Antennas.TxPowerMax = true;
                //settings.Antennas.TxPowerInDbm = 31;
                settings.Antennas.RxSensitivityInDbm = rxSensitivity;

                //settings.Filters.TagFilter1.MemoryBank = MemoryBank.Epc;
                //// Start matching at the third word (bit 32), since the 
                //// first two words of the EPC memory bank are the
                //// CRC and control bits. BitPointers.Epc is a helper
                //// enumeration you can use, so you don't have to remember this.
                //settings.Filters.TagFilter1.BitPointer = BitPointers.Epc;
                //// Only match tags with EPCs that start with "2222"
                //settings.Filters.TagFilter1.TagMask = "2222";
                //// This filter is 16 bits long (one word).
                //settings.Filters.TagFilter1.BitCount = 16;

                //settings.Filters.Mode = TagFilterMode.OnlyFilter1;

                reader.ApplySettings(settings);
                reader.TagsReported += OnTagsReported;
                reader.Start();

                // Start timer
                //RFID_Update_Timer.Elapsed += new ElapsedEventHandler(Update_RFID_Status);
                //RFID_Update_Timer.Interval = 0.5 * 1000;
                //RFID_Update_Timer.Enabled = true;

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                //reader.Stop();

            }
            catch (OctaneSdkException e)
            {
                // Handle Octane SDK errors.
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                // Handle other .NET errors.
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }


        // Update Excel file
        private void Update_RFID_Status(object source, ElapsedEventArgs e)
        {
            if (Ant_count_1 == 0) Ant_1 = 0;
            else Ant_1 = Ant_1 / Ant_count_1;

            if (Ant_count_2 == 0) Ant_2 = 0;
            else Ant_2 = Ant_2 / Ant_count_2;

            if (Ant_count_3 == 0) Ant_3 = 0;
            else Ant_3 = Ant_3 / Ant_count_3;

            if (Ant_count_4 == 0) Ant_4 = 0;
            else Ant_4 = Ant_4 / Ant_count_4;

            API_DFS_1 = API_DFS_1 / (Ant_count_1 + 1);
            API_DFS_2 = API_DFS_2 / (Ant_count_2 + 1);
            API_DFS_3 = API_DFS_3 / (Ant_count_3 + 1);
            API_DFS_4 = API_DFS_4 / (Ant_count_4 + 1);


            Console.WriteLine(Row_count);
            Ant_1 = 0; Ant_2 = 0; Ant_3 = 0; Ant_4 = 0;
            Ant_count_1 = 0; Ant_count_2 = 0; Ant_count_3 = 0; Ant_count_4 = 0;
            API_DFS_1 = 0; API_DFS_2 = 0; API_DFS_3 = 0; API_DFS_4 = 0;

            if (Row_count > 10000)
            {
                reader.Stop();
                reader.Disconnect();
                Environment.Exit(0);
            }
        }


        // 
        void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            // This event handler is called asynchronously 
            // when tag reports are available.
            // Loop through each tag in the report 
            // Print the data.
            // and write them to the txt
            foreach (Tag tag in report)
            {
                if (!START_TIME_FLAG)
                {
                    startTime = Convert.ToDouble(tag.LastSeenTime.ToString());
                    START_TIME_FLAG = true;
                }
                //tag.Epc.ToString() == "3008 33B2 DDD9 0140 0000 0000"    
                //Console.WriteLine(tag.Epc.ToString());
                if (tag.Epc.ToString() == "0000 0000 0000 0000 0000 020D" || tag.Epc.ToString() == "0000 0000 0000 0000 0000 020F" || false)
                {


                    if (Array.FindIndex(TagIds.holdingtags, id => id.Equals(tag.Epc.ToString())) > -1)
                    {
                        int antenna = tag.AntennaPortNumber - 1;
                        if (!holdingTags.ContainsKey(tag.Epc.ToString()))
                        {
                            
                            holdingTags.Add(tag.Epc.ToString(), new double[4]);
                            holdingTags[tag.Epc.ToString()][antenna] = tag.PeakRssiInDbm - rxSensitivity;
                        }
                        else {
                            if (holdingTags[tag.Epc.ToString()][antenna] == 0)
                            {
                                holdingTags[tag.Epc.ToString()][antenna] = tag.PeakRssiInDbm - rxSensitivity;
                            }
                            else {
                                double originalvalue = holdingTags[tag.Epc.ToString()][antenna];
                                holdingTags[tag.Epc.ToString()][antenna] = (originalvalue + tag.PeakRssiInDbm - rxSensitivity) / 2;
                            }
                            
                        }
                    }
                    else {
                        int tag_index = Array.FindIndex(TagIds.recordedTags, id => id.Equals(tag.Epc.ToString()));

                        if (tag_index != -1)
                        {
                            int antenna = tag.AntennaPortNumber - 1;
                            if (matrix[tag_index, antenna] == 0)
                            {
                                matrix[tag_index, antenna] = tag.PeakRssiInDbm - rxSensitivity;
                            }
                            else
                            {
                                double originalvalue = matrix[tag_index, antenna];
                                matrix[tag_index, antenna] = (originalvalue + tag.PeakRssiInDbm - rxSensitivity) / 2;
                            }
                        }
                    }





                     

                    if (tag.AntennaPortNumber == 1)
                    {
                        if (Row_count == 1)
                        {
                            phaseAngle_1_Past = tag.PhaseAngleInRadians;
                            timestamp_1_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_1 = 0;
                            Ant_1_previous = 0;
                            tmp_phase = 0;
                            API_DFS_1 = tag.RfDopplerFrequency;
                            channel_1_Past = tag.ChannelInMhz;
                            Row_count++;

                        }
                        else if (tag.ChannelInMhz == channel_1_Past)
                        {

                            delta_Pahse_Angle_1_tmp = (tag.PhaseAngleInRadians - phaseAngle_1_Past);
                            delta_Timestamp_1_tmp = (Convert.ToDouble(tag.LastSeenTime.ToString()) - timestamp_1_Past) / 1000000;
                            tmp_phase = phaseAngle_1_Past;
                            phaseAngle_1_Past = tag.PhaseAngleInRadians;
                            timestamp_1_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_1 = (1 / (4 * Math.PI)) * (delta_Pahse_Angle_1_tmp / delta_Timestamp_1_tmp);
                            API_DFS_1 = tag.RfDopplerFrequency;
                            Ant_count_1++;
                            timeWriteFile = Convert.ToDouble(tag.LastSeenTime.ToString());
                            if (Math.Abs(Ant_1 - 0) < 5 && Math.Abs(Ant_1 - Ant_1_previous) <= 5)
                            {
                                //Write_file(1, Ant_1, timeWriteFile);

                                Write_file(tag.Epc.ToString(), tag.AntennaPortNumber, tag.PeakRssiInDbm, Convert.ToDouble(tag.LastSeenTime.ToString()));
                                
                                Ant_1_previous = Ant_1;
                                Row_count++;
                            }
                            else {
                                //reject
                            }
                        }
                        else {
                            //Console.WriteLine("Hopping here");
                            tmp_phase = 0;
                            phaseAngle_1_Past = tag.PhaseAngleInRadians;
                            timestamp_1_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            channel_1_Past = tag.ChannelInMhz;

                        }
                    }
                    else if (tag.AntennaPortNumber == 2)
                    {
                        Console.WriteLine(tag.Epc.ToString());
                        if (Row_count == 1)
                        {
                            phaseAngle_2_Past = tag.PhaseAngleInRadians;
                            timestamp_2_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_2 = 0;
                            Ant_2_previous = 0;
                            tmp_phase = 0;
                            API_DFS_2 = tag.RfDopplerFrequency;
                            channel_2_Past = tag.ChannelInMhz;
                            Row_count++;

                        }
                        else if (tag.ChannelInMhz == channel_2_Past)
                        {

                            delta_Pahse_Angle_2_tmp = (tag.PhaseAngleInRadians - phaseAngle_2_Past);
                            delta_Timestamp_2_tmp = (Convert.ToDouble(tag.LastSeenTime.ToString()) - timestamp_2_Past) / 1000000;
                            tmp_phase = phaseAngle_2_Past;
                            phaseAngle_2_Past = tag.PhaseAngleInRadians;
                            timestamp_2_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_2 = (1 / (4 * Math.PI)) * (delta_Pahse_Angle_2_tmp / delta_Timestamp_2_tmp);
                            API_DFS_2 = tag.RfDopplerFrequency;
                            Ant_count_2++;
                            timeWriteFile = Convert.ToDouble(tag.LastSeenTime.ToString());
                            if (Math.Abs(Ant_2 - 0) < 5 && Math.Abs(Ant_2 - Ant_2_previous) <= 5)
                            {
                                RFID_Beta_5.Velocity v = RFID_Beta_5.Velocity.getVelocity();
                                double velocity = v.v_calculator(tag.ChannelInMhz, Ant_2);
                                //Write_file(tag.Epc.ToString(), Ant_2, API_DFS_2, velocity, Convert.ToDouble(tag.LastSeenTime.ToString()));
                                Write_file(tag.Epc.ToString(), tag.AntennaPortNumber, tag.PeakRssiInDbm, Convert.ToDouble(tag.LastSeenTime.ToString()));
                                //RfidVelocityList.Add(tag.Epc.ToString(),)

                                frameCounter++;

                                totalCounter++;


                                //RecordTagVelocity(tag.Epc.ToString(), velocity, Convert.ToDouble(tag.LastSeenTime.ToString()));

                                //_formatEpc(tag.LastSeenTime.ToString());

                                Ant_2_previous = Ant_2;
                                Row_count++;
                            }
                            else {
                                //reject
                            }
                        }
                        else {
                            //Console.WriteLine("Hopping here");
                            tmp_phase = 0;
                            phaseAngle_2_Past = tag.PhaseAngleInRadians;
                            timestamp_2_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            channel_2_Past = tag.ChannelInMhz;

                        }
                    }
                    else if (tag.AntennaPortNumber == 3)
                    {
                        if (Row_count == 1)
                        {
                            phaseAngle_2_Past = tag.PhaseAngleInRadians;
                            timestamp_2_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_2 = 0;
                            Ant_2_previous = 0;
                            tmp_phase = 0;
                            API_DFS_2 = tag.RfDopplerFrequency;
                            channel_2_Past = tag.ChannelInMhz;
                            Row_count++;

                        }
                        else if (tag.ChannelInMhz == channel_2_Past)
                        {

                            delta_Pahse_Angle_3_tmp = (tag.PhaseAngleInRadians - phaseAngle_3_Past);
                            delta_Timestamp_3_tmp = (Convert.ToDouble(tag.LastSeenTime.ToString()) - timestamp_3_Past) / 1000000;
                            tmp_phase = phaseAngle_3_Past;
                            phaseAngle_3_Past = tag.PhaseAngleInRadians;
                            timestamp_3_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            Ant_3 = (1 / (4 * Math.PI)) * (delta_Pahse_Angle_3_tmp / delta_Timestamp_3_tmp);
                            API_DFS_3 = tag.RfDopplerFrequency;
                            Ant_count_3++;
                            timeWriteFile = Convert.ToDouble(tag.LastSeenTime.ToString());
                            if (Math.Abs(Ant_3 - 0) < 5 && Math.Abs(Ant_3 - Ant_3_previous) <= 5)
                            {
                                //Write_file(3, Ant_3, timeWriteFile);
                                Write_file(tag.Epc.ToString(), tag.AntennaPortNumber, tag.PeakRssiInDbm,Convert.ToDouble(tag.LastSeenTime.ToString()));
                                Ant_3_previous = Ant_3;
                                Row_count++;
                            }
                            else {
                                //reject
                            }
                        }
                        else {
                            //Console.WriteLine("Hopping here");
                            tmp_phase = 0;
                            phaseAngle_2_Past = tag.PhaseAngleInRadians;
                            timestamp_2_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            channel_2_Past = tag.ChannelInMhz;

                        }
                    }
                    else if (tag.AntennaPortNumber == 4)
                    {
                        if (Row_count == 1)
                        {
                            phaseAngle_4_Past = tag.PhaseAngleInRadians;
                            timestamp_4_Past = Convert.ToDouble(tag.LastSeenTime.ToString());
                            API_DFS_4 += tag.RfDopplerFrequency;
                            channel_4_Past = tag.ChannelInMhz;
                        }
                        else {
                            if (Math.Abs(tag.ChannelInMhz - channel_4_Past) == 0)
                            {
                                double delta_Pahse_Angle = (tag.PhaseAngleInRadians - phaseAngle_4_Past);
                                double delta_Timestamp = (Convert.ToDouble(tag.LastSeenTime.ToString()) - timestamp_4_Past) / 1000000;
                                phaseAngle_4_Past = tag.PhaseAngleInRadians;
                                timestamp_4_Past = Convert.ToDouble(tag.LastSeenTime.ToString());

                                Ant_4 = Ant_4 + (1 / (4 * 3.1416)) * (delta_Pahse_Angle / delta_Timestamp);
                                API_DFS_4 += tag.RfDopplerFrequency;
                                Ant_count_4++;

                                Write_file(tag.Epc.ToString(), tag.AntennaPortNumber, tag.PeakRssiInDbm, Convert.ToDouble(tag.LastSeenTime.ToString()));

                            }
                            channel_4_Past = tag.ChannelInMhz;
                        }
                    }
                }

            }
        }

        private void RecordTagVelocity(string tagId, double velocity, double time)
        {
            var timeReal = GetMbtaDateTime(time);
            if (RfidVelocityList.ContainsKey(tagId) && tagLastTime.ContainsKey(tagId))
            {

                var lastTime = tagLastTime[tagId];

                double distance = Convert.ToDouble((timeReal - lastTime).Milliseconds) * velocity / 10;

                if (distance >= 4 || distance <= -4) {
                    distance = 0;
                }

                RfidVelocityList[tagId].velocity.Add(timeReal, distance);


                tagLastTime[tagId] = timeReal;


            }
            else {
                var rfidVel = new RfidVelocity();
                rfidVel.velocity.Add(timeReal, 0);
                RfidVelocityList.Add(tagId, rfidVel);
                tagLastTime.Add(tagId, timeReal);
            }
            lastTagTime = timeReal;
        }

        // Write into a txt file
        public static void Write_file(string tagId, double Ant_2, double API_DFS_2, double velocity, double time)
        {
            FileStream file = new FileStream(fileName, FileMode.Append);
            StreamWriter writer = new StreamWriter(file, Encoding.Default);

            var time2 = GetMbtaDateTime(time);

            writer.WriteLine("{0},{1},{2},{3},{4}",
            tagId, Ant_2, API_DFS_2, velocity, time2.ToString(@"yyyy-MM-dd HH:mm:ss:fff"));

            //Console.WriteLine("{0},{1},{2},{3},{4}",
            //tagId, Ant_2, API_DFS_2, velocity, time2.ToString(@"yyyy-MM-dd HH:mm:ss:fff"));

            writer.Close();
            file.Close();
        }


        public static void Write_file(String tagId,double port,double rss,double time) {
            FileStream file = new FileStream(fileName, FileMode.Append);
            StreamWriter writer = new StreamWriter(file, Encoding.Default);

            var time2 = GetMbtaDateTime(time);

            writer.WriteLine("{0},{1},{2},{3}",tagId, port, rss, time2.ToString(@"yyyy-MM-dd HH:mm:ss:fff"));

            //Console.WriteLine("{0},{1},{2},{3},{4}",
            //tagId, Ant_2, API_DFS_2, velocity, time2.ToString(@"yyyy-MM-dd HH:mm:ss:fff"));

            writer.Close();
            file.Close();
        }

        public static DateTime FromUnixTime(long unixTime)
        {
            unixTime = unixTime / 1000;
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
            return epoch.AddMilliseconds(unixTime - TIME_DIFFERENCE);
        }

        public static DateTime GetMbtaDateTime(double unixTimestamp)
        {
            unixTimestamp = unixTimestamp / 1000;
            DateTimeZone mbtaTimeZone = DateTimeZoneProviders.Tzdb["America/New_York"];
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var mbtaEpochTime = epoch.AddMilliseconds(unixTimestamp);
            var instant = Instant.FromUtc(mbtaEpochTime.Year, mbtaEpochTime.Month,
                mbtaEpochTime.Day, mbtaEpochTime.Hour, mbtaEpochTime.Minute, mbtaEpochTime.Second);
            var nodaTime = instant.InZone(mbtaTimeZone);

            return nodaTime.ToDateTimeUnspecified().AddMilliseconds(mbtaEpochTime.Millisecond);
            //return new DateTime();
        }



        public void RFID_stop_recording()
        {
            RFID_Update_Timer.Close();
            reader.Stop();
            reader.Disconnect();
        }

        // modified for multiple reader intances
        public void Stop()
        {

            if (reader.IsConnected)
            {
                try
                {
                    reader.Stop();
                    reader.Disconnect();
                    Console.WriteLine(reader.Name.ToString() + "stoped in RFID");
                    //ErrorLog.LogSystemEvent(reader.Name.ToString() + "stoped in RFID");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    //ErrorLog.LogSystemError(e.Message);
                }
                finally
                {
                    //if (reader.IsConnected)
                    //{
                    //    reader.Stop();
                    //    Console.WriteLine(reader.Name.ToString() + "stoped in RFID");
                    //    ErrorLog.LogSystemEvent(reader.Name.ToString() + "stoped in RFID");
                    //}
                }

            }
        }
        //ErrorLog.LogSystemEvent("All RFID iS Stopped");
        //ErrorLog.EndLog();

    }
}
