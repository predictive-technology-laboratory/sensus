---
uid: sensus_r
---

# SensusR

SensusR is a package written for the [R Statistical Computing Environment](http://www.r-project.org). The purpose of SensusR 
is to make ingest, analysis, and visualization of Sensus data straightforward.

## Installing SensusR
There are several options for installing SensusR.

### From CRAN
SensusR is available directly through [CRAN](http://cran.r-project.org/web/packages/SensusR/index.html), but you should 
probably install it via the usual method (`install.packages` on the command line or through the menu system in RStudio).

### From Source
You can also obtain source packages for SensusR directly from the Sensus 
GitHub [repository](https://github.com/MatthewGerber/sensus/tree/master/SensusR/Releases). If you download one of these 
source archives, use the following command to install the package, editing the path appropriately for your machine:

    install.packages("/path/to/SensusR_VERSION.tar.gz", repos = NULL, type = "source")

In the above command, VERSION is the SensusR version that you are installing. After installation, call `library(SensusR)` to 
load the package and see the package help for details on available functions and their use. Some notes on these releases follow:

* 2.1.0:  In order to plot location data, you'll need to reinstall the `ggmap` package from source with `install.packages("ggmap", type = "source")`.

## Obtaining Sensus Data
The approach to obtaining your Sensus data will depending on how you have configured your <xref:Sensus.Protocol>. In particular, 
it will depend on your choice of <xref:Sensus.DataStores.Local.LocalDataStore>, <xref:Sensus.DataStores.Remote.RemoteDataStore>, 
and configuration of each. Below are some common configurations and approaches for downloading the associated Sensus data.

#### File Local Data Store + Amazon S3 Remote Data Store
To obtain data stored in your Amazon S3 bucket, follow the <xref:Sensus.DataStores.Remote.AmazonS3RemoteDataStore>. This is 
typically the best approach to remote storage.

#### File Local Data Store Only (No Remote Data Store)
In some situations (e.g., where privacy is a major concern), it might be better to store all Sensus data locally on the 
participant's device. The tradeoff, of course, is usage of the device's on-board storage, which might be substantial over 
time. Nonetheless, if remote storage is not possible, then no other alternatives exist. To share the data that have 
accumulated in the Local Data Store, go [here](xref:Sensus.DataStores.Local.LocalDataStore.UploadToRemoteDataStore).

## Releasing SensusR
The following steps were adapted from http://www.r-bloggers.com/how-to-check-your-package-with-r-devel.

1. Run `sudo pkgutil --forget org.r-project.R.XXXX.fw.pkg` so that the installer does not overwrite your current R installation. 
   In this command, `XXXX` is your Mac OS version. This can be found with `pkgutil --packages | grep r-project`.
1. Rename your R.app and R64.app or move them temporarily into another folder, as the installer of R-devel probably will replace 
   them by new version that are not compatible with your existing stable R version.
1. Install [R-devel](http://r.research.att.com) if you haven't already.
1. Install [RSwitch](http://r.research.att.com/#other).
1. Use RSwitch to change to your normal version of R and then build the SensusR source package within RStudio via Build > Build 
   Source Package. Move the resulting package to the `SensusR/Releases` directory.
1. Use RSwitch to change to R-devel.
1. Install packages which your own packages depends on. You have to do it from source, as the binaries for the R-devel do not 
   exist. As of SensusR 2.0.0 this command is `install.packages(c("jsonlite","lubridate","plyr","sp","ggmap","ggplot2"), type = "source")`. Note 
   that ggplot2 requires the jpeg package. In order to install the latter from source you first need to install jpeg via Homebrew using `brew install jpeg`.
1. Check the package:  `R CMD check SensusR/Releases/SensusR_2.0.0.tar.gz --as-cran`
