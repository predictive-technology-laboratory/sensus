read.sensus.json = function(path, convert.to.local.timezone)
{
  data = list()
  con = file(path, open="r")
  while (TRUE) 
  {
    line = readLines(con, n = 1, warn = FALSE)
    
    if(length(line) == 0)
      break
    
    json = jsonlite::fromJSON(line)
    
    datum.type = strsplit(json$"$type", ",")[[1]][1]
    datum.type = tail(strsplit(datum.type, "[.]")[[1]], n=1)    
    
    json = json[-which(names(json) %in% c("$id", "$type", "Id", "ProbeType"))]
    
    if(!(datum.type %in% names(data)))
    {
      data[[datum.type]] = json
      names(data[[datum.type]]) = names(json)
    } 
    else
    {
      data[[datum.type]] = rbind(data[[datum.type]], json)
    }
  }
  
  close(con)
    
  for(datum.type in names(data))
  {
    rownames(data[[datum.type]]) = NULL
    data[[datum.type]] = as.data.frame(data[[datum.type]])
    
    for(col.name in names(data[[datum.type]]))
    {
      data[[datum.type]][,col.name] = unlist(data[[datum.type]][,col.name])
    }
    
    data[[datum.type]]$Timestamp = strptime(data[[datum.type]]$Timestamp, format = "%Y-%m-%dT%H:%M:%OS", tz="UTC")
    
    if(convert.to.local.timezone)
    {
      data[[datum.type]]$Timestamp = with_tz(data[[datum.type]]$Timestamp, Sys.timezone())
    }
  }
  
  return(data)  
}