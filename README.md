# Töttöröö API

Scrapes API presented in website https://asiakasnakyma.jatskiauto.com/, and tries to bind Vehicles to routes, parses Schedules to machine readable format.
Idea was to create iOS Siri Shortcut for schedules (and to get an answer if the vehicle is stopping near you today!).

# iOS shortcut links (For reference)

https://www.icloud.com/shortcuts/71c3a00e7684426aa32ad48c1a95a8ed (Get stop near me that is active today)

https://www.icloud.com/shortcuts/445a02dfd2334ec3bd7c0dac32d54476 (Get nearest stop's schedule)

# Notes
- Current Vehicle locations are updated periodically (every 60 seconds) (UpdateLocationsService).
- *Not sure of working status:* Daily data is currently reset once every day between 10:00 and 11:00 (UpdateRoutesService).
- Use Swagger to test, or Postman etc. Swagger is configured to run even in production. (Postman reference in this Repo)
- Search project for GAPPS (Google API key), and insert your own key. Used only for Zip Code determination in cases it is not supplied.

# General ideas for improvements
- Put a database in place, so data doesn't have to be resetted every day.
- Maybe replace Periodic schedule timers with something else
- Async fixes etc.

# General ideas for new features
- Push notification when Vehicle is near your preferred Stopping location (Visited stopping locations are already saved)

# API Endpoints
- /NearestStopToday?Lat=<LATVALUE>&Long=<LONGVALUE>&Range=<RANGE_METERS> (GET)
- /NearestStop?Lat=<LATVALUE>&Long=<LONGVALUE>&Range=<RANGE_METERS> (GET)
- /Jaateloauto?Mode=Routes (GET)
- /Jaateloauto?Mode=Vehicles (GET)
- /Jaateloauto?Mode=Stops (GET)

# Hosting
- Tested in IIS, but since project is platform independent, world is your oyster
- Discussion on how to keep .NET Core IIS App running:

https://stackoverflow.com/questions/57523899/keep-asp-net-core-app-running-all-the-time-in-iis
https://docs.hangfire.io/en/latest/deployment-to-production/making-aspnet-app-always-running.html

# Tech side
- .NET 6.0
- Built with Visual Studio 2022
