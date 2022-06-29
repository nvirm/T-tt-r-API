using System;
using System.Threading.Tasks;
using GoogleApi;
using GoogleApi.Entities.Maps.Geocoding.Location.Request;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Globalization;
using Geolocation;
using System.Linq;

namespace JaateloautoAPI.Helpers
{
    public class JaateloHelper
    {
        /* Maintenance parts lock the thread, 
         * but are used only in Off-hours. 
         * 
         * On a busy day, binding routes and route stops
         * Might take several minutes.
         * 
         * Since this API doesn't use a DB,
         * thought it was an acceptable solution.
         */

        //--HTTP Tool--
        private async Task<string> httpTool(string mode, string inputstr = "")
        {
            var ret = "";

            var root_url = "https://api.paikannuspalvelu.fi/v1/public/";
            var data_key = "eVCPeXjwNNYMfGZ6sGwzLcFB462FTfgJ5TqAX7nf";
            var author = "rest@paikannuspalvelu.fi";
            var format = "geojson";
            var req = new HttpClient();

            try
            {

                req.Timeout = new TimeSpan(0, 0, 20);
                req.DefaultRequestHeaders.Add("User-Agent", "JaateloautoCustomAPI/1.0.0");

                switch (mode)
                {
                    case "getAllRoutes":
                        req.BaseAddress = new Uri(root_url + "route/" + "?data_key=" + data_key + "&author=" + author + "&format=" + format);
                        break;
                    case "getTodayLocations":
                        req.BaseAddress = new Uri(root_url + "route/today/stop/" + "?data_key=" + data_key + "&author=" + author + "&format=" + format);
                        break;
                    case "getAllVehicles":
                        req.BaseAddress = new Uri(root_url + "device/" + "?data_key=" + data_key + "&author=" + author + "&format=" + format);
                        break;
                    case "getVehicleDetails":
                        req.BaseAddress = new Uri(root_url + "device/" + inputstr + "/?data_key=" + data_key + "&author=" + author + "&format=" + format);
                        break;
                    case "searchRoute":
                        req.BaseAddress = new Uri(root_url + "route/search/" + "?routes=" + inputstr + "&data_key=" + data_key + "&author=" + author + "&format=" + format);
                        break;
                    default:
                        return ret;
                }
                var resp = new HttpResponseMessage();
                resp = await req.GetAsync(req.BaseAddress);
                if (resp.IsSuccessStatusCode)
                {
                    ret = await resp.Content.ReadAsStringAsync();
                }
                else
                {
                    var testRet = resp.Content.ReadAsStringAsync();
                    ret = "";
                }
            }
            catch
            {
                ret = "ERR";
                req.Dispose();
                return ret;
            }


            req.Dispose();
            return ret;
        }
        //--HTTP Tool END--

        //--MAINTENANCE & DAILY DATA GATHERING--
        public async Task<string> getBaseInfo()
        {
            //Initialize data
            var ret = "";
            VRoutes.Maintenance = true;
            var part1 = Task.Run(async () => await getBaseInfo_1()).Result; //GET ALL ROUTES
            if (part1 == "OK")
            {
                Console.WriteLine("PART1--COMPLETED");
                var part2 = Task.Run(async () => await getBaseInfo_2()).Result; //GET TODAY STOPS
                if (part2 == "OK")
                {
                    Console.WriteLine("PART2--COMPLETED");
                    var part3 = Task.Run(async () => await getBaseInfo_3()).Result; //MATCH TODAY STOPS TO ROUTES
                    if (part3 == "OK")
                    {
                        Console.WriteLine("PART3--COMPLETED");
                        var part4 = Task.Run(async () => await getBaseInfo_4()).Result; //GET VEHICLES IN FIELD
                        if (part4 == "OK")
                        {
                            Console.WriteLine("PART4--COMPLETED");
                            VRoutes.DataUpdated = DateTime.Now; //Update BaseData Datetime
                            VRoutes.InitialRunDone = true;
                            VRoutes.Maintenance = false;
                            ret = "OK";
                        }
                        else
                        {
                            ret = "PART4ERR";
                        }
                    }
                    else
                    {
                        ret = "PART3ERR";
                    }
                }
                else
                {
                    ret = "PART2ERR";
                }
            }
            else
            {
                ret = "PART1ERR";
            }
            return ret;
        }
        public async Task<string> getBaseInfo_1()
        {
            var ret = "";
            //GET ALL ROUTES
            var reqRoutes = await httpTool("getAllRoutes");
            if (reqRoutes != null || reqRoutes != "")
            {
                try
                {
                    var routeArr = JsonSerializer.Deserialize<List<Route>>(reqRoutes, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                    //Map Routes
                    foreach (var rp in routeArr)
                    {

                        //Check if exists
                        var vrrIndex = -1;
                        if (VRoutes.VehicleRoutes != null)
                        {
                            vrrIndex = VRoutes.VehicleRoutes.FindIndex(rr => rr.Id == Convert.ToInt32(rp.Id));
                        }
                        else
                        {
                            VRoutes.VehicleRoutes = new List<VehicleRoute>();
                        }
                        if (vrrIndex != -1)
                        {
                            var target = VRoutes.VehicleRoutes.Find(rr => rr.Id == Convert.ToInt32(rp.Id));
                            target.RouteId = Convert.ToInt32(rp.Id);
                            target.CommonName = rp.Name;
                            if (target.DataUpdated.Date < DateTime.Now.Date)
                            {
                                target.FoundZipTodayList = false;
                                target.FoundZipTodayListConfirmed = false;
                                target.FoundZipTodayListRan = false;
                                target.EstimatedCurrentStop = 0;
                            }
                            target.DataUpdated = DateTime.Now;
                            target.File = rp.File;
                            target.Zips = rp.Zips;
                            if (target.RouteStops == null)
                            {
                                var listrs = new List<RouteStop>();
                                target.RouteStops = listrs;
                            }
                            Console.WriteLine("Updated VehicleRoute-" + target.Id + "-" + target.CommonName);
                        }
                        else
                        {
                            var vr = new VehicleRoute();
                            vr.Id = Convert.ToInt32(rp.Id);
                            vr.RouteId = Convert.ToInt32(rp.Id);
                            vr.CommonName = rp.Name;
                            vr.DataUpdated = DateTime.Now;
                            vr.FoundZipTodayList = false;
                            vr.FoundZipTodayListConfirmed = false;
                            vr.FoundZipTodayListRan = false;
                            vr.File = rp.File;
                            vr.Zips = rp.Zips;
                            var listrs = new List<RouteStop>();
                            vr.RouteStops = listrs;
                            VRoutes.VehicleRoutes.Add(vr);
                            Console.WriteLine("Created VehicleRoute-" + vr.Id + "-" + vr.CommonName);
                        }
                    }
                    ret = "OK";
                }
                catch (Exception e)
                {
                    ret = "ERR: " + e.Message.ToString();
                };
            }
            else
            {
                ret = "ERR";
            }
            return ret;
        }

        public async Task<string> getBaseInfo_2()
        {
            var ret = "";

            //GET TODAY LIST, MATCH ZIP CODE TO ROUTE (Possible hit)
            var reqToday = await httpTool("getTodayLocations");
            if (reqToday != null || reqToday != "")
            {
                try
                {
                    var todayArr = JsonSerializer.Deserialize<TodayCollection>(reqToday, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                    var todayZips = new List<string>();
                    //Map Today points
                    for (int i = 0; i < todayArr.Features.Count; i++)
                    {
                        var rs = new RouteStop();
                        rs.Id = i+1;
                        rs.VehicleRouteId = 0;
                        rs.Zip = todayArr.Features[i].Properties.Zip;
                        rs.coordinates = todayArr.Features[i].Geometry.Coordinates; //Order ([0]lng,[1]lat)
                        rs.NameOnRoute = todayArr.Features[i].Properties.Name_On_Route;
                        rs.SequenceOnRoute = todayArr.Features[i].Properties.Sequence_On_Route.Value;
                        rs.Notes = todayArr.Features[i].Properties.Notes;
                        rs.UrlPDFTimetable = "https://www.jatskiauto.com/haku/hae?zip=" + todayArr.Features[i].Properties.Zip;
                        rs.RoutePhoneNo = todayArr.Features[i].Properties.Contact;
                        rs.Schedules = todayArr.Features[i].Properties.Schedules;

                        //Add zip to todayZIPS
                        if (!todayZips.Contains(todayArr.Features[i].Properties.Zip))
                        {
                            todayZips.Add(todayArr.Features[i].Properties.Zip);
                        }
                        //Parse schedules
                        var pDta = new DateTime[] { };
                        foreach (var dt in todayArr.Features[i].Properties.Schedules)
                        {
                            var dtp = dt;
                            if (dt.StartsWith("Tänään klo: "))
                            {
                                var dtpp = dtp.Replace("Tänään klo: ", DateTime.Now.ToString("ddd dd.MM.yyyy", CultureInfo.GetCultureInfo("fi-FI")) + " ");
                                dtp = dtpp;
                            }
                            else
                            {
                                var dtpp = dtp.Replace("klo: ", "");
                                dtp = dtpp;
                            }
                            //Parse to Datetime
                            DateTime dtt = DateTime.Parse(dtp, CultureInfo.GetCultureInfo("fi-Fi"));
                            if (rs.ParsedSchedules == null)
                            {
                                var pss = new List<DateTime>();
                                rs.ParsedSchedules = pss;
                            }
                            rs.ParsedSchedules.Add(dtt);

                        }
                        if (VRouteStops.RouteStops == null)
                        {
                            var rss = new List<RouteStop>();
                            VRouteStops.RouteStops = rss;
                        }
                        VRouteStops.RouteStops.Add(rs); //Add this Stop to memory
                        Console.WriteLine("VRoutestops.RouteStops-Added " + rs.Id + "-" + rs.NameOnRoute);
                    }
                    //Flag routes that could exist in TODAY's Schedule
                    foreach (var tz in todayZips)
                    {
                        var vrList = VRoutes.VehicleRoutes.FindAll(zz => zz.Zips.Contains(tz));
                        foreach (var vr in vrList)
                        {
                            vr.FoundZipTodayList = true;
                            vr.DataUpdated = DateTime.Now;
                            Console.WriteLine(vr.CommonName + "-FoundZipTodayList");
                        }
                    }
                    ret = "OK";
                }
                catch (Exception e)
                {
                    ret = "ERR: " + e.Message.ToString();
                };
            }

            return ret;
        }

        public async Task<string> getBaseInfo_3()
        {
            var ret = "";

            //Go through single route zips
            //Parse Datetimes, confirm 
            try
            {
                var hroutes = VRoutes.VehicleRoutes.FindAll(xx => xx.FoundZipTodayList == true);

                foreach (var hr in hroutes)
                {
                    var sRoute = await httpTool("searchRoute", hr.File);
                    if (sRoute != null || sRoute != "")
                    {
                        var sResults = JsonSerializer.Deserialize<TodayCollection>(sRoute, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        for (int i = 0; i < sResults.Features.Count; i++)
                        {
                            //Parse schedules
                            foreach (var dt in sResults.Features[i].Properties.Schedules)
                            {
                                if (dt.StartsWith("Tänään klo: "))
                                {
                                    //Match on a route today, mark Route active for today
                                    hr.FoundZipTodayList = true;
                                    hr.FoundZipTodayListConfirmed = true;
                                    hr.FoundZipTodayListRan = true;
                                    Console.WriteLine(hr.CommonName + "-ConfirmedFoundTodaylist");
                                    //Find coordinates / Address that matches a RouteStop, map to Route
                                    /*
                                     * VRouteStops.RouteStop. etc .RouteId = hr.Id
                                     * Map All routestops that match, to VRoutes.VehicleRoute.Routestops
                                     */
                                    var rStopL = VRouteStops.RouteStops.Find(xx => xx.NameOnRoute == sResults.Features[i].Properties.Name_On_Route);
                                    if (rStopL != null)
                                    {
                                        rStopL.VehicleRouteId = hr.Id;
                                        if (hr.RouteStops == null)
                                        {
                                            hr.RouteStops = new List<RouteStop>();
                                        }
                                        hr.RouteStops.Add(rStopL);
                                        Console.WriteLine(rStopL.NameOnRoute + "-AddedTo-" + hr.CommonName);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        ret = "ERR";
                    }
                    System.Threading.Thread.Sleep(500);
                }
                //var test1 = VRoutes.VehicleRoutes.FindAll(xx => xx.FoundZipTodayListConfirmed == true);
                //var test2 = VRoutes.VehicleRoutes.FindAll(xx => xx.RouteStops.Count > 0);

                ret = "OK";
            }
            catch (Exception e)
            {
                ret = "ERR: " + e.Message;
            }

            return ret;
        }

        public async Task<string> getBaseInfo_4()
        {
            var ret = "";
            var currVehicles = await httpTool("getAllVehicles");
            if (currVehicles != null || currVehicles != "")
            {
                try
                {

                    var sResults = JsonSerializer.Deserialize<VehicleCollection>(currVehicles, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, });
                    if (VInfos.VehicleInfos == null)
                    {
                        var rss = new List<VehicleInfo>();
                        VInfos.VehicleInfos = rss;
                    }
                    foreach (var vi in sResults.Features)
                    {
                        var viIndex = VInfos.VehicleInfos.FindIndex(rr => rr.Id == vi.Properties.Id);
                        if (viIndex != -1)
                        {
                            //Update
                            var target = VInfos.VehicleInfos.Find(rr => rr.Id == vi.Properties.Id);
                            target.Id = (long)vi.Properties.Id;
                            target.VehicleId = vi.Properties.Id.ToString();
                            target.CommonUpdated = sResults.__Generated;
                            target.DataUpdated = DateTime.Now;
                            target.VehicleInUse = true; //Found in field, should be in use
                            target.Name = vi.Properties.Name; //Integer field
                            target.NotInUseThreshold = 0;
                            target.coordinates = vi.Geometry.Coordinates;

                            Console.WriteLine("Updated VehicleInfo-" + vi.Properties.Id.ToString());

                        }
                        else
                        {
                            //Create new
                            var vii = new VehicleInfo();
                            vii.Id = (long)vi.Properties.Id;
                            vii.VehicleId = vi.Properties.Id.ToString();
                            vii.CommonUpdated = sResults.__Generated;
                            vii.DataUpdated = DateTime.Now;
                            vii.VehicleInUse = true;
                            vii.Name = vi.Properties.Name;
                            vii.NotInUseThreshold = 0;
                            vii.coordinates = vi.Geometry.Coordinates;
                            Console.WriteLine("Created VehicleInfo-" + vi.Properties.Id.ToString());

                            VInfos.VehicleInfos.Add(vii);

                        }
                    }

                    ret = "OK";
                }
                catch (Exception e)
                {
                    ret = "ERR: " + e.Message.ToString();
                }
            }
            else
            {
                ret = "ERR";

            }


            return ret;
        }

        public async Task<string> resetBaseInfo()
        {
            var ret = "";
            VRoutes.Maintenance = true;
            //Reset data daily (Somewhere during 10:00-12:00)
            try
            {
                //VRoutes.VehicleRoutes.Clear();
                //VInfos.VehicleInfos.Clear();
                //VRouteStops.RouteStops.Clear();
                VRoutes.VehicleRoutes = new List<VehicleRoute>();  //Not sure of clearing syntax, try this
                VInfos.VehicleInfos = new List<VehicleInfo>();     //Not sure of clearing syntax, try this
                VRouteStops.RouteStops = new List<RouteStop>();    //Not sure of clearing syntax, try this

                ret = "OK";
            }
            catch (Exception e)
            {
                ret = "ERR: " + e.Message;
            }
            
            return ret;
        }
        //--MAINTENANCE & DAILY DATA GATHERING END--

        //Get current data without fetching--VehicleRoutes
        public async Task<List<VehicleRoute>> getCurrentData()
        {
            var ret = new List<VehicleRoute>();
            if (VRoutes.Maintenance == false)
            {
                ret = VRoutes.VehicleRoutes;
            }
            return ret;
        }
        //Get current data without fetching--VehicleInfos
        public async Task<List<VehicleInfo>> getCurrentVehicles()
        {
            var ret = new List<VehicleInfo>();
            if (VRoutes.Maintenance == false)
            {
                ret = VInfos.VehicleInfos;
            }
            return ret;
        }
        //Get current data without fetching--RouteStops
        public async Task<List<RouteStop>> getCurrentRouteStops()
        {
            var ret = new List<RouteStop>();
            if (VRoutes.Maintenance == false)
            {
                ret = VRouteStops.RouteStops;
            }
            return ret;
        }

        //Get distance between two points, returns meters
        public async Task<double> getDistanceMeters(double[] coords, double[] pointCoords)
        {
            double distance = 0;
            try
            {
                Geolocation.Coordinate origin = new Geolocation.Coordinate(coords[0], coords[1]);
                Geolocation.Coordinate destination = new Geolocation.Coordinate(pointCoords[0], pointCoords[1]);
                distance = GeoCalculator.GetDistance(origin, destination, 1, DistanceUnit.Meters);
                
                Console.WriteLine("DistanceCalc: " + distance);
            }
            catch(Exception e)
            {
                Console.WriteLine("ERR in getDistanceMeters: " + e.Message);
                return distance;
            }

            return distance;
        }

        //Get nearest absolute stop, regardless of timetable
        //TODO: Bind Results to a vehicle when vehicle mapping is complete.
        public async Task<SingleStopDetails> getNearestStop(double[] userCoords, int threshold, bool onlyToday, string suppliedZipCode = "")
        {
            var ret = new SingleStopDetails();
            ret.StatusCode = 404; //Default
            double distanceThreshold = threshold;                    //How close to be considered close enough (meters) (5km?)
            var matchList = new List<Tuple<int,double>>();      //RoutesStop ID, Distance from user -- Newly generated routedata from ZipCode
            var matchListToday = new List<RouteDistObj>();      //Today's data needs route info as well, since it operates on existing data
            if (VRoutes.Maintenance == false)
            {
                //Get CLOSEST Stop when onlyToday = false, Get CLOSEST Stop TODAY when onlyToday = true
                if (onlyToday == true)
                {
                    //Try finding results for today
                    var resToday = VRoutes.VehicleRoutes.Where(ff => ff.FoundZipTodayListConfirmed == true);
                    foreach (var ro in resToday)
                    {
                        foreach (var rs in ro.RouteStops)
                        {
                            var calc = getDistanceMeters(userCoords, rs.coordinates);
                            if (calc.Result != 0)
                            {
                                if (calc.Result < distanceThreshold)
                                {
                                    Tuple<int, double> tple = new Tuple<int, double>(rs.Id, calc.Result);
                                    var mLa = new RouteDistObj();
                                    mLa.Tuple = tple;
                                    mLa.VehicleRouteId = rs.VehicleRouteId;
                                    matchListToday.Add(mLa);
                                }
                            }
                        }
                    }
                }

                if (matchListToday.Count > 0)
                {
                    //Got results for TODAY's stops, get closest point and return data.
                    var topResId = matchListToday.OrderBy(mm => mm.Tuple.Item2).FirstOrDefault();
                    var topResObj = VRoutes.VehicleRoutes.Find(xy => xy.Id == topResId.VehicleRouteId);
                    var topResStop = topResObj.RouteStops.Find(xy => xy.Id == topResId.Tuple.Item1);
                    Console.WriteLine("Closest match------------");


                    var nextDt = topResStop.ParsedSchedules.FindAll(xt => xt >= DateTime.Now).OrderBy(x => x).FirstOrDefault();
                    var schDt = topResStop.ParsedSchedules.FindAll(xt => xt >= DateTime.Now).OrderBy(x => x).ToList();

                    Console.WriteLine(topResStop.NameOnRoute + "--- Distance: " + topResId.Tuple.Item2.ToString() + " meters. Next arrival: " + nextDt.ToString());

                    var resObject = new SingleStopDetails();
                    resObject.StatusCode = 200;
                    resObject.Your_Distance_To_Stop_Meters = topResId.Tuple.Item2;
                    var vehList = new List<long>();
                    foreach (var veh in topResObj.RouteStops)
                    {
                        if (veh.SequenceVisitedVehicleId != 0 && veh.SequenceVisitedTime.Date == DateTime.Now.Date)
                        {
                            vehList.Add(veh.SequenceVisitedVehicleId);
                        }
                    }
                    if (vehList.Count > 0)
                    {
                        var vehName = vehList.GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();
                        resObject.Vehicle_Name = vehName.ToString();

                        var currSeqObj = topResObj.RouteStops.Where(ff => ff.SequenceVisited == true && ff.SequenceVisitedVehicleId == vehName).OrderByDescending(xx => xx.SequenceOnRoute).FirstOrDefault();
                        if (currSeqObj != null)
                        {
                            if (currSeqObj.SequenceOnRoute != 0)
                            {
                                resObject.Vehicle_CurrentSequence = currSeqObj.SequenceOnRoute;
                            }
                        }
                    }
                    else
                    {
                        resObject.Vehicle_Name = "";
                        resObject.Vehicle_CurrentSequence = 0;
                    }
                    resObject.UrlTimetablePDF = topResStop.UrlPDFTimetable;
                    resObject.StopZipcode = topResStop.Zip;
                    resObject.StopSequence = topResStop.SequenceOnRoute;
                    resObject.StopContact = topResStop.RoutePhoneNo;
                    resObject.Next_Scheduled_Stop = nextDt;
                    resObject.StopAddress = topResStop.NameOnRoute;
                    resObject.Schedule = schDt;
                    resObject.StopCoordinates = topResStop.coordinates;
                    //Change to Lat-Lng in results, no idea why it is Lng-Lat in the API.
                    var coor = topResStop.coordinates;
                    var modCoords = new double[] { coor[1], coor[0] };
                    resObject.StopCoordinates = modCoords;

                    ret = resObject;
                    return ret;

                }
                else
                {
                    if (onlyToday == false)
                    {
                        //No results for today, Use Google API Or Supplied ZIP and generate closest data based on ZipCode

                        var listOfZip = new List<string>();
                        if (suppliedZipCode != "")
                        {
                            //Use User supplied ZIP
                            listOfZip.Add(suppliedZipCode);
                        }
                        else
                        {
                            //Only use Google API if ZIP not present in params (SE MAKSAA!)
                            listOfZip = await getZipCodes(userCoords);
                        }
                        
                        if (listOfZip.Count > 0)
                        {
                            var queryString = "";
                            foreach (var zip in listOfZip)
                            {
                                var hroutes = VRoutes.VehicleRoutes.FindAll(xx => xx.Zips.Contains(zip));
                                int itemCount = hroutes.Count;
                                for (int i = 0; i < itemCount; i++)
                                {
                                    if ((i + 1) == itemCount)
                                    {
                                        //Remove comma from Last iteration
                                        queryString += hroutes[i].File;
                                    }
                                    else
                                    {
                                        queryString += hroutes[i].File + ",";
                                    }
                                }
                            }

                            var sRoute = await httpTool("searchRoute", queryString);
                            if (sRoute != null || sRoute != "")
                            {
                                var tempStops = new List<RouteStop>();
                                var sResults = JsonSerializer.Deserialize<TodayCollection>(sRoute, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                                for (int i = 0; i < sResults.Features.Count; i++)
                                {
                                    var rs = new RouteStop();
                                    rs.Id = i + 1;
                                    rs.VehicleRouteId = 0;
                                    rs.Zip = sResults.Features[i].Properties.Zip;
                                    rs.coordinates = sResults.Features[i].Geometry.Coordinates; //Order ([0]lng,[1]lat)
                                    rs.NameOnRoute = sResults.Features[i].Properties.Name_On_Route;
                                    rs.SequenceOnRoute = sResults.Features[i].Properties.Sequence_On_Route.Value;
                                    rs.Notes = sResults.Features[i].Properties.Notes;
                                    rs.UrlPDFTimetable = "https://www.jatskiauto.com/haku/hae?zip=" + sResults.Features[i].Properties.Zip;
                                    rs.RoutePhoneNo = sResults.Features[i].Properties.Contact;
                                    rs.Schedules = sResults.Features[i].Properties.Schedules;
                                    //Parse schedules
                                    var pDta = new DateTime[] { };
                                    foreach (var dt in sResults.Features[i].Properties.Schedules)
                                    {
                                        var dtp = dt;
                                        if (dt.StartsWith("Tänään klo: "))
                                        {
                                            var dtpp = dtp.Replace("Tänään klo: ", DateTime.Now.ToString("ddd dd.MM.yyyy", CultureInfo.GetCultureInfo("fi-FI")) + " ");
                                            dtp = dtpp;
                                        }
                                        else
                                        {
                                            var dtpp = dtp.Replace("klo: ", "");
                                            dtp = dtpp;
                                        }
                                        //Parse to Datetime
                                        DateTime dtt = DateTime.Parse(dtp, CultureInfo.GetCultureInfo("fi-Fi"));
                                        if (rs.ParsedSchedules == null)
                                        {
                                            var pss = new List<DateTime>();
                                            rs.ParsedSchedules = pss;
                                        }
                                        rs.ParsedSchedules.Add(dtt);
                                    }
                                    tempStops.Add(rs);
                                }

                                foreach (var rs in tempStops)
                                {
                                    var calc = getDistanceMeters(userCoords, rs.coordinates);
                                    if (calc.Result != 0)
                                    {
                                        if (calc.Result < distanceThreshold)
                                        {
                                            Tuple<int, double> tple = new Tuple<int, double>(rs.Id, calc.Result);
                                            matchList.Add(tple);
                                        }
                                    }
                                }
                                if (matchList.Count > 0)
                                {
                                    Console.WriteLine("Following are in range------------");
                                    foreach (var match in matchList)
                                    {
                                        var xx = tempStops.Find(xy => xy.Id == match.Item1);
                                        var xTimes = xx.ParsedSchedules.FindAll(xt => xt >= DateTime.Now).OrderBy(x => x);

                                        Console.WriteLine(xx.NameOnRoute + "---" + xTimes.FirstOrDefault().ToString());
                                    }
                                    //Got results for TODAY's stops, get closest point and return data.
                                    var topResId = matchList.OrderBy(mm => mm.Item2).FirstOrDefault();
                                    var topResObj = tempStops.Find(xy => xy.Id == topResId.Item1);
                                    Console.WriteLine("Closest match------------");

                                    var nextDt = topResObj.ParsedSchedules.FindAll(xt => xt >= DateTime.Now).OrderBy(x => x).FirstOrDefault();
                                    var schDt = topResObj.ParsedSchedules.FindAll(xt => xt >= DateTime.Now).OrderBy(x => x).ToList();

                                    Console.WriteLine(topResObj.NameOnRoute + "--- Distance: " + topResId.Item2.ToString() + " meters. Next arrival: " + nextDt.ToString());

                                    var resObject = new SingleStopDetails();
                                    resObject.StatusCode = 200;
                                    resObject.Your_Distance_To_Stop_Meters = topResId.Item2;
                                    resObject.Vehicle_CurrentSequence = 0; //NOT APPLICABLE, ROUTE NOT ACTIVE TODAY
                                    resObject.Vehicle_Name = ""; //NOT APPLICABLE, ROUTE NOT ACTIVE TODAY
                                    resObject.StopSequence = topResObj.SequenceOnRoute;
                                    resObject.UrlTimetablePDF = topResObj.UrlPDFTimetable;
                                    resObject.StopZipcode = topResObj.Zip;
                                    resObject.StopContact = topResObj.RoutePhoneNo;
                                    resObject.Next_Scheduled_Stop = nextDt;
                                    resObject.StopAddress = topResObj.NameOnRoute;
                                    resObject.Schedule = schDt;
                                    //Change to Lat-Lng in results, no idea why it is Lng-Lat in the API.
                                    var coor = topResObj.coordinates;
                                    var modCoords = new double[] { coor[1], coor[0] };
                                    resObject.StopCoordinates = modCoords;

                                    ret = resObject;
                                }


                            }
                            else
                            {
                                Console.WriteLine("Error in getNearestStop");
                            }
                        }
                        else
                        {
                            Console.WriteLine("No matches in getNearestStop");
                        }

                    }

                }
            }

            return ret;
        }

        //Unused, left for reference:   Get PDF timetable with ZIPCode
        public async Task<ActionResult> getTimetablePDF(string zipcode)
        {
            var root_url = "https://www.jatskiauto.com/haku/hae?zip=";

            var req = new HttpClient();
            req.Timeout = new TimeSpan(0, 0, 20);
            req.DefaultRequestHeaders.Add("User-Agent", "JaateloautoCustomAPI/1.0.0");
            req.BaseAddress = new Uri(root_url + zipcode);

            var resp = new HttpResponseMessage();
            resp = await req.GetAsync(req.BaseAddress);
            if (resp.IsSuccessStatusCode)
            {
                var respBytes = await resp.Content.ReadAsByteArrayAsync();
                req.Dispose();
                return new FileContentResult(respBytes, "application/octet-stream");
            }
            else
            {
                req.Dispose();
                return null;
            }
        }

        //Get current ZIP code from location data //IN: LNG LAT
        public async Task<List<String>> getZipCodes(double[] coords)
        {
            var pstcodestr = new List<string>();
            var gkey = "GOOGLEAPIKEYHERE"; //GOOGLE APIKEY HERE, FIX TO AN ENV-VARIABLE ASAP...
            
            var request = new LocationGeocodeRequest
            {
                Key = gkey,
                Location = new GoogleApi.Entities.Common.Coordinate(coords[1], coords[0]) //lat, lng
            };
            //var response = GoogleMaps.LocationGeocode.Query(request);
            var response = await GoogleMaps.Geocode.LocationGeocode.QueryAsync(request);
            if (response != null)
            {
                if (response.Results != null)
                {
                    foreach (var result in response.Results)
                    {
                        if (result.AddressComponents != null)
                        {
                            var addrComp = result.AddressComponents;

                            foreach (var component in addrComp)
                            {
                                if (component.Types != null)
                                {
                                    bool foundCode = false;
                                    foreach (var compType in component.Types)
                                    {
                                        if (compType.ToString() == "Postal_Code")
                                        {
                                            foundCode = true;
                                        }
                                    }
                                    if (foundCode == true)
                                    {
                                        //Check if value exists in List already
                                        if (!pstcodestr.Contains(component.LongName))
                                        {
                                            pstcodestr.Add(component.LongName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return pstcodestr;
            }

            return pstcodestr;
        }

        /* BELOW ARE TODO PARTS:
         * -- Adding/Removal of vehicle-route mapping if threshold based on:
         * ++ Vehicle with a stop in a certain radius of the point is considered (50m? Less?)
         * ++ Vehicle with most RouteStop.SequenceVisitedVehicleId ==> Route Vehicle
         * -- Handling the current sequence of route
         * ++ Routestop with SequenceVisited = true with the highest sequence number.
         * -- Create API for Pulling Vehicle Info data
         * -- Create API for Pulling All Data
         */

        //Map Current location of vehicle to a routestop
        public async Task<string> parseVehiclesToRoutes(double rangethreshold)
        {
            var ret = "";

            //Get all Routes + Stops with Today mapped as schedule that have not been visited
            var items = new List<RouteStopMapper>();
            try
            {
                foreach (var route in VRoutes.VehicleRoutes)
                {
                    foreach (var rs in route.RouteStops)
                    {
                        //Check if mapped already, Proceed if no match
                        if (rs.SequenceVisitedTime.Date != DateTime.Now.Date)
                        {
                            foreach (var schedule in rs.ParsedSchedules)
                            {
                                if (schedule.Date == DateTime.Now.Date)
                                {
                                    //Check That Scheduled time is not in the past
                                    var dateTimeMin = DateTime.Now.AddHours(-1);
                                    var dateTimeMax = DateTime.Now.AddHours(1);

                                    if (schedule <= dateTimeMax)
                                    {
                                        if (schedule >= dateTimeMin)
                                        {
                                            var item = new RouteStopMapper();
                                            item.VehicleRouteId = route.RouteId;
                                            item.RouteStopId = rs.Id;
                                            item.RouteScheduledStop = schedule;
                                            item.Coordinates = rs.coordinates;
                                            items.Add(item);

                                            Console.WriteLine("Added for ScheduleVehicleMapper: " + schedule.ToLongDateString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                ret = "ERR";
            }

            var vehItems = new List<VehicleLocationMapper>();
            //Get Vehicle current locations
            var currVehicles = await httpTool("getAllVehicles");
            if (currVehicles != null || currVehicles != "")
            {
                try
                {
                    var sResults = JsonSerializer.Deserialize<VehicleCollection>(currVehicles, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, });
                    foreach (var vi in sResults.Features)
                    {
                        var vehItem = new VehicleLocationMapper();
                        vehItem.VehicleId = (long)vi.Properties.Id;
                        vehItem.Coordinates = vi.Geometry.Coordinates;
                        vehItems.Add(vehItem);
                    }
                }catch
                {
                    ret = "ERR";
                }
            }

            //Calculate distance between each vehicle to each RouteStop on list
            try
            {
                foreach (var vehItem in vehItems)
                {
                    foreach (var rStop in items)
                    {
                        var calcRes = await getDistanceMeters(vehItem.Coordinates, rStop.Coordinates);
                        if (calcRes <= rangethreshold)
                        {
                            //Update RouteStop visited statuses
                            var target = VRoutes.VehicleRoutes.Where(ff => ff.RouteId == rStop.VehicleRouteId).First();
                            var targetStop = target.RouteStops.Where(ff => ff.Id == rStop.RouteStopId).First();
                            targetStop.SequenceVisited = true;
                            targetStop.SequenceVisitedTime = DateTime.Now;
                            targetStop.SequenceVisitedVehicleId = vehItem.VehicleId;
                        }
                    }
                }
            }
            catch
            {
                ret = "ERR";
            }

            return ret;
        }

    }

    
    /*This API Classes*/

    public class RouteStopMapper
    {
        public int VehicleRouteId { get; set; }
        public int RouteStopId { get; set; }
        public double[] Coordinates { get; set; }
        public DateTime RouteScheduledStop { get; set; }
    }
    public class VehicleLocationMapper
    {
        public long VehicleId { get; set; }
        public double[] Coordinates { get; set; }
    }
    public class RouteDistObj
    {
        public int VehicleRouteId { get; set; }
        public Tuple<int, double> Tuple { get; set; }
    }

    public class SingleStopDetails
    {
        public int StatusCode { get; set; }
        public string StopAddress { get; set; }
        public string StopZipcode { get; set; }
        public string StopContact { get; set; }
        public string UrlTimetablePDF { get; set; }
        public double[] StopCoordinates { get; set; }
        public double Your_Distance_To_Stop_Meters { get; set; }
        public DateTime Next_Scheduled_Stop { get; set; }
        public List<DateTime> Schedule { get; set; }
        public int StopSequence { get; set; }
        public int Vehicle_CurrentSequence { get; set; }

        public string Vehicle_Name { get; set; }
    }
    /* Original API De-Serializing helpers */
    //De-serializing API classes--ROUTE
    public class Route
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public List<string>? Zips { get; set; }
        public string? File { get; set; }
    }

    //De-serializing API classes--TODAY ROUTES
    public class TodayCollection
    {
        public string? Type { get; set; }
        public int? Count { get; set; }
        public int? Total_Count { get; set; }
        public List<TodayCollectionFeatures>? Features { get; set; }
    }

    public class TodayCollectionFeatures
    {
        public string? Type { get; set; }
        public TodayCollectionFeaturesGeometry? Geometry { get; set; }
        public TodayCollectionFeaturesProperties? Properties { get; set; }

    }
    public class TodayCollectionFeaturesGeometry
    {
        public string? Type { get; set; }
        public double[]? Coordinates { get; set; }
    }
    public class TodayCollectionFeaturesProperties
    {
        public string? Zip { get; set; }
        public string? Name_On_Route { get; set; }
        public int? Sequence_On_Route { get; set; }
        public string? Notes { get; set; }
        public string? Contact { get; set; }
        public string[]? Schedules { get; set; }

    }
    //De-serializing API classes--ALL VEHICLES
    public class VehicleCollection
    {
        public string? __Generated { get; set; }
        public List<VehicleCollectionFeatures>? Features { get; set; }
    }
    public class VehicleCollectionFeatures
    {
        public string? Type { get; set; }
        public VehicleCollectionFeaturesGeometry? Geometry { get; set; }
        public VehicleCollectionFeaturesProperties? Properties { get; set; }
    }
    public class VehicleCollectionFeaturesGeometry
    {
        public string? Type { get; set; }
        public double[]? Coordinates { get; set; }
    }
    public class VehicleCollectionFeaturesProperties
    {
        public long? Id { get; set; }
        [JsonConverter(typeof(StringConverter))]
        public string? Name { get; set; } //JSON has this as integer value, unless it is not :-)
        public string? Heading { get; set; }
        public string? Speed { get; set; }

    }

    public class StringConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {

            if (reader.TokenType == JsonTokenType.Number)
            {
                var stringValue = reader.GetInt32();
                return stringValue.ToString();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            throw new System.Text.Json.JsonException();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

    }

}
