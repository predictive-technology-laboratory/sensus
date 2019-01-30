#' SensusR:  Sensus Analytics
#'
#' Provides access and analytic functions for Sensus data. More information can be found at the
#' following URL:
#' 
#'     https://predictive-technology-laboratory.github.io/sensus
#' 
#' @section SensusR functions:
#' The SensusR functions handle reading, cleaning, plotting, and otherwise analyzing data collected
#' via the Sensus system.
#'
#' @docType package
#' 
#' @name SensusR
NULL

#' Lists S3 buckets.
#' 
#' @param profile AWS credentials profile to use for authentication.
#' @param aws.path Path to AWS client.
#' 
sensus.list.aws.s3.buckets = function(profile = "default", aws.path = "/usr/local/bin/aws")
{
  aws.args = paste("s3api --profile", profile, "list-buckets --query \"Buckets[].Name\"", sep = " ")
  output = system2(aws.path, aws.args, stdout = TRUE)
  output.json = jsonlite::fromJSON(output)
  return(output.json)
}

#' Synchronizes data from Amazon S3 to a local path.
#' 
#' @param s3.path Path within S3. This can be a prefix (partial path).
#' @param profile AWS credentials profile to use for authentication.
#' @param local.path Path to location on local machine.
#' @param aws.path Path to AWS client.
#' @param delete Whether or not to delete local files that are not present in the S3 path.
#' @param decompress Whether or not to decompress any gzip files after downloading them.
#' @return Local path to location of downloaded data.
#' @examples 
#' # data.path = sensus.sync.from.aws.s3("s3://bucket/path/to/data", local.path = "~/Desktop/data")
sensus.sync.from.aws.s3 = function(s3.path, profile = "default", local.path = tempfile(), aws.path = "/usr/local/bin/aws", delete = FALSE, decompress = FALSE)
{
  aws.args = paste("s3 --profile", profile, "sync ", s3.path, local.path, sep = " ")
  
  if(delete)
  {
    aws.args = paste(aws.args, "--delete")
  }
  
  exit.code = system2(aws.path, aws.args)
  
  if(decompress)
  {
    sensus.decompress.gz.files(local.path)
  }
  
  return(local.path)
}

#' Decrypts Sensus .bin files that were encrypted using asymmetric public/private key encryption.
#' 
#' @param data.path Path to Sensus .bin data (either a file or a directory).
#' @param is.directory Whether or not the path is a directory.
#' @param recursive Whether or not to read files recursively from directory indicated by path.
#' @param rsa.private.key.path Path to RSA private key generated using OpenSSL.
#' @param rsa.private.key.password Password used to decrypt the RSA private key.
#' @param replace.files Whether or not to delete .bin files after they have been decrypted.
#' @return None
#' @examples
#' # sensus.decrypt.bin.files(data.path = "/path/to/bin/files/directory", 
#' #                          rsa.private.key.path = "/path/to/private.pem", 
#' #                          replace.files = FALSE)
sensus.decrypt.bin.files = function(data.path, is.directory = TRUE, recursive = TRUE, rsa.private.key.path, rsa.private.key.password = askpass, replace.files = FALSE)
{
  bin.paths = c(data.path)
  
  if(is.directory)
  {
    bin.paths = list.files(data.path, recursive = recursive, full.names = TRUE, include.dirs = FALSE, pattern = "*.bin")
  }
  
  # read the RSA private key
  rsa.private.key.file = file(rsa.private.key.path, "rb")
  rsa.private.key = read_key(rsa.private.key.file, password = rsa.private.key.password)
  close(rsa.private.key.file)
  
  print(paste("Decrypting", length(bin.paths), "file(s)..."))
  
  for(bin.path in bin.paths)
  {
    bin.file = file(bin.path, "rb")
    
    # read/decrypt the symmetric (aes) key
    enc.aes.key.size = readBin(bin.file, integer(), 1, 4)
    enc.aes.key = readBin(bin.file, raw(), enc.aes.key.size)
    aes.key = rsa_decrypt(enc.aes.key, rsa.private.key)
    
    # read/decrypt the symmetric (aes) initialization vector
    enc.aes.iv.size = readBin(bin.file, integer(), 1, 4)
    enc.aes.iv = readBin(bin.file, raw(), enc.aes.iv.size)
    aes.iv = rsa_decrypt(enc.aes.iv, rsa.private.key)
    
    # read the data content
    file.size.bytes = file.size(bin.path)
    data.size.bytes = file.size.bytes - (4 + enc.aes.key.size + 4 + enc.aes.iv.size)
    enc.data = readBin(bin.file, raw(), data.size.bytes)
    
    # make sure we read the rest of the file
    empty.check = readBin(bin.file, raw(), 1)
    close(bin.file)
    
    if(length(empty.check) != 0)
    {
      write("Decryption error:  Leftover bytes in data segment. Proceeding with decryption anyway, but there is something seriously wrong.", stderr())
    }
    
    # decrypt the data using the aes key/iv
    data = aes_cbc_decrypt(enc.data, aes.key, aes.iv)
    
    # write data to decrypted file
    decrypted.path = sub(".bin$", "", bin.path)
    decrypted.file = file(decrypted.path, "wb")
    writeBin(data, decrypted.file)
    close(decrypted.file)
    
    if(replace.files)
    {
      file.remove(bin.path)
    }
  }
}

#' Decompresses JSON files downloaded from AWS S3.
#' 
#' @param local.path Path to location on local machine.
#' @param skip If TRUE and the output file already exists, the output file is returned as is.
#' @param overwrite If TRUE and the output file already exists, the file is silently overwritten; otherwise an exception is thrown (unless skip is TRUE).
#' @param remove If TRUE, the input file is removed afterward, otherwise not.
#' @return None
#' @examples 
#' # data.path = system.file("extdata", "example-data", package="SensusR")
#' # sensus.decompress.gz.files(data.path)
sensus.decompress.gz.files = function(local.path, skip = TRUE, overwrite = FALSE, remove = FALSE)
{
  gz.paths = list.files(local.path, recursive = TRUE, full.names = TRUE, include.dirs = FALSE, pattern = "*.gz$")
  
  print(paste("Decompressing", length(gz.paths), "file(s)..."))
  
  for(gz.path in gz.paths)
  {
    gunzip(gz.path, skip = skip, overwrite = overwrite, remove = remove)
  }
}

#' Read JSON-formatted Sensus data.
#' 
#' @param data.path Path to Sensus JSON data (either a file or a directory).
#' @param is.directory Whether or not the path is a directory.
#' @param recursive Whether or not to read files recursively from directory indicated by path.
#' @param local.timezone The local timezone to convert datum timestamps to, or NULL to leave the timestamps unconverted.
#' @param data.types Specific data types to read. A full list of data types can be found here:  \url{https://predictive-technology-laboratory.github.io/sensus/api/Sensus.Datum.html}. For example \code{c("AccelerometerDatum", "HeightDatum")} will only read accelerometer and height data. Pass \code{NULL} to read all data types.
#' @return All data, listed by type.
#' @examples
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
sensus.read.json.files = function(data.path, 
                                  is.directory = TRUE, 
                                  recursive = TRUE,
                                  local.timezone = Sys.timezone(),
                                  data.types = NULL)
{
  paths = c(data.path)
  
  if(is.directory)
  {
    paths = list.files(data.path, recursive = recursive, full.names = TRUE, include.dirs = FALSE, pattern = "*.json$")
  } else if(!file.exists(paths[1]))
  {
    warning(paste("File does not exist: ", path))
    return(NULL)
  }
  
  num.files = length(paths)
  
  # keep track of the expected data count, both by type and in total.
  expected.data.cnt.by.type = list()
  expected.total.cnt = 0
  
  # keep track of columns observed for each data type
  data.type.column.type = list()
  
  # process each path
  data = list()
  file.num = 0
  for(path in paths)
  {
    file.num = file.num + 1
    
    print(paste("Parsing JSON file ", file.num, " of ", num.files, ":  ", path, sep = ""))

    # skip zero-length files as well as participation reward files.
    file.size = file.info(path)$size
    if(file.size == 0 || length(grep('ParticipationRewardDatum', path)) > 0)
    {
      next
    }
    
    # if we're filtering for specific data types, read JSON lines and filter for desired data types.
    if(length(data.types) > 0)
    {
      file.lines = readLines(path, file.size, warn = FALSE)
      
      # set up booleans for lines to keep, always keeping first and last lines if they are array brackets.
      lines.to.keep = rep(FALSE, length(file.lines))
      lines.to.keep[1] = file.lines[1] == "["
      lines.to.keep[length(lines.to.keep)] = file.lines[length(file.lines)] == "]"
      
      # keep lines of each desired data type
      for(data.type in data.types)
      {
        # example line:  {"$type":"Sensus.Probes.Movement.GyroscopeDatum, SensusAndroid","X":0.00038380140904337168,"Y":0.00050759583245962858,"Z":-4.7261957661248744E-05,"Id":"e7485732-8678-46ee-b458-a1515adf8655","DeviceId":"dae1fdfe91facb68","Timestamp":"2019-01-09T13:08:21.172667+00:00","ProtocolId":"cf54e60f-0461-4234-9548-c8f297117d37","BuildId":"1547004843","ParticipantId":null,"DeviceManufacturer":"Google","DeviceModel":"sailfish","OperatingSystem":"Android P","TaggedEventId":null,"TaggedEventTags":null}
        lines.with.data.type = grep(pattern = paste("\"\\$type\":\"[^\"]+", data.type, "[^\"]+\"", sep=""), x = file.lines, value = FALSE, fixed = FALSE)
        lines.to.keep[lines.with.data.type] = TRUE
      }
      
      # subset lines to those that were array brackets or matched one of the desired data types
      file.lines = file.lines[lines.to.keep]
      
      # ensure last line doesn't end with a comma, which will be the case if we filter out the final JSON object in the original array but keep previous ones, which ended with commas.
      if(length(file.lines) >= 2)
      {
        index = length(file.lines) - 1
        file.lines[index] = sub(",$", "", file.lines[index])
      }
      
      # collapse subsetted lines back to a single string with newlines
      file.text = paste(file.lines, collapse = "\n")
    }
    # otherwise, read all text as is.
    else
    {
      file.text = readChar(path, file.size)
    }
    
    # check for incomplete JSON file. this can happen if the app is killed before it has a chance
    # to properly close of the JSON array. in such cases, the file will abruptly terminate with
    # an unclosed JSON array, and the fromJSON call below will fail. detect this condition and fix
    # up the JSON accordingly.
    if(!endsWith(file.text, "]"))
    {
      # replace the final line, which is incomplete, with a closing square brace to 
      # complete the JSON array. if the final line is preceded by a comma (as it will 
      # typically be with one JSON object per line) then also include the comma in the 
      # text to be replaced. the final result should be a valid JSON array.
      file.text = sub(",?\n[^\n]*$", "\n]", file.text)
      
      warning(paste("File", path, "contained an unclosed JSON array. Trimmed off final line."))
      
      # if we somehow failed to fix up the array with our regex substitution, let the
      # user know and ignore the file.
      if(!endsWith(file.text, "]"))
      {
        warning(paste("Failed to fix path", path, ". Ignoring this file."))
        next
      }
    }
    
    file.json = jsonlite::fromJSON(file.text)

    # skip empty JSON
    if(is.null(file.json) || is.na(file.json) || length(file.json) == 0)
    {
      next
    }
    
    # add to expected total count of data, which is the length of the Id list column (or any column).
    expected.total.cnt = expected.total.cnt + length(file.json$Id)
    
    # file.json is a list with one entry per column (e.g., for the X coordinates of accelerometer readings). sub-list
    # the file.json list to include only those columns that are not themselves lists. such list columns will be seen
    # in cases like the survey data, which we currently cannot handle.
    file.json = file.json[sapply(file.json, typeof) != "list"]
    
    # set datum type and OS columns
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
    
    # remove the original $type column
    file.json$"$type" = NULL
    
    # remove the original $Anonymized column. not needed. this column was removed from serialization 
    # in a later version of sensus, but we retain it here for backwards compatibility.
    file.json$Anonymized = NULL
    
    # parse timestamps and convert to local time zone if needed
    file.json$Timestamp = strptime(file.json$Timestamp, format = "%Y-%m-%dT%H:%M:%OS", tz="UTC")    
    if(!is.null(local.timezone))
    {
      file.json$Timestamp = lubridate::with_tz(file.json$Timestamp, local.timezone)
    }
    
    # the input files will have JSON objects of different type (e.g., location and acceleration). the resulting file.json variable
    # will a list element for each column across all data types (e.g., latitude and X columns for location and acceleration types).
    # since 1 or more columns for each data type will be specific to that type, these specific columns will have NA values for the
    # other types (e.g., the X acceleration column for location data will be all NAs). the first step in cleaning all of this up is 
    # to split the entries in each column list by type. do this now...
    split.file.json = lapply(split(file.json, file.json$Type), function(data.type)
    {
      # the data.type variable is for a specific type (e.g., location data) and it has all columns
      # from all data types. only those columns for location data will have non-NA values. the other
      # columns (e.g., X from acceleration) will be entirely NAs and can be removed. identify these
      # all-NA columns next. ignore the special case of TaggedEventId columns, which are often all
      # null within an entire file but should still be retained.
      column.is.all.nas = sapply(data.type, function(data.type.column)
      {
        return(sum(is.na(data.type.column)) == length(data.type.column))
      })

      # remove list elements (columns) for the current data type that have all NAs, as this indicates
      # that the column actually belongs to some other data type. the only exception to this is the
      # TaggedEventId column, which will typically be all NAs but is a valid column for all data types.
      data.type[column.is.all.nas & (names(data.type) != "TaggedEventId")] = NULL
      
      return(data.type)
    })
    
    # now that we have reorganized all data into a list by type, append the list for each type to our 
    # collection. later we'll take care of merging together all data of each type into a dataframe, but
    # we can't do this now because we don't know how large of a dataframe needs to be preallocated.
    for(data.type in names(split.file.json))
    {
      # add to data by type, putting each file in its own list entry (we'll merge files later)
      if(is.null(data[[data.type]]))
      {
        data[[data.type]] = list()
      }
      
      data.type.file.num = length(data[[data.type]]) + 1
      data[[data.type]][[data.type.file.num]] = split.file.json[[data.type]]
      
      # update expected data counts by type (use number of non-null IDs as basis for counting)
      data.type.curr.cnt = 0
      if(!is.null(expected.data.cnt.by.type[[data.type]]))
      {
        data.type.curr.cnt = expected.data.cnt.by.type[[data.type]]
      }
      
      expected.data.cnt.by.type[[data.type]] = data.type.curr.cnt + sum(!is.na(split.file.json[[data.type]]$Id))
      
      # keep track of all observed columns/types. we'll use this set of columns/types to initialize
      # the final data frame. we're tracking them here to guard against files that have different
      # JSON fields (e.g., due to version upgrades or other strangeness).
      column.type = sapply(split.file.json[[data.type]], class)
      for(column in names(column.type))
      {
        # the timestamp column has classes that cannot be constructed automatically in 
        # subsequent code where the lookup is used. ignore it here and manually add it later.
        if(column == "Timestamp")
        {
          next
        }
        
        if(is.null(data.type.column.type[[data.type]]))
        {
          data.type.column.type[[data.type]] = list()
        }
        
        if(is.null(data.type.column.type[[data.type]][[column]]))
        {
          data.type.column.type[[data.type]][[column]] = column.type[[column]]
        }
      }
    }
  }
 
  # merge files for each data type
  final.data.cnt.by.type = list()
  final.total.cnt = 0
  for(datum.type in names(data))
  { 
    print(paste("Merging data for type ", datum.type, ".", sep = ""))
    
    datum.type.data = data[[datum.type]]
    
    # pre-allocate vectors for each column in data frame according to each column's type, using
    # length equal to the number of rows. by preallocating everything we'll avoid large reallocations
    # of memory that tend to crash R.
    datum.type.num.rows = sum(sapply(datum.type.data, nrow))
    datum.type.col.classes = data.type.column.type[[datum.type]]
    datum.type.col.vectors = lapply(datum.type.col.classes, vector, length = datum.type.num.rows)
    
    # pre-allocate timestamp vector manually. can't do it the same as above.
    datum.type.col.vectors[["Timestamp"]] = as.POSIXlt(rep(NA, datum.type.num.rows))
    
    # merge files for current datum.type into the preallocated vectors
    insert.start.row = 1
    num.files = length(datum.type.data)
    datum.type.col.vectors.names = names(datum.type.col.vectors)
    percent.done = 0
    for(file.num in 1:num.files)
    {
      # merge columns of current file into pre-allocated vectors
      file.data = datum.type.data[[file.num]]
      file.data.rows = nrow(file.data)
      insert.end.row = insert.start.row + file.data.rows - 1
      for(col.name in datum.type.col.vectors.names)
      {
        # check whether the current file data actually has the desired column. it might
        # not due to sensus version changes or other anticipated issues.
        if(col.name %in% colnames(file.data))
        {
          datum.type.col.vectors[[col.name]][insert.start.row:insert.end.row] = file.data[ , col.name]
        }
        else
        {
          warning(paste("Data file for type ", datum.type, " is missing column ", col.name, ". There will be null values in this column.", sep=""))
        }
      }
      
      insert.start.row = insert.start.row + file.data.rows
      
      curr.percent.done = as.integer(100 * file.num / num.files)
      if(curr.percent.done > percent.done)
      {
        percent.done = curr.percent.done
        print(paste(curr.percent.done, "% done merging data for ", datum.type, " (", file.num, " of ", num.files, ").", sep = ""))
      }
    }
    
    print(paste("Creating data frame for ", datum.type, ".", sep = ""))
    
    # create data frame for current type from pre-allocated vectors 
    data.type.data.frame = data.frame(datum.type.col.vectors, stringsAsFactors = FALSE)
    
    # record final count for the current type. we do this before the deduplication step that comes next
    # since our expected counts were also calculated without any deduplication.
    final.data.cnt.by.type[[datum.type]] = nrow(data.type.data.frame)
    final.total.cnt = final.total.cnt + nrow(data.type.data.frame)
    
    # filter redundant data by datum id and sort by timestamp
    data.type.data.frame = data.type.data.frame[!duplicated(data.type.data.frame$Id), ]
    data.type.data.frame = data.type.data.frame[order(data.type.data.frame$Timestamp), ]
    
    # set data frame within final list
    data[[datum.type]] = data.type.data.frame
    
    # set class information for plotting
    class(data[[datum.type]]) = c(datum.type, class(data[[datum.type]]))
  }
  
  # make sure expected and final counts match per type
  for(datum.type in names(data))
  {
    expected.cnt = expected.data.cnt.by.type[[datum.type]]
    final.cnt = final.data.cnt.by.type[[datum.type]]
    if(expected.cnt == final.cnt)
    {
      print(paste(datum.type, ":  Expected and final counts match (", expected.cnt, ").", sep=""))
    }
    else
    {
      warning(paste(datum.type, ":  Expected ", expected.cnt, " but obtained ", final.cnt, ".", sep = ""))
    }
  }
  
  # make sure expected and final total counts match overall
  if(expected.total.cnt == final.total.cnt)
  {
    print(paste("Final count is correct (", expected.total.cnt, ").", sep=""))
  }
  else
  {
    warning(paste("Final count mismatch (expected ", expected.total.cnt, " but got ", final.total.cnt, ").", sep=""))
  }
  
  return(data)
}

#' Gets unique device IDs within a dataset.
#'
#' @param data Data to write, as read using \code{\link{sensus.read.json.files}}.
#' @return Unique device IDs within the data
#' 
sensus.get.unique.device.ids = function(data)
{
  return(unique(unlist(sapply(names(data), function(datum.type) unique(data[[datum.type]]$DeviceId)), use.names = FALSE)))
}

#' Write data to CSV files.
#' 
#' @param data Data to write, as read using \code{\link{sensus.read.json.files}}.
#' @param directory Directory to write CSV files to. Will be created if it does not exist.
#' @param file.name.prefix Prefix to add to the generated file names.
#' @examples 
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
#' # sensus.write.csv.files(data, directory = "/path/to/directory")
sensus.write.csv.files = function(data, directory, file.name.prefix = "")
{
  dir.create(directory, showWarnings = FALSE)
  
  for(name in names(data))
  {
    write.csv(data[[name]], file = file.path(directory, paste(file.name.prefix, name, ".csv", sep = "")), row.names = FALSE)
  }
}

#' Write data to rdata files.
#' 
#' @param data Data to write, as read using \code{\link{sensus.read.json.files}}.
#' @param directory Directory to write CSV files to. Will be created if it does not exist.
#' @param file.name.prefix Prefix to add to the generated file names.
#' @examples 
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
#' # sensus.write.csv.files(data, directory = "/path/to/directory")
sensus.write.rdata.files = function(data, directory, file.name.prefix = "")
{
  dir.create(directory, showWarnings = FALSE)
  
  for(name in names(data))
  {
    datum = data[[name]]
    save(datum, file = file.path(directory, paste(file.name.prefix, name, ".rdata", sep = "")))
  }
}

#' Lists activities in a given phase and state.
#' 
#' @param data Data, as returned by \code{\link{sensus.read.json.files}}.
#' @param phase Phase of activity (Starting, During, Stopping)
#' @param state State of phase (Active, Inactive, Unknown)
#' 
sensus.list.activities = function(data, phase = "Starting", state = "Active")
{
  data$ActivityDatum[data$ActivityDatum$Phase == phase & data$ActivityDatum$State == state, ]
}

#' Plot accelerometer data.
#' 
#' @method plot AccelerometerDatum
#' @param x Accelerometer data.
#' @param pch Plotting character.
#' @param type Line type. 
#' @param ... Other plotting parameters.
#' @examples
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
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
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
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
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
#' plot(data$BatteryDatum)
plot.BatteryDatum = function(x, pch = ".", type = "l", main = "Battery", ...)
{
  plot(x$Timestamp, x$Level, main = main, xlab = "Time", ylab = "Level (%)", pch = pch, type = type, ...)
}

#' Plot cell tower data.
#' 
#' @method plot CellTowerDatum
#' @param x Cell tower data.
#' @param ... Other plotting arguments.
#' @examples
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
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
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
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
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
#' plot(data$LightDatum)
plot.LightDatum = function(x, pch = ".", type = "l", ...)
{
  plot(x$Timestamp, x$Brightness, main = "Light", xlab = "Time", ylab = "Level", pch = pch, type = type, ...)
}

#' Plot location data.
#' 
#' @method plot LocationDatum
#' @param x Location data.
#' @param ... Arguments to pass to plotting routines. This can include two special arguments:  qmap.args (passed to \code{\link{qmap}}) and geom.point.args (passed to \code{\link{geom_point}}).
#' @examples
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
#' #plot(data$LocationDatum) -- this line of example code does not play nicely with the CRAN servers.
plot.LocationDatum = function(x, ...)
{
  args = list(...)
  
  # ensure that we have arguments for qmap
  if(is.null(args[["qmap.args"]]))
  {
    args[["qmap.args"]] = list()
  }
  
  qmap.args = args[["qmap.args"]]
  
  # set default center location if one is not provided. use average latitude/longitude
  if(is.null(qmap.args[["location"]]))
  {
    avg.x = mean(x$Longitude)
    avg.y = mean(x$Latitude)
    qmap.args[["location"]] = paste(avg.y, avg.x, sep=",")
  }
  
  # create map
  map = do.call(ggmap::qmap, qmap.args)
  
  # ensure that we have arguments for geom_point
  if(is.null(args[["geom.point.args"]]))
  {
    args[["geom.point.args"]] = list()
  }
  
  # assume geom_point arguments from data and any passed arguments
  geom.point.args = list(data = x, ggplot2::aes_string(x = "Longitude", y = "Latitude"))
  geom.point.args = c(geom.point.args, args[["geom.point.args"]])
  
  # add points
  map + do.call(ggplot2::geom_point, geom.point.args)
}

#' Plot screen data.
#' 
#' @method plot ScreenDatum
#' @param x Screen data.
#' @param ... Other plotting parameters.
#' @examples
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
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
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
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
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
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
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
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
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
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
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
#' lags = sensus.get.all.timestamp.lags(data)
#' plot(lags[["AccelerometerDatum"]])
sensus.get.all.timestamp.lags = function(data)
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
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
#' plot(sensus.get.timestamp.lags(data$AccelerometerDatum))
sensus.get.timestamp.lags = function(datum)
{
  lags = NULL
  if(nrow(datum) > 1)
  {
    time.differences = diff(datum$Timestamp)
    lags = hist(as.numeric(time.differences), xlab = paste("Lag (", units(time.differences), ")", sep=""), main = "Sensus Data")
  } 
  
  return(lags)
}

#' Plot the CDF of inter-reading time lags.
#' 
#' @param datum Data frame for a single datum.
#' @param xlim Limits for the x-axis.
#' @param xlab Label for x-axis.
#' @param ylab Label for y-axis.
#' @param main Label for plot.
#' @examples 
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
#' sensus.plot.lag.cdf(data$AccelerometerDatum)
sensus.plot.lag.cdf = function(datum, xlim = c(0,1), xlab = "Inter-reading time (seconds)", ylab = "Percentile", main = paste("Inter-reading times (n=", nrow(datum), ")", sep=""))
{
  lags = diff(datum$Timestamp)
  lag.ecdf = ecdf(as.numeric(lags))
  num.rows = nrow(datum)
  plot(lag.ecdf, xlim = xlim, xlab = xlab, ylab = ylab, main = main)
}

#' Removes all data associated with a device ID from a data collection.
#' 
#' @param datum Data collection to process.
#' @param device.id Device ID to remove.
#' @return Data without a particular device ID.
#' @examples 
#' data.path = system.file("extdata", "example-data", package="SensusR")
#' data = sensus.read.json.files(data.path)
#' filtered.data = sensus.remove.device.id(data$AccelerometerDatum, "a448s0df98f")
sensus.remove.device.id = function(datum, device.id)
{
  return(datum[datum$DeviceId != device.id, ])
}

#' Trim leading white space from a string.
#' 
#' @param x String to trim.
#' @return Result of trimming.
#' @examples 
#' trim.leading("  asdfasdf")
trim.leading = function (x) sub("^\\s+", "", x)

#' Trim trailing white space from a string.
#' 
#' @param x String to trim.
#' @return Result of trimming.
#' @examples 
#' trim.trailing("asdfasdf  ")
trim.trailing = function (x) sub("\\s+$", "", x)

#' Trim leading and trailing white space from a string.
#' 
#' @param x String to trim.
#' @return Result of trimming.
#' @examples 
#' trim("  asdf  ")
trim = function (x) gsub("^\\s+|\\s+$", "", x)