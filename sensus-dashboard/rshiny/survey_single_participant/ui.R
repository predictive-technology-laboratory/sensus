library(shiny)
library(plotly)

shinyUI(fluidPage(
    br(),
    title = "Survey Data, Single Participant",
    plotlyOutput("plot"),
    hr(),
    fluidRow(
      column(3,
        h4("Single Participant View"),
        uiOutput("idList")
      )
    )
))
