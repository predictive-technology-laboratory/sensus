#' SensusR:  Sensus Analytics
#'
#' This package provides access and analytic functions for Sensus data.
#' 
#' @section SensusR functions:
#' The SensusR functions handle reading, cleaning, plotting, and otherwise analyzing data collected
#' via the Sensus system.
#'
#' @docType package
#' @name SensusR
NULL

#' Read JSON-formatted Sensus data.
#' 
#' @param path Path to JSON file.
#' @param convert.to.local.timezone Whether or not to convert timestamps to the local timezone.
#' @return All data, listed by type.
read.sensus.json = function(path, convert.to.local.timezone = FALSE)
{
  local.timezone = Sys.timezone()
  
  # read all lines, only retaining non-empty lines
  con = file(path, open="r")
  lines = as.matrix(readLines(con))
  lines = apply(lines, 1, trim)
  lines = as.matrix(lines[sapply(lines, nchar) > 0])
  close(con)

  # parse each line to json
  lines = apply(lines, 1, function(line)
  {
    json = jsonlite::fromJSON(line)
    
    # set short version of type
    datum.type = strsplit(json$"$type", ",")[[1]][1]
    datum.type = tail(strsplit(datum.type, "[.]")[[1]], n=1)
    json$Type = datum.type
    
    # we no longer need the $type column
    json = json[-which(names(json) %in% c("$type"))]
    
    return(as.data.frame(json, stringsAsFactors = FALSE))
  })
  
  # split up data by type
  types = as.factor(sapply(lines, function(line) { return(line$Type) }))
  data = split(lines, types)
  
  # unlist everything
  for(datum.type in levels(types))
  {
    first.row = data[[datum.type]][[1]]
    column.names = names(first.row)
    
    # build new dataframe for the current data type
    new.data = data.frame(matrix(nrow=length(data[[datum.type]]), ncol=0))
    for(col in column.names)
    {
      col.data = unlist(sapply(data[[datum.type]], function(row,col) { return(row[[col]])}, col))
      new.data[[col]] = col.data
    }
    
    # parse/convert all time stamps
    new.data$Timestamp = strptime(new.data$Timestamp, format = "%Y-%m-%dT%H:%M:%OS", tz="UTC")    
    if(convert.to.local.timezone)
    {
      new.data$Timestamp = lubridate::with_tz(new.data$Timestamp, local.timezone)
    }
    
    # don't need type anymore, since we've group by type
    new.data$Type = NULL
    
    # order by timestamp
    new.data = new.data[order(new.data$Timestamp),]
    
    # filter redundant data by Id and remove Id column
    new.data = new.data[!duplicated(new.data$Id),]
    new.data$Id = NULL
    
    data[[datum.type]] = new.data
  }
  
  return(data)
}

#' Plot accelerometer data
#' 
#' @param accelerometer Accelerometer data (e.g., data$AccelerometerDatum)
#' @param pch Plotting character.
#' @param type Line type. 
plot.accelerometer = function(accelerometer, pch = ".", type = "l")
{
  par(mfrow=c(2,2))
  plot(accelerometer$Timestamp, accelerometer$X, main = "Accelerometer", xlab = "Time", ylab = "X", pch = pch, type = type)
  plot(accelerometer$Timestamp, accelerometer$Y, main = "Accelerometer", xlab = "Time", ylab = "Y", pch = pch, type = type)
  plot(accelerometer$Timestamp, accelerometer$Y, main = "Accelerometer", xlab = "Time", ylab = "Z", pch = pch, type = type)
  par(mfrow=c(1,1))
}

#' Plot altitude data.
#' 
#' @param altitude Altitude data (e.g., data$AltitudeDatum)
#' @param pch Plotting character.
#' @param type Line type. 
plot.altitude = function(altitude, pch = ".", type = "l")
{
  plot(altitude$Timestamp, altitude$Altitude, main = "Altitude", xlab = "Time", ylab = "Meters", pch = pch, type = type)
}

#' Plot battery data.
#' 
#' @param batter Battery data (e.g., data$BatteryDatum)
#' @param pch Plotting character.
#' @param type Line type. 
plot.battery = function(battery, pch = ".", type = "l")
{
  plot(battery$Timestamp, battery$Level, main = "Battery", xlab = "Time", ylab = "Level (%)", pch = pch, type = type)
}

#' Plot cell tower data.
#' 
#' @param cell.tower Cell tower data (e.g., data$CellTowerDatum)
plot.cell.tower = function(cell.tower)
{
  freqs = plyr::count(cell.tower$CellTower)
  pie(freqs$freq, freqs$x, main = "Cell Tower")
}

#' Plot compass data.
#' 
#' @param compass Compass data (e.g., data$CompassDatum)
#' @param pch Plotting character.
#' @param type Line type. 
plot.compass = function(compass, pch = ".", type = "l")
{
  plot(compass$Timestamp, compass$Heading, main = "Compass", xlab = "Time", ylab = "Heading", pch = pch, type = type)
}

#' Plot light data.
#' 
#' @param light Light data (e.g., data$LightDatum)
#' @param pch Plotting character.
#' @param type Line type. 
plot.light = function(light, pch = ".", type = "l")
{
  plot(light$Timestamp, light$Brightness, main = "Light", xlab = "Time", ylab = "Level", pch = pch, type = type)
}

#' Plot location data.
#' 
#' @param location Location data (e.g., data$LocationDatum)
#' @param resolution Mapping resolution ("low"/"high").
plot.location = function(location, resolution = "low")
{
  lon = location$Longitude
  lat = location$Latitude
  newmap = rworldmap::getMap(resolution = resolution)
  plot(newmap, xlim = range(lon), ylim = range(lat), asp = 1)
  points(lon, lat, col = "red", cex = .6)
}

#' Plot running apps.
#' 
#' @param running.apps Running apps data (e.g., data$RunningAppsDatum)
plot.running.apps = function(running.apps)
{
  freqs = plyr::count(running.apps$Name)
  pie(freqs$freq, freqs$x, main = "Running Apps")
}

#' Plot screen status.
#' 
#' @param screen Screen status data (e.g., data$ScreenDatum)
plot.screen = function(screen)
{
  plot(screen$Timestamp, screen$On, main = "Screen", xlab = "Time", ylab = "On/Off", pch=".", type = "l")
}

#' Plot sound data.
#' 
#' @param sound Sound data (e.g., data$SoundDatum)
#' @param pch Plotting character.
#' @param type Line type.
plot.sound = function(sound, pch = ".", type = "l")
{
  plot(sound$Timestamp, sound$Decibels, main = "Sound", xlab = "Time", ylab = "Decibels", pch = pch, type = type)
}

#' Plot speed data.
#' 
#' @param speed Speed data (e.g., data$SpeedDatum)
#' @param pch Plotting character.
#' @param type Line type.
plot.speed = function(speed, pch = ".", type = "l")
{
  plot(speed$Timestamp, speed$KPH, main = "Speed", xlab = "Time", ylab = "KPH", pch = pch, type = type)
}

#' Plot telephony data.
#' 
#' @param telephony Telephony data (e.g., data$TelephonyDatum)
plot.telephony = function(telephony)
{
  freqs = plyr::count(telephony$PhoneNumber[telephony$PhoneNumber != ""])
  pie(freqs$freq, freqs$x, main = "Phone Numbers")
}

#' Plot WLAN data.
#' 
#' @param wlan WLAN data (e.g., data$WlanDatum)
plot.wlan = function(wlan)
{
  freqs = plyr::count(wlan$AccessPointBSSID[wlan$AccessPointBSSID != ""])
  pie(freqs$freq, freqs$x, main = "WLAN BSSID")
}

#' Plot timestamp lags for a Sensus data frame.
#' 
#' @param data Data to plot lags for (e.g., the result of \code{read.sensus.json}.)
plot.timestamp.lags = function(data, mfrow = c(4, 4))
{
  par(mfrow = mfrow)
  for(datum.type in names(data))
  {
    if(nrow(data[[datum.type]]) > 1)
    {
      hist(as.numeric(diff(data[[datum.type]]$Timestamp)), main = datum.type, xlab = "Lag (Seconds)")
    }
  }
}

#' Trim leading white space from a string.
#' 
#' @param x String to trim.
#' @return Result of trimming.
trim.leading = function (x) sub("^\\s+", "", x)

#' Trim trailing white space from a string.
#' 
#' @param x String to trim.
#' @return Result of trimming.
trim.trailing = function (x) sub("\\s+$", "", x)

#' Trim leading and trailing white space from a string.
#' 
#' @param x String to trim.
#' @return Result of trimming.
trim = function (x) gsub("^\\s+|\\s+$", "", x)




