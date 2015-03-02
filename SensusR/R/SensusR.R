
read.sensus.json = function(path, convert.to.local.timezone)
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
    new.data = new.data[!duplicated($Id)]
    new.data$Id = NULL
    
    data[[datum.type]] = new.data
  }
  
  return(data)
}

plot.accelerometer = function(accelerometer, pch = ".", type = "l")
{
  par(mfrow=c(2,2))
  plot(accelerometer$Timestamp, accelerometer$X, main = "Accelerometer", xlab = "Time", ylab = "X", pch = pch, type = type)
  plot(accelerometer$Timestamp, accelerometer$Y, main = "Accelerometer", xlab = "Time", ylab = "Y", pch = pch, type = type)
  plot(accelerometer$Timestamp, accelerometer$Y, main = "Accelerometer", xlab = "Time", ylab = "Z", pch = pch, type = type)
  par(mfrow=c(1,1))
}

plot.altitude = function(altitude, pch = ".", type = "l")
{
  plot(altitude$Timestamp, altitude$Altitude, main = "Altitude", xlab = "Time", ylab = "Meters", pch = pch, type = type)
}

plot.battery = function(battery, pch = ".", type = "l")
{
  plot(battery$Timestamp, battery$Level, main = "Battery", xlab = "Time", ylab = "Level (%)", pch = pch, type = type)
}

plot.cell.tower = function(cell.tower)
{
  freqs = plyr::count(cell.tower$CellTower)
  pie(freqs$freq, freqs$x, main = "Cell Tower")
}

plot.compass = function(compass, pch = ".", type = "l")
{
  plot(compass$Timestamp, compass$Heading, main = "Compass", xlab = "Time", ylab = "Heading", pch = pch, type = type)
}

plot.light = function(light, pch = ".", type = "l")
{
  plot(light$Timestamp, light$Brightness, main = "Light", xlab = "Time", ylab = "Level", pch = pch, type = type)
}

plot.location = function(location, resolution = "low")
{
  lon = location.data$Longitude
  lat = location.data$Latitude
  newmap <- rworldmap::getMap(resolution = resolution)
  plot(newmap, xlim = range(lon), ylim = range(lat), asp = 1)
  points(lon, lat, col = "red", cex = .6)
}

plot.running.apps = function(running.apps)
{
  freqs = plyr::count(running.apps$Name)
  pie(freqs$freq, freqs$x, main = "Running Apps")
}

plot.screen = function(screen)
{
  plot(screen$Timestamp, screen$On, main = "Screen", xlab = "Time", ylab = "On/Off", pch=".", type = "l")
}

plot.sound = function(sound, pch = ".", type = "l")
{
  plot(sound$Timestamp, sound$Decibels, main = "Sound", xlab = "Time", ylab = "Decibels", pch = pch, type = type)
}

plot.speed = function(speed, pch = ".", type = "l")
{
  plot(speed$Timestamp, speed$KPH, main = "Speed", xlab = "Time", ylab = "KPH", pch = pch, type = type)
}

plot.telephony = function(telephony)
{
  freqs = plyr::count(telephony$PhoneNumber[telephony$PhoneNumber != ""])
  pie(freqs$freq, freqs$x, main = "Phone Numbers")
}

plot.wlan = function(wlan)
{
  freqs = plyr::count(wlan$AccessPointBSSID[wlan$AccessPointBSSID != ""])
  pie(freqs$freq, freqs$x, main = "WLAN BSSID")
}

plot.timestamp.lags = function(data, mfrow = c(4, 4))
{
  par(mfrow = mfrow)
  for(datum.type in names(data))
  {
    if(nrow(data[[datum.type]]) > 1)
    {
      hist(as.numeric(diff(data[[datum.type]]$Timestamp)), main = datum.type, xlab = "Lag")
    }
  }
}

trim.leading = function (x)  sub("^\\s+", "", x)

trim.trailing = function (x) sub("\\s+$", "", x)

trim = function (x) gsub("^\\s+|\\s+$", "", x)




