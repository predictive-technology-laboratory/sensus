library(shiny)
library(datasets)	
library(SensusR)
library(plotly)
library(rgdal)
library(ggplot2)
library(ggmap)
library(maps)
library(Jmisc)
library(RPostgreSQL)

rm(list=ls())

shinyServer(function(input, output) {

	# capitalizes the first letter of a string
  	cap <- function(x) {
		s <- strsplit(x, " ")[[1]]
		paste(toupper(substring(s, 1,1)), substring(s, 2), sep = "", collapse = " ")
	}

	# keep track of which panel is displayed
  	current.panel <- reactiveValues(
  		panel = "data.source"
  	)
  	output$panel <- renderText({
  		current.panel$panel
  	})
  	outputOptions(output, "panel", suspendWhenHidden = FALSE)





	# ===========================================================================================================================
	# data source (PostgreSQL or S3)
	# ===========================================================================================================================

	data.connection <- reactiveValues(source = "")

	# data source selection
	output$pg.data.source <- renderUI({
		actionButton("pg.data.source", label = "PostgreSQL")
	})
	observeEvent(input$pg.data.source, {
    	current.panel$panel <- "pg.settings"
  	})
	output$s3.data.source <- renderUI({
		actionButton("s3.data.source", label = "Amazon S3")
	})
  	observeEvent(input$s3.data.source, {
    	current.panel$panel <- "s3.settings"
  	})

	# PostgreSQL settings
	output$pg.settings.host <- renderUI({
		textInput("pg.settings.host", label = NULL, value = "", placeholder = "Host")
	})
	output$pg.settings.port <- renderUI({
		textInput("pg.settings.port", label = NULL, value = "", placeholder = "Port")
	})
	output$pg.settings.database <- renderUI({
		textInput("pg.settings.database", label = NULL, value = "", placeholder = "Database")
	})
	output$pg.settings.user <- renderUI({
		textInput("pg.settings.user", label = NULL, value = "", placeholder = "User")
	})
	output$pg.settings.password <- renderUI({
		textInput("pg.settings.password", label = NULL, value = "", placeholder = "Password")
	})
	output$pg.settings.submit <- renderUI({
		actionButton("pg.settings.submit", label = "Submit")
	})
	pg.settings.host <- reactive({
		input$pg.settings.host
	})
	pg.settings.port <- reactive({
		input$pg.settings.port
	})
	pg.settings.database <- reactive({
		input$pg.settings.database
	})
	pg.settings.user <- reactive({
		input$pg.settings.user
	})
	pg.settings.password <- reactive({
		input$pg.settings.password
	})
	pg.settings.message <- reactiveValues(
		message = NULL
	)
	output$pg.settings.message <- renderText({
		pg.settings.message$message
	})
	observeEvent(input$pg.settings.submit, {
		# try to connect to database
		tryCatch({
			connection <- dbConnect(dbDriver("PostgreSQL"), dbname = pg.settings.database(), host = pg.settings.host(), port = as.integer(pg.settings.port()), user = pg.settings.user(), password = pg.settings.password())
			if (dbExistsTable(connection, "datum") == TRUE) {
				dbDisconnect(connection)
				data.connection$source <- "pg"
	    		current.panel$panel <- "view.data"
			} else {
				pg.settings$message <- "Connection failed"
				print("Connection failed")
			}
		}, warning = function(w) {
			pg.settings.message$message <- w
		}, error = function(e) {
			pg.settings.message$message <- e
		}, finally = {})
	})
  	output$pg.settings.back <- renderUI({
		actionButton("pg.settings.back", label = "Back")
	})
  	observeEvent(input$pg.settings.back, {
    	current.panel$panel <- "data.source"
  	})

	# S3 settings
	output$s3.settings.accesskey <- renderUI({
		textInput("s3.settings.accesskey", label = NULL, value = "", placeholder = "Access key")
	})
	output$s3.settings.privatekey <- renderUI({
		textInput("s3.settings.privatekey", label = NULL, value = "", placeholder = "Private key")
	})
	output$s3.settings.bucket <- renderUI({
		textInput("s3.settings.bucket", label = NULL, value = "", placeholder = "Bucket")
	})
	output$s3.settings.download.directory <- renderUI({
		textInput("s3.settings.download.directory", label = NULL, value = "", placeholder = "Download directory")
	})
	output$s3.settings.submit <- renderUI({
		actionButton("s3.settings.submit", label = "Submit")
	})
	s3.settings.accesskey <- reactive({
  		input$s3.settings.accesskey
  	})
	s3.settings.privatekey <- reactive({
		input$s3.settings.privatekey
	})
	s3.settings.bucket <- reactive({
		input$s3.settings.bucket
	})
	s3.settings.download.directory <- reactive({
		input$s3.settings.download.directory
	})
	s3.settings.message <- reactiveValues(
		message = NULL
	)
	output$s3.settings.message <- renderText({
		s3.settings.message$message
	})
	s3.bucket.directories <- NULL
	observeEvent(input$s3.settings.submit, {
		# try to connect to S3
		tryCatch({
			s3.bucket.directories <- sub("/", "", sub(".* ", "", sub(".*PRE", "", system(paste0("aws s3 ls s3://", s3.settings.bucket()), intern = TRUE))))
			if (s3.bucket.directories != NULL) {
				data.connection$source <- "s3"
				current.panel$panel <- "view.data"
			} else {
				s3.settings$message <- "Connection failed"
			}
		}, warning = function(w) {
			s3.settings.message$message <- w
		}, error = function(e) {
			s3.settings.message$message <- e
		}, finally = {})
  	})
  	output$s3.settings.back <- renderUI({
		actionButton("s3.settings.back", label = "Back")
	})
  	observeEvent(input$s3.settings.back, {
    	current.panel$panel <- "data.source"
  	})







	# ===========================================================================================================================
	# data panel
	# ===========================================================================================================================

	# returns a list of directories in S3 bucket
  	get.directories <- function() {
  		sub("/", "", sub(".* ", "", sub(".*PRE", "", system(paste0("aws s3 ls s3://", s3.settings.bucket()), intern = TRUE))))
  	}

  	# returns the subset of S3 directories for which we have a specific data type
  	get.valid.directories <- function() {
  		directories <- get.directories()
	  	num <- length(directories)
	  	valid.directories <- list()
	  	count <- 1
	  	for (i in 1:num) {
	  		current <- directories[i]
	  		if (length(list.files(paste0(s3.settings.download.directory(), "/", current, "/", cap(input$type), "Datum"))) > 0) {
				valid.directories[count] <- as.integer(current)
				count <- count + 1
	  		}
	  	}
	  	valid.directories
  	}

	# checks to see if input$type has changed or the table corresponding to input$type has grown
	count <- 0
	check.data <- function() {
		# PostgreSQL
		if (data.connection$source == "pg") {
			connection <- dbConnect(dbDriver("PostgreSQL"), dbname = pg.settings.database(), host = pg.settings.host(), port = pg.settings.port(), user = pg.settings.user(), password = pg.settings.password())
			if (input$type == "script") {
				count <- (as.integer(dbGetQuery(connection, paste0("SELECT count(*) FROM ", input$type ,"datum"))) + as.integer(dbGetQuery(connection, paste0("SELECT count(*) FROM scriptrundatum"))))
			} else {
				count <- as.integer(dbGetQuery(connection, paste0("SELECT count(*) FROM ", input$type ,"datum")))
			}
			dbDisconnect(connection)
		}
		# S3
		if (data.connection$source == "s3") {
			directories <- get.directories()
			for (n in directories) {
				sensus.sync.from.aws.s3(paste0("s3://", s3.settings.bucket(), "/", n, "/", cap(input$type), "Datum"), local.path = paste0(s3.settings.download.directory(), "/", n, "/", cap(input$type), "Datum"), delete = FALSE)
			}
	  		count = list.files(s3.settings.download.directory(), recursive = TRUE, full.names = TRUE, include.dirs = TRUE)
		}
		count
	}

	# refreshes data
	refresh.data <- function() {
		frame <- NULL
		# PostgreSQL
		if (data.connection$source == "pg") {
			connection <- dbConnect(dbDriver("PostgreSQL"), dbname = pg.settings.database(), host = pg.settings.host(), port = as.integer(pg.settings.port()), user = pg.settings.user(), password = pg.settings.password())
			if (input$type == "script") {
				frame <- dbReadTable(connection, paste0(input$type, "datum"))
				frame$run <- "completion"
				temp.frame <- dbReadTable(connection, "scriptrundatum")
				temp.frame$run <- "trigger"
				temp.frame$groupid <- temp.frame$anonymized
				temp.frame$inputid <- temp.frame$anonymized
				temp.frame$scriptresponsevalue <- temp.frame$anonymized
				temp.frame$presentationtimestamp <- temp.frame$anonymized
				temp.frame$locationtimestamp <- temp.frame$anonymized
				temp.frame$completionrecords <- temp.frame$anonymized
				frame <- rbind(frame, temp.frame)
			} else {
				frame <- dbReadTable(connection, paste0(input$type, "datum"))
			}
			dbDisconnect(connection)
		}
		# S3
		if (data.connection$source == "s3") {
			frame <- list()
			valid.directories <- get.valid.directories()
			num <- length(valid.directories)
			type.upper.case <- cap(input$type)
		  	type.full <- paste0(cap (type.upper.case, "Datum"))
			if (mode == 1) {
		  		for (i in 1:num) {
		  			current <- toString(valid.directories[i])
		  			frame[[current]] <- (sensus.read.json(paste0(s3.settings.download.directory(), "/", current, "/", type.upper.case, "Datum"), recursive = FALSE))$type.full
		  			lapply(frame[[current]]$Timestamp, function(x) format(as.POSIXct(x, origin = "1582-10-14", format = "%a/%b/%d %H:%M:%S")))
		  		}
	  		}
			if (mode == 2) {
				frame <- data.frame(sensus.read.json(paste0(s3.settings.download.directory(), "/", valid.directories[1], "/", type.upper.case, "Datum"), recursive = FALSE)$type.full)
				frame$UserID <- valid.directories[1]
				for (i in 2:num) {
					current <- valid.directories[i]
					temp <- data.frame(sensus.read.json(paste0(s3.settings.download.directory(), "/", current, "/", type.upper.case, "Datum"), recursive = FALSE)$type.full)
					temp$UserID <- current
					frame <- rbind(frame, temp)
				}
				lapply(frame$Timestamp, function(x) format(as.POSIXct(x, origin = "1582-10-14", format = "%a/%b/%d %H:%M:%S")))
			}
		}
		frame
	}

	# poll for data at intervals
	data <- reactivePoll(15000, NULL, check.data, refresh.data)






	# ===========================================================================================================================
	# filters
	# ===========================================================================================================================

	# accelerometer
	output$accelerometer.x.axis <- renderUI({
		selectInput("accelerometer.x.axis", "X Axis:", c("amplitude", names(data())), selected = "timestamp")
	})
	output$accelerometer.y.axis <- renderUI({
		selectInput("accelerometer.y.axis", "Y Axis:", c("amplitude", names(data())))
	})
	output$accelerometer.separate.y.axes <- renderUI({
		checkboxInput("accelerometer.separate.y.axes", "Separate y axes:")
	})
	output$accelerometer.hide.legend <- renderUI({
		checkboxInput("accelerometer.hide.legend", "Hide legend:")
	})

	# altitude
	output$altitude.x.axis <- renderUI({
		selectInput("altitude.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$altitude.y.axis <- renderUI({
		selectInput("altitude.y.axis", "Y Axis:", names(data()))
	})
	output$altitude.separate.y.axes <- renderUI({
		checkboxInput("altitude.separate.y.axes", "Separate y axes:")
	})
	output$altitude.hide.legend <- renderUI({
		checkboxInput("altitude.hide.legend", "Hide legend:")
	})

	# ambient temperature
	output$temperature.x.axis <- renderUI({
		selectInput("temperature.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$temperature.y.axis <- renderUI({
		selectInput("temperature.y.axis", "Y Axis:", names(data()))
	})
	output$temperature.separate.y.axes <- renderUI({
		checkboxInput("temperature.separate.y.axes", "Separate y axes:")
	})
	output$temperature.hide.legend <- renderUI({
		checkboxInput("temperature.hide.legend", "Hide legend:")
	})

	# battery level
	output$battery.x.axis <- renderUI({
		selectInput("battery.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$battery.y.axis <- renderUI({
		selectInput("battery.y.axis", "Y Axis:", names(data()))
	})
	output$battery.separate.y.axes <- renderUI({
		checkboxInput("battery.separate.y.axes", "Separate y axes:")
	})
	output$battery.hide.legend <- renderUI({
		checkboxInput("battery.hide.legend", "Hide legend:")
	})

	# TODO biological sex... how to present this?

	# TOOO birthdate... how to present this?

	# TODO blood type... how to present this?

	# bluetooth device proximity
	output$bluetooth.x.axis <- renderUI({
		selectInput("bluetooth.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$bluetooth.y.axis <- renderUI({
		selectInput("bluetooth.y.axis", "Y Axis:", names(data()))
	})
	output$bluetooth.separate.y.axes <- renderUI({
		checkboxInput("bluetooth.separate.y.axes", "Separate y axes:")
	})
	output$bluetooth.hide.legend <- renderUI({
		checkboxInput("bluetooth.hide.legend", "Hide legend:")
	})

	# cell tower
	output$tower.x.axis <- renderUI({
		selectInput("tower.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$tower.y.axis <- renderUI({
		selectInput("tower.y.axis", "Y Axis:", names(data()))
	})
	output$tower.separate.y.axes <- renderUI({
		checkboxInput("tower.separate.y.axes", "Separate y axes:")
	})
	output$tower.hide.legend <- renderUI({
		checkboxInput("tower.hide.legend", "Hide legend:")
	})

	# compass
	output$compass.x.axis <- renderUI({
		selectInput("compass.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$compass.y.axis <- renderUI({
		selectInput("compass.y.axis", "Y Axis:", names(data()))
	})
	output$compass.separate.y.axes <- renderUI({
		checkboxInput("compass.separate.y.axes", "Separate y axes:")
	})
	output$compass.hide.legend <- renderUI({
		checkboxInput("compass.hide.legend", "Hide legend:")
	})

	# TODO facebook... how to present this?

	# TODO height... how to present this?

	# light
	output$light.x.axis <- renderUI({
		selectInput("light.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$light.y.axis <- renderUI({
		selectInput("light.y.axis", "Y Axis:", names(data()))
	})
	output$light.separate.y.axes <- renderUI({
		checkboxInput("light.separate.y.axes", "Separate y axes:")
	})
	output$light.hide.legend <- renderUI({
		checkboxInput("light.hide.legend", "Hide legend:")
	})

	# location
	output$location.plot.type <- renderUI({
		selectInput("location.plot.type", "Plot type:", c("Cartesian", "Map"), selected = "Cartesian")
	})
	output$location.x.axis <- renderUI({
		selectInput("location.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$location.y.axis <- renderUI({
		selectInput("location.y.axis", "Y Axis:", names(data()))
	})
	output$location.separate.y.axes <- renderUI({
		checkboxInput("location.separate.y.axes", "Separate y axes:")
	})
	output$location.hide.legend <- renderUI({
		checkboxInput("location.hide.legend", "Hide legend:")
	})

	# participation reward
	output$participation.x.axis <- renderUI({
		selectInput("participation.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$participation.y.axis <- renderUI({
		selectInput("participation.y.axis", "Y Axis:", names(data()))
	})
	output$participation.separate.y.axes <- renderUI({
		checkboxInput("participation.separate.y.axes", "Separate y axes:")
	})
	output$participation.hide.legend <- renderUI({
		checkboxInput("participation.hide.legend", "Hide legend:")
	})

	# point of interest proximity (model after location w/ options for cartesian vs map)
	output$poi.plot.type <- renderUI({
		selectInput("poi.plot.type", "Plot type:", c("Cartesian", "Map"), selected = "Cartesian")
	})
	output$poi.x.axis <- renderUI({
		selectInput("poi.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$poi.y.axis <- renderUI({
		selectInput("poi.y.axis", "Y Axis:", names(data()))
	})
	output$poi.separate.y.axes <- renderUI({
		checkboxInput("poi.separate.y.axes", "Separate y axes:")
	})
	output$poi.hide.legend <- renderUI({
		checkboxInput("poi.hide.legend", "Hide legend:")
	})

	# TODO protocol report (unnecessary? how to present?)

	# screen
	output$screen.x.axis <- renderUI({
		selectInput("screen.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$screen.y.axis <- renderUI({
		selectInput("screen.y.axis", "Y Axis:", names(data()))
	})
	output$screen.separate.y.axes <- renderUI({
		checkboxInput("screen.separate.y.axes", "Separate y axes:")
	})
	output$screen.hide.legend <- renderUI({
		checkboxInput("screen.hide.legend", "Hide legend:")
	})

	# script (and script run)
	output$script.name <- renderUI({
		selectInput("script.name", "Script name:", c("All", unique(data()$scriptname)))
	})
	output$script.x.axis <- renderUI({
		selectInput("script.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$script.y.axis <- renderUI({
		selectInput("script.y.axis", "Y Axis:", names(data()))
	})
	output$script.hide.legend <- renderUI({
		checkboxInput("script.hide.legend", "Hide legend:")
	})

	# sms
	output$sms.x.axis <- renderUI({
		selectInput("sms.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$sms.y.axis <- renderUI({
		selectInput("sms.y.axis", "Y Axis:", names(data()))
	})
	output$sms.separate.y.axes <- renderUI({
		checkboxInput("sms.separate.y.axes", "Separate y axes:")
	})
	output$sms.hide.legend <- renderUI({
		checkboxInput("sms.hide.legend", "Hide legend:")
	})

	# sound
	output$sound.x.axis <- renderUI({
		selectInput("sound.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$sound.y.axis <- renderUI({
		selectInput("sound.y.axis", "Y Axis:", names(data()))
	})
	output$sound.separate.y.axes <- renderUI({
		checkboxInput("sound.separate.y.axes", "Separate y axes:")
	})
	output$sound.hide.legend <- renderUI({
		checkboxInput("sound.hide.legend", "Hide legend:")
	})

	# speed
	output$speed.x.axis <- renderUI({
		selectInput("speed.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$speed.y.axis <- renderUI({
		selectInput("speed.y.axis", "Y Axis:", names(data()))
	})
	output$speed.separate.y.axes <- renderUI({
		checkboxInput("speed.separate.y.axes", "Separate y axes:")
	})
	output$speed.hide.legend <- renderUI({
		checkboxInput("speed.hide.legend", "Hide legend:")
	})

	# telephony
	output$telephony.x.axis <- renderUI({
		selectInput("telephony.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$telephony.y.axis <- renderUI({
		selectInput("telephony.y.axis", "Y Axis:", names(data()))
	})
	output$telephony.separate.y.axes <- renderUI({
		checkboxInput("telephony.separate.y.axes", "Separate y axes:")
	})
	output$telephony.hide.legend <- renderUI({
		checkboxInput("telephony.hide.legend", "Hide legend:")
	})

	# wlan
	output$wlan.x.axis <- renderUI({
		selectInput("wlan.x.axis", "X Axis:", names(data()), selected = "timestamp")
	})
	output$wlan.y.axis <- renderUI({
		selectInput("wlan.y.axis", "Y Axis:", names(data()))
	})
	output$wlan.separate.y.axes <- renderUI({
		checkboxInput("wlan.separate.y.axes", "Separate y axes:")
	})
	output$wlan.hide.legend <- renderUI({
		checkboxInput("wlan.hide.legend", "Hide legend:")
	})

	# filter if necessary
	filtered.data <- reactive({
		frame <- NULL
		if (input$type == "accelerometer") {
			# use subset of points if the table is very large
			num.rows <- nrow(data())
			if (num.rows > 10000) {
				frame <- data()[seq(1, num.rows, num.rows / 1000),]
			} else {
				frame <- data()
			}
			# compute averaged magnitude
			frame$amplitude <- abs(frame$x) + abs(frame$y) + abs(frame$z) / 3
		}
		if (input$type == "battery") {
			frame <- data()
		}
		if (input$type == "compass") {
			frame <- data()
		}
		if (input$type == "location") {
			# use subset of points if the table is very large
			num.rows <- nrow(data())
			if (num.rows > 10000) {
				frame <- data()[seq(1, num.rows, num.rows / 1000),]
			} else {
				frame <- data()
			}
		}
		if (input$type == "script") {
			if (data.connection$source == "pg") {
	  			if (input$script.name != "All") {
	  				frame <- data()[which(data()$scriptname == input$script.name),]
	  			} else {
	  				frame <- data()
	  			}
			}
			if (data.connection$source == "s3") {
				frame <- list()
				num <- length(valid.directories)
				if (mode == 1) {
					for (i in 1:num) {
			  			current <- toString(valid.directories[i])
			  			if (input$type == "script") {
				  			if (input$script.name != "All") {
				  				frame[[current]] <- data()[which(data()[[current]]$scriptname == input$script.name),]
				  			} else {
				  				frame[[current]] <- polled.data()[[current]]
				  			}
			  			}
			  		}
			  	}
			  	if (mode == 2) {
			  		frame <- NULL
					frame <- data()
				}
			}
		}
		if (input$type == "sms") {
			frame <- data()
		}
		if (input$type == "speed") {
			frame <- data()
		}
		if (input$type == "telephony") {
			frame <- data()
		}
		# TODO maybe shorten deviceids so they don't overlap in display?
		frame
	})

	# TODO catch display errors and show them in an alert box or something

	# ===========================================================================================================================
	# plot
	# ===========================================================================================================================
	output$accelerometer.plot <- renderPlotly({
		gg <- ggplot(filtered.data(), aes_string(x = input$accelerometer.x.axis, y = input$accelerometer.y.axis)) + geom_point(aes(color = deviceid), size = 2) + theme(strip.background = element_blank(), strip.text = element_blank())
		if (input$accelerometer.separate.y.axes) {
			gg <- gg + facet_grid(deviceid ~ .)
		}
		if (input$accelerometer.hide.legend) {
			gg <- gg + theme(legend.position = "none")
		}
		plot <- ggplotly(gg)
	})
	output$battery.plot <- renderPlotly({
  		gg <- ggplot(filtered.data(), aes_string(x = input$battery.x.axis, y = input$battery.y.axis)) + geom_point(aes(color = deviceid), size = 2) + theme(strip.background = element_blank(), strip.text = element_blank())
  		if (input$battery.separate.y.axes) {
			gg <- gg + facet_grid(deviceid ~ .)
		}
		if (input$battery.hide.legend) {
			gg <- gg + theme(legend.position = "none")
		}
		plot <- ggplotly(gg)
	})
	output$compass.plot <- renderPlotly({
  		gg <- ggplot(filtered.data(), aes_string(x = input$compass.x.axis, y = input$compass.y.axis)) + geom_point(aes(color = deviceid), size = 2) + theme(strip.background = element_blank(), strip.text = element_blank())
  		if (input$compass.separate.y.axes) {
			gg <- gg + facet_grid(deviceid ~ .)
		}
		if (input$compass.hide.legend) {
			gg <- gg + theme(legend.position = "none")
		}
		plot <- ggplotly(gg)
	})
	output$location.plot.cartesian <- renderPlotly({
		gg <- ggplot(filtered.data(), aes_string(x = input$location.x.axis, y = input$location.y.axis)) + geom_point(aes(color = deviceid), size = 2) + theme(strip.background = element_blank(), strip.text = element_blank())
		if (input$location.separate.y.axes) {
			gg <- gg + facet_grid(deviceid ~ .)
		}
		if (input$location.hide.legend) {
			gg <- gg + theme(legend.position = "none")
		}
		plot <- ggplotly(gg)
	})
	# TODO figure out why map won't display or reimplement... looks to be a common issue but none of the fixes work: https://stackoverflow.com/questions/34465947/plotly-maps-not-rendering-in-r
	output$location.plot.map <- renderPlotly({
		g <- list(
		  scope = 'usa',
		  projection = list(type = 'albers usa'),
		  showland = TRUE,
		  landcolor = toRGB("gray95"),
		  subunitcolor = toRGB("gray85"),
		  countrycolor = toRGB("gray85"),
		  countrywidth = 0.5,
		  subunitwidth = 0.5
		)
		plot <- plot_ly(filtered.data(), lat = latitude, lon = longitude, type = 'scattergeo', mode = 'markers') %>% layout(title = 'Map', geo = g)
	})
	output$script.plot <- renderPlotly({
		gg <- ggplot(filtered.data(), aes_string(x = input$script.x.axis, y = input$script.y.axis)) + geom_point(aes(color = run), size = 2) + facet_grid(deviceid ~ .) + theme(strip.background = element_blank(), strip.text = element_blank())
		if (input$script.hide.legend) {
			gg <- gg + theme(legend.position = "none")
		}
		plot <- ggplotly(gg)
	})
	output$sms.plot <- renderPlotly({
		gg <- ggplot(filtered.data(), aes_string(x = input$sms.x.axis, y = input$sms.y.axis)) + geom_point(aes(color = deviceid), size = 2) + theme(strip.background = element_blank(), strip.text = element_blank())
		if (input$sms.separate.y.axes) {
			gg <- gg + facet_grid(deviceid ~ .)
		}
		if (input$sms.hide.legend) {
			gg <- gg + theme(legend.position = "none")
		}
		plot <- ggplotly(gg)
	})
	output$speed.plot <- renderPlotly({
		gg <- ggplot(filtered.data(), aes_string(x = input$speed.x.axis, y = input$speed.y.axis)) + geom_point(aes(color = deviceid), size = 2) + theme(strip.background = element_blank(), strip.text = element_blank())
		if (input$speed.separate.y.axes) {
			gg <- gg + facet_grid(deviceid ~ .)
		}
		if (input$speed.hide.legend) {
			gg <- gg + theme(legend.position = "none")
		}
		plot <- ggplotly(gg)
	})
	output$telephony.plot <- renderPlotly({
		gg <- ggplot(filtered.data(), aes_string(x = input$telephony.x.axis, y = input$telephony.y.axis)) + geom_point(aes(color = deviceid), size = 2) + theme(strip.background = element_blank(), strip.text = element_blank())
		if (input$telephony.separate.y.axes) {
			gg <- gg + facet_grid(deviceid ~ .)
		}
		if (input$telephony.hide.legend) {
			gg <- gg + theme(legend.position = "none")
		}
		plot <- ggplotly(gg)
	})
})
