library(shiny)
library(plotly)

shinyUI(
  fluidPage(
    title = "Data Viewer",
    br(),
    # data source
    conditionalPanel("Select a data source:",
      condition = "output.panel == 'data.source'",
      uiOutput("pg.data.source"),
      uiOutput("s3.data.source")
    ),
    # PostgreSQL settings
    conditionalPanel("PostgreSQL settings:",
      condition = "output.panel == 'pg.settings'",
      uiOutput("pg.settings.host"),
      uiOutput("pg.settings.port"),
      uiOutput("pg.settings.database"),
      uiOutput("pg.settings.user"),
      uiOutput("pg.settings.password"),
      uiOutput("pg.settings.submit"),
      uiOutput("pg.settings.back"),
      uiOutput("pg.settings.message")
    ),
    # S3 settings
    conditionalPanel("Amazon S3 settings:",
      condition = "output.panel == 's3.settings'",
      uiOutput("s3.settings.accesskey"),
      uiOutput("s3.settings.privatekey"),
      uiOutput("s3.settings.bucket"),
      uiOutput("s3.settings.download.directory"),
      uiOutput("s3.settings.submit"),
      uiOutput("s3.settings.back"),
      uiOutput("s3.settings.message")
    ),
    # view data
    conditionalPanel(
      condition = "output.panel == 'view.data'",
      tabsetPanel(id = "type",
        # accelerometer
        tabPanel("Accelerometer", value = "accelerometer",
          br(),
          fluidRow(
            column(3, uiOutput("accelerometer.x.axis")),
            column(3, uiOutput("accelerometer.y.axis")),
            column(3, uiOutput("accelerometer.separate.y.axes"), uiOutput("accelerometer.hide.legend"))
          ),
          hr(), 
          plotlyOutput("accelerometer.plot")
        ),
        # altitude
        tabPanel("Altitude", value = "altitude",
          br(),
          fluidRow(
            column(3, uiOutput("altitude.x.axis")),
            column(3, uiOutput("altitude.y.axis")),
            column(3, uiOutput("altitude.separate.y.axes"), uiOutput("altitude.hide.legend"))
          ),
          hr(), 
          plotlyOutput("altitude.plot")
        ),
        # ambient temperature
        tabPanel("Temperature", value = "ambienttemperature",
          br(),
          fluidRow(
            column(3, uiOutput("temperature.x.axis")),
            column(3, uiOutput("temperature.y.axis")),
            column(3, uiOutput("temperature.separate.y.axes"), uiOutput("temperature.hide.legend"))
          ),
          hr(), 
          plotlyOutput("temperature.plot")
        ),
        # battery level
        tabPanel("Battery", value = "battery",
          br(),
          fluidRow(
            column(3, uiOutput("battery.x.axis")),
            column(3, uiOutput("battery.y.axis")),
            column(3, uiOutput("battery.separate.y.axes"), uiOutput("battery.hide.legend"))
          ),
          hr(),
          plotlyOutput("battery.plot")
        ),
        # bluetooth device proximity
        tabPanel("Bluetooth", value = "bluetoothdeviceproximity",
          br(),
          fluidRow(
            column(3, uiOutput("bluetooth.x.axis")),
            column(3, uiOutput("bluetooth.y.axis")),
            column(3, uiOutput("bluetooth.separate.y.axes"), uiOutput("bluetooth.hide.legend"))
          ),
          hr(),
          plotlyOutput("bluetooth.plot")
        ),
        # cell tower
        tabPanel("Cell tower", value = "celltower",
          br(),
          fluidRow(
            column(3, uiOutput("tower.x.axis")),
            column(3, uiOutput("tower.y.axis")),
            column(3, uiOutput("tower.separate.y.axes"), uiOutput("tower.hide.legend"))
          ),
          hr(),
          plotlyOutput("tower.plot")
        ),
        # compass
        tabPanel("Compass", value = "compass",
          br(),
          fluidRow(
            column(3, uiOutput("compass.x.axis")),
            column(3, uiOutput("compass.y.axis")),
            column(3, uiOutput("compass.separate.y.axes"), uiOutput("compass.hide.legend"))
          ),
          hr(),
          plotlyOutput("compass.plot")
        ),
        # facebook
        tabPanel("Facebook", value = "facebook",
          br(),
          fluidRow(
          ),
          hr(),
          plotlyOutput("facebook.plot")
        ),
        # light
        tabPanel("Light", value = "light",
          br(),
          fluidRow(
            column(3, uiOutput("light.x.axis")),
            column(3, uiOutput("light.y.axis")),
            column(3, uiOutput("light.separate.y.axes"), uiOutput("light.hide.legend"))
          ),
          hr(),
          plotlyOutput("light.plot")
        ),
        # location
        tabPanel("Location", value = "location",
          br(),
          fluidRow(
            column(3, uiOutput("location.plot.type")),
            conditionalPanel(
              condition = "input['location.plot.type'] == 'Cartesian'",
              column(3, uiOutput("location.x.axis")),
              column(3, uiOutput("location.y.axis")),
              column(3, uiOutput("location.separate.y.axes"), uiOutput("location.hide.legend"))
            )
          ),
          hr(),
          conditionalPanel(
            condition = "input['location.plot.type'] == 'Cartesian'",
            plotlyOutput("location.plot.cartesian")
          ),
          conditionalPanel(
            condition = "input['location.plot.type'] == 'Map'",
            plotlyOutput("location.plot.map")
          )
        ),
        # participation reward
        tabPanel("Participation", value = "participationreward",
          br(),
          fluidRow(
            column(3, uiOutput("participation.x.axis")),
            column(3, uiOutput("participation.y.axis")),
            column(3, uiOutput("participation.separate.y.axes"), uiOutput("participation.hide.legend"))
          ),
          hr(),
          plotlyOutput("participation.plot")
        ),
        # point of interest proximity
        tabPanel("POI", value = "pointofinterestproximity",
          br(),
          fluidRow(
            column(3, uiOutput("poi.x.axis")),
            column(3, uiOutput("poi.y.axis")),
            column(3, uiOutput("poi.separate.y.axes"), uiOutput("poi.hide.legend"))
          ),
          hr(),
          plotlyOutput("poi.plot")
        ),
        # screen
        tabPanel("Screen", value = "screen",
          br(),
          fluidRow(
            column(3, uiOutput("screen.x.axis")),
            column(3, uiOutput("screen.y.axis")),
            column(3, uiOutput("screen.separate.y.axes"), uiOutput("screen.hide.legend"))
          ),
          hr(),
          plotlyOutput("screen.plot")
        ),
        # script
        tabPanel("Script", value = "script",
          br(),
          fluidRow(
            column(3, uiOutput("script.name")),
            column(3, uiOutput("script.x.axis")),
            column(3, uiOutput("script.y.axis")),
            column(3, uiOutput("script.hide.legend"))
          ),
          hr(),
          plotlyOutput("script.plot")
        ),
        # sms
        tabPanel("SMS", value = "sms",
          br(),
          fluidRow(
            column(3, uiOutput("sms.x.axis")),
            column(3, uiOutput("sms.y.axis")),
            column(3, uiOutput("sms.separate.y.axes"), uiOutput("sms.hide.legend"))
          ),
          hr(),
          plotlyOutput("sms.plot")
        ),
        # sound
        tabPanel("Sound", value = "sound",
          br(),
          fluidRow(
            column(3, uiOutput("sound.x.axis")),
            column(3, uiOutput("sound.y.axis")),
            column(3, uiOutput("sound.separate.y.axes"), uiOutput("sound.hide.legend"))
          ),
          hr(),
          plotlyOutput("sound.plot")
        ),
        # speed
        tabPanel("Speed", value = "speed",
          br(),
          fluidRow(
            column(3, uiOutput("speed.x.axis")),
            column(3, uiOutput("speed.y.axis")),
            column(3, uiOutput("speed.separate.y.axes"), uiOutput("speed.hide.legend"))
          ),
          hr(),
          plotlyOutput("speed.plot")
        ),
        # telephony
        tabPanel("Telephony", value = "telephony",
          br(),
          fluidRow(
            column(3, uiOutput("telephony.x.axis")),
            column(3, uiOutput("telephony.y.axis")),
            column(3, uiOutput("telephony.separate.y.axes"), uiOutput("telephony.hide.legend"))
          ),
          hr(),
          plotlyOutput("telephony.plot")
        ),
        # wlan
        tabPanel("WLAN", value = "wlan",
          br(),
          fluidRow(
            column(3, uiOutput("wlan.x.axis")),
            column(3, uiOutput("wlan.y.axis")),
            column(3, uiOutput("wlan.separate.y.axes"), uiOutput("wlan.hide.legend"))
          ),
          hr(),
          plotlyOutput("wlan.plot")
        )
      )
    )
  )
)
