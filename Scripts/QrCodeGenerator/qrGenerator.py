# -*- coding: utf-8 -*-
"""
Created on Mon Oct 14 17:51:42 2019

@author: mehdi
"""

import pyqrcode 
  
  

json = "www.geeksforgeeks.org"
PIDprefix="test"
PIDstart=1000
PIDsize=10
Filedestination="C:/Users/mehdi/Documents/Python_scripts/qrCode_generator/results"

for i in range(PIDsize):
    PID = PIDstart+i
    string = "sensus-protocol:"+json+":"+PIDprefix+str(PID)
    print(string)
    url = pyqrcode.create(string)
    url.png(Filedestination+"/"+PIDprefix+str(PID)+".png", scale=8)

 
  

