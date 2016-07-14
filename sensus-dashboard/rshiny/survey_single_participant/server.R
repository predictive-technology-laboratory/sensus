library(shiny)
library(SensusR)
library(plotly)

rm(list=ls())

shinyServer(function(input, output) {

  # gets a list of directories in S3 bucket
  get.directories <- function() {
    sub("/", "", sub(".* ", "", sub(".*PRE", "", system(paste("aws s3 ls s3://summertesting"), intern = TRUE))))
  }



	# ============================
	# prepare and link UI elements
	# ============================

	output$idList <- renderUI({
		selectInput("participant", "Select participant:", as.list(sort(get.directories())))
	})



  # ===========
  # import data
  # ===========

  # TODO reimplement to query database instead of downloading files

  # downloads data from s3 and checks if there are new files
  check.data <- function() {
    directories <- get.directories()
    for (n in directories) {
      sensus.sync.from.aws.s3(paste0("s3://summertesting/", n, "/ScriptDatum"), local.path = paste0("/Users/wesbonelli/Documents/research/rshiny/data/summertesting/", n, "/ScriptDatum"), delete = FALSE)
    }
    paths = list.files("/Users/wesbonelli/Documents/research/rshiny/data/summertesting/", recursive = TRUE, full.names = TRUE, include.dirs = TRUE)
    length(paths)
  }
  # ingests new data
  refresh.data <- function() {
  		frame <- sensus.read.json(paste0("/Users/wesbonelli/Documents/research/rshiny/data/summertesting/", input$participant, "/ScriptDatum"))
  		lapply(frame$ScriptDatum$Timestamp, function(x) format(as.POSIXct(x, origin = "1582-10-14", format = "%a/%b/%d %H:%M:%S")))
  		frame
  }
  # refresh data every 15 seconds
  polled.data <- reactivePoll(15000, NULL, check.data, refresh.data)



	# ===========
  # set up plot
  # ===========

  output$plot <- renderPlotly({
  	p <- plot_ly(polled.data()$ScriptDatum, x = Timestamp, y = ScriptName, mode = "markers")
  	p <- layout(title = input$participant, xaxis = list(title = ""), yaxis = list(title = ""))
  	p
  })
})
