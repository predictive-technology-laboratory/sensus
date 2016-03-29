#' SensusR:  Sensus Analytics
#'
#' Provides access and analytic functions for Sensus data. More information can be found at the
#' following URL:
#' 
#'     https://github.com/MatthewGerber/sensus/wiki
#' 
#' @section SensusR functions:
#' The SensusR functions handle reading, cleaning, plotting, and otherwise analyzing data collected
#' via the Sensus system.
#'
#' @docType package
#' 
#' @name SensusR
NULL

#' Download data from Amazon S3 path.
#' 
#' @param s3.path Full path within S3.
#' @param local.path Path to location on local drive.
#' @param aws.path Path to AWS client.
#' @return Local path to location of downloaded data.
#' @examples 
#' # data.path = download.from.aws.s3("s3://bucket/path/to/data", "~/Desktop/data")
download.from.aws.s3 = function(s3.path, local.path = tempfile(), aws.path = "/usr/local/bin")
{
  aws = paste(aws.path, "aws", sep = "/")
  args = paste("s3 cp --recursive", s3.path, local.path, sep = " ")
  exit.code = system2(aws, args)
  return(local.path)
}

#' Read JSON-formatted Sensus data.
#' 
#' @param data.path Path to Sensus JSON data (either a file or a directory).
#' @param is.directory Whether or not the path is a directory.
#' @param recursive Whether or not to read files recursively from directory indicated by path.
#' @param convert.to.local.timezone Whether or not to convert timestamps to the local timezone.
#' @param local.timesonze If converting timestamps to local timesonze, the local timezone to use.
#' @return All data, listed by type.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"), is.directory = FALSE)
read.sensus.json = function(data.path, is.directory = TRUE, recursive = TRUE, convert.to.local.timezone = TRUE, local.timezone = Sys.timezone())
{
  paths = c(data.path)
  if(is.directory)
  {
    paths = list.files(data.path, recursive = recursive, full.names = TRUE, include.dirs = FALSE)
  }
  
  num.files = length(paths)
  
  data = list()
  file.num = 0
  for(path in paths)
  {
    file.num = file.num + 1
    
    print(paste("Parsing JSON file ", file.num, " of ", num.files, ":  ", path, sep = ""))

    # read and parse JSON
    file.size = file.info(path)$size
    if(file.size == 0)
    {
      next
    }
    
    file.text = readChar(path, file.size)
    file.json = jsonlite::fromJSON(file.text)

    # skip empty JSON
    if(is.null(file.json) || is.na(file.json) || nrow(file.json) == 0)
    {
      next
    }
    
    # remove list-type columns
    file.json = file.json[sapply(file.json, typeof) != "list"]
    
    # set datum type and OS
    type.os = lapply(file.json$"$type", function(type)
    {
      type.split = strsplit(type, ",")[[1]]
      datum.type = trim(tail(strsplit(type.split[1], ".", fixed=TRUE)[[1]], n=1))
      os = trim(type.split[2])
      
      return(c(datum.type, os))
    })
    
    type.os = matrix(unlist(type.os), nrow = length(type.os), byrow=TRUE)
    
    file.json$Type = type.os[,1]
    file.json$OS = type.os[,2]
    file.json$"$type" = NULL
    
    # parse timestamps
    file.json$Timestamp = strptime(file.json$Timestamp, format = "%Y-%m-%dT%H:%M:%OS", tz="UTC")    
    if(convert.to.local.timezone)
    {
      file.json$Timestamp = lubridate::with_tz(file.json$Timestamp, local.timezone)
    }
    
    # add to data by type, putting each file in its own list entry (we'll merge files later)
    type = file.json$Type[1]
    if(is.null(data[[type]])) {
      data[[type]] = list()
    }
    
    data.type.file.num = length(data[[type]]) + 1
    data[[type]][[data.type.file.num]] = file.json
  }
 
  # merge files for each data type
  for(datum.type in names(data))
  { 
    datum.type.data = data[[datum.type]]
    
    # pre-allocate vectors for each column in data frame
    datum.type.num.rows = sum(sapply(datum.type.data, nrow))
    datum.type.col.classes = sapply(datum.type.data[[1]], class)
    datum.type.col.classes[["Timestamp"]] = NULL  # cannot directly create vector with mode POSIXlt
    datum.type.col.vectors = lapply(datum.type.col.classes, vector, length = datum.type.num.rows)
    datum.type.col.vectors[["Timestamp"]] = as.POSIXlt(rep(NA, datum.type.num.rows))
    
    # merge files for current datum.type
    insert.start.row = 1
    num.files = length(datum.type.data)
    datum.type.col.vectors.names = names(datum.type.col.vectors)
    percent.done = 0
    for(file.num in 1:num.files)
    {
      curr.percent.done = as.integer(100 * file.num / num.files)
      if(curr.percent.done > percent.done)
      {
        percent.done = curr.percent.done
        print(paste(curr.percent.done, "% done merging data for ", datum.type, ".", sep = ""))
      }
        
      # merge columns of current file into pre-allocated vectors
      file.data = datum.type.data[[file.num]]
      file.data.rows = nrow(file.data)
      insert.end.row = insert.start.row + file.data.rows - 1
      for(col.name in datum.type.col.vectors.names)
      {
        datum.type.col.vectors[[col.name]][insert.start.row:insert.end.row] = file.data[ , col.name]
      }
      
      insert.start.row = insert.start.row + file.data.rows
    }
    
    print(paste("Creating data frame for ", datum.type, ".", sep = ""))
    
    # create data frame from pre-allocated vectors 
    data.type.data.frame = data.frame(datum.type.col.vectors, stringsAsFactors = FALSE)
    
    # filter redundant data by id and sort by timestamp
    data.type.data.frame = data.type.data.frame[!duplicated(data.type.data.frame$Id), ]
    data.type.data.frame = data.type.data.frame[order(data.type.data.frame$Timestamp), ]
    
    # add year, month, day, hour, minute, second, day of week, day of month, and day of year
    data.type.data.frame$Year = lubridate::year(data.type.data.frame$Timestamp)
    data.type.data.frame$Month = lubridate::month(data.type.data.frame$Timestamp)
    data.type.data.frame$Day = lubridate::day(data.type.data.frame$Timestamp)
    data.type.data.frame$Hour = lubridate::hour(data.type.data.frame$Timestamp)
    data.type.data.frame$Minute = lubridate::minute(data.type.data.frame$Timestamp)
    data.type.data.frame$Second = lubridate::second(data.type.data.frame$Timestamp)
    data.type.data.frame$DayOfWeek = lubridate::wday(data.type.data.frame$Timestamp)
    data.type.data.frame$DayOfMonth = lubridate::mday(data.type.data.frame$Timestamp)
    data.type.data.frame$DayOfYear = lubridate::yday(data.type.data.frame$Timestamp)
    
    # set data frame within final list
    data[[datum.type]] = data.type.data.frame
    
    # set class information for plotting
    class(data[[datum.type]]) = c(datum.type, class(data[[datum.type]]))
  }
  
  return(data)
}

#' Plot accelerometer data.
#' 
#' @method plot AccelerometerDatum
#' @param x Accelerometer data.
#' @param pch Plotting character.
#' @param type Line type. 
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$AccelerometerDatum)
plot.AccelerometerDatum = function(x, pch = ".", type = "l", ...)
{ 
  par(mfrow=c(2,2))
  plot.default(x$Timestamp, x$X, main = "Accelerometer", xlab = "Time", ylab = "X", pch = pch, type = type)
  plot.default(x$Timestamp, x$Y, main = "Accelerometer", xlab = "Time", ylab = "Y", pch = pch, type = type)
  plot.default(x$Timestamp, x$Y, main = "Accelerometer", xlab = "Time", ylab = "Z", pch = pch, type = type)
  par(mfrow=c(1,1))
}

#' Plot altitude data.
#' 
#' @method plot AltitudeDatum
#' @param x Altitude data.
#' @param pch Plotting character.
#' @param type Line type. 
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$AltitudeDatum)
plot.AltitudeDatum = function(x, pch = ".", type = "l", ...)
{
  plot.default(x$Timestamp, x$Altitude, main = "Altitude", xlab = "Time", ylab = "Meters", pch = pch, type = type, ...)
}

#' Plot battery data.
#' 
#' @method plot BatteryDatum
#' @param x Battery data.
#' @param pch Plotting character.
#' @param type Line type. 
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$BatteryDatum)
plot.BatteryDatum = function(x, pch = ".", type = "l", ...)
{
  plot(x$Timestamp, x$Level, main = "Battery", xlab = "Time", ylab = "Level (%)", pch = pch, type = type, ...)
}

#' Plot cell tower data.
#' 
#' @method plot CellTowerDatum
#' @param x Cell tower data.
#' @param ... Other plotting arguments.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$CellTowerDatum)
plot.CellTowerDatum = function(x, ...)
{
  freqs = plyr::count(x$CellTower)
  if(nrow(freqs) > 0)
  {
    pie(freqs$freq, freqs$x, main = "Cell Tower", ...)
  }
}

#' Plot compass data.
#' 
#' @method plot CompassDatum
#' @param x Compass data.
#' @param pch Plotting character.
#' @param type Line type. 
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$CompassDatum)
plot.CompassDatum = function(x, pch = ".", type = "l", ...)
{
  plot(x$Timestamp, x$Heading, main = "Compass", xlab = "Time", ylab = "Heading", pch = pch, type = type, ...)
}

#' Plot light data.
#' 
#' @method plot LightDatum
#' @param x Light data.
#' @param pch Plotting character.
#' @param type Line type. 
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$LightDatum)
plot.LightDatum = function(x, pch = ".", type = "l", ...)
{
  plot(x$Timestamp, x$Brightness, main = "Light", xlab = "Time", ylab = "Level", pch = pch, type = type, ...)
}

#' Plot location data.
#' 
#' @method plot LocationDatum
#' @param x Location data.
#' @param qmap.args Plotting parameters to pass to \code{\link{qmap}}.
#' @param geom.point.args Plotting parameters to pass to \code{\link{geom_point}}.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$LocationDatum)
plot.LocationDatum = function(x, ...)
{
  args = list(...)
  
  qmap.args = args[["qmap.args"]]
  if(!is.null(qmap.args))
  {
    if(is.null(qmap.args[["location"]]))
    {
      avg.x = mean(x$Longitude)
      avg.y = mean(x$Latitude)
      qmap.args[["location"]] = paste(avg.y, avg.x, sep=",")
    }
  }
  
  map = do.call(ggmap::qmap, qmap.args)
  
  geom.point.args = list(data = x, ggplot2::aes(x = Longitude, y = Latitude))
  passed.geom.point.args = args[["geom.point.args"]]
  if(!is.null(passed.geom.point.args))
  {
    geom.point.args = c(geom.point.args, passed.geom.point.args)
  }
  
  map + do.call(ggplot2::geom_point, geom.point.args)
}

#' Plot running apps data.
#' 
#' @method plot RunningAppsDatum
#' @param x Apps data.
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$RunningAppsDatum)
plot.RunningAppsDatum = function(x, ...)
{
  freqs = plyr::count(x$Name)
  if(nrow(freqs) > 0)
  {
    pie(freqs$freq, freqs$x, main = "Running Apps", ...)
  }
}

#' Plot screen data.
#' 
#' @method plot ScreenDatum
#' @param x Screen data.
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$ScreenDatum)
plot.ScreenDatum = function(x, ...)
{
  plot(x$Timestamp, x$On, main = "Screen", xlab = "Time", ylab = "On/Off", pch=".", type = "l", ...)
}

#' Plot sound data.
#' 
#' @method plot SoundDatum
#' @param x Sound data.
#' @param pch Plotting character.
#' @param type Line type. 
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$SoundDatum)
plot.SoundDatum = function(x, pch = ".", type = "l", ...)
{
  plot(x$Timestamp, x$Decibels, main = "Sound", xlab = "Time", ylab = "Decibels", pch = pch, type = type, ...)
}

#' Plot speed data.
#' 
#' @method plot SpeedDatum
#' @param x Speed data.
#' @param pch Plotting character.
#' @param type Line type. 
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$SpeedDatum)
plot.SpeedDatum = function(x, pch = ".", type = "l", ...)
{
  plot(x$Timestamp, x$KPH, main = "Speed", xlab = "Time", ylab = "KPH", pch = pch, type = type, ...)
}

#' Plot telephony data.
#' 
#' @method plot TelephonyDatum
#' @param x Telephony data.
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$TelephonyDatum)
plot.TelephonyDatum = function(x, ...)
{
  par(mfrow = c(2,1))
  
  outgoing.freqs = plyr::count(x$PhoneNumber[x$PhoneNumber != "" & x$State == 1])
  if(nrow(outgoing.freqs) > 0)
  {
    pie(outgoing.freqs$freq, outgoing.freqs$x, main = "Outgoing Calls", ...)
  }
  
  incoming.freqs = plyr::count(x$PhoneNumber[x$PhoneNumber != "" & x$State == 2])
  if(nrow(incoming.freqs) > 0)
  {
    pie(incoming.freqs$freq, incoming.freqs$x, main = "Incoming Calls", ...)
  }
  
  par(mfrow = c(1,1))
}

#' Plot WLAN data.
#' 
#' @method plot WlanDatum
#' @param x WLAN data.
#' @param ... Other plotting parameters.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(data$WlanDatum)
plot.WlanDatum = function(x, ...)
{
  freqs = plyr::count(x$AccessPointBSSID[x$AccessPointBSSID != ""])
  if(nrow(freqs) > 0)
  {
    pie(freqs$freq, freqs$x, main = "WLAN BSSID", ...)
  }
}

#' Get timestamp lags for a Sensus data frame.
#' 
#' @param data Data to plot lags for (e.g., the result of \code{read.sensus.json}).
#' @return List of lags organized by datum type.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' lags = get.all.timestamp.lags(data)
#' plot(lags[["AccelerometerDatum"]])
get.all.timestamp.lags = function(data)
{
  lags = list()
  for(datum.type in names(data))
  {
    if(nrow(data[[datum.type]]) > 1)
    {
      time.differences = diff(data[[datum.type]]$Timestamp)
      lags[[datum.type]] = hist(as.numeric(time.differences), main = datum.type, xlab = paste("Lag (", units(time.differences), ")", sep=""))
    }
  }
  
  return(lags)
}

#' Get timestamp lags for a Sensus datum.
#' 
#' @param datum One element of a Sensus data frame (e.g., data$CompassDatum).
#' @return List of lags.
#' @examples
#' data = read.sensus.json(system.file("extdata", "example.data.txt", package="SensusR"))
#' plot(get.timestamp.lags(data$AccelerometerDatum))
get.timestamp.lags = function(datum)
{
  lags = NULL
  if(nrow(datum) > 1)
  {
    time.differences = diff(datum$Timestamp)
    lags = hist(as.numeric(time.differences), xlab = paste("Lag (", units(time.differences), ")", sep=""), main = "Sensus Data")
  } 
  
  return(lags)
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




