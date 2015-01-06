MSNSQL
======

Automatic mission removal tool for UnitedOperations.net

Startup Parameters
------------

__-f \<path>__<br />
Overrides the default location and filename of the configuration file to use. <br />e.g. -f "C:\Folder\Fol der\configuration.ini"


Configuration File
------------

####[DATABASE]
ADDRESS=IP to server<br />
PORT=Port of MySQL database<br/ >
USER=Database username<br/ >
PASS=Database password<br/ >
DB=Database<br/ >
TABLE=Table to query<br/ >
FIELD=Field containing mission filename<br/ >
CONDITION=WHERE clause for the SQL query<br/ >
QUERY= _OPTIONAL_ Overrides the 3 above settings and executes the full SQL query here.<br/ >

####[GAMESERVER]
LIVE_MISSION_DIRECTORY=Directory, with trailing slash, to the directory containing live missions.<br/ >
BROKEN_MISSION_DIRECTORY=Directory, with trailing slash, to the directory missions will be moved to.<br/ >

####[PROGRAM]
LOGLEVEL=Minimum log message severity to display. Avalable values are DEBUG, INFORMATION, WARNING, ERROR, CRITICAL and FATAL.<br/ >
BEHAVIOR=Defines the matching moving mode of the program. Details below. Available values are WHITELIST and BLACKLIST.<br/ >
