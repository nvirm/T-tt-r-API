using System;
using System.Collections.Generic;

namespace JaateloautoAPI
{
    /* 
     * VRoutes is the MAIN data point
     * VInfos and VRouteStops are more or less
     * helper classes to enrich VRoutes (which includes same information)
     */

    public static class VRoutes
    {
        public static List<VehicleRoute> VehicleRoutes { get; set; }
        public static DateTime DataUpdated { get; set; } //Internal -- For daily reset
        public static Boolean Maintenance { get; set; } //Internal -- Cancel requests during daily reset
        public static Boolean InitialRunDone { get; set; }
    }
    public static class VInfos
    {
        public static List<VehicleInfo> VehicleInfos { get; set; }
    }
    public static class VRouteStops
    {
        public static List<RouteStop> RouteStops { get; set; }
    }

    //Single route, Route Zips and name of 
    public class VehicleRoute
    {
        public int Id { get; set; }
        public int RouteId { get; set; } //GET All Routes - ID
        public string CommonName { get; set; } //GET All Routes - name
        public DateTime DataUpdated { get; set; } //Internal Data updated timestamp
        public long VehicleInfoId { get; set; } //Internal: Vehicle that is considered to be on route
        public bool FoundZipTodayList { get; set; } //GET Today Stops - True if Count of Zip matches from array is > 0 (Zip doesn't guarantee the route to be found today)
        public bool FoundZipTodayListConfirmed { get; set; } //GET Single route results, parse datetimes from schedules, confirm hit (T‰n‰‰n).
        public bool FoundZipTodayListRan { get; set; } //Has this entry been ran already?
        public string File { get; set; } //GET All Routes - file (used for details in GET Route + Schedules)
        public List<string> Zips { get; set; } //GET All Routes - Zips array
        public int EstimatedCurrentStop { get; set; } //Internal estimate of current progress
        public List<RouteStop> RouteStops { get; set; }
    }

    //Single Stopping place for vehicle
    public class RouteStop
    {
        public int Id { get; set; }
        public int VehicleRouteId { get; set; }     //If Route has been identified, map it to vehicleroute id
        public string Zip { get; set; }             //GET Today Stops - properties - zip
        public double[] coordinates { get; set; }   //GET Today Stops - geometry - coordinates (lng,lat?)
        public string NameOnRoute { get; set; }     //GET Today Stops - properties - name_on_route
        public int SequenceOnRoute { get; set; }    //GET Today Stops - properties - sequence_on_route
        public bool SequenceVisited { get; set; }    //Internal: Deduction if this point has been visited
        public long SequenceVisitedVehicleId { get; set; } //Internal: Deduction which vehicle visited this stop
        public DateTime SequenceVisitedTime { get; set; } //Internal: When was point marked as visited
        public string Notes { get; set; }           //GET Today Stops - properties - notes
        public string UrlPDFTimetable { get; set; } //https://www.jatskiauto.com/haku/hae?zip={{ZIP}}
        public string RoutePhoneNo { get; set; }    //GET Today Stops - properties - contact
        public string[] Schedules { get; set; }     //GET Today Stops - properties - schedules
        public List<DateTime> ParsedSchedules { get; set; } //Parse string schedules to datetimes
    }

    //Data for single vehicle
    public class VehicleInfo
    {
        public long Id { get; set; }
        public int VehicleRouteId { get; set; }     //If Route has been identified, map it to vehicleroute id
        public string VehicleId { get; set; } //GET All Truck locations - properties -  Id  ||  used in Get Single Truck Details (unsure of format, bigint? so go with string for now)
        public string Name { get; set; } //GET Single Truck details - properties - name || integer name (shown in UI)
        public DateTime DataUpdated { get; set; } //INTERNAL Data updated timestamp
        public string CommonUpdated { get; set; } //GET All Truck locations - __generated || Api timestamp of updated data - Compare with this
        public bool VehicleInUse { get; set; } //INTERNAL If Single Truck Route History:count = 0 then false, or if not found in truck locations list
        public int NotInUseThreshold { get; set; } //INTERNAL Don't remove vehicle in use on first event of missing from All Truck locations
        public double[] coordinates { get; set; }  //GET All truck locations - "coordinates" array
        public List<Location> HistoryCoordinates { get; set; } // GET Single Truck Route History - geometry - coordinates array of arrays || Expensive query (single device query) 

    }
    public class Location
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }


}
