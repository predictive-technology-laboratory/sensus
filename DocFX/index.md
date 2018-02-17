![Sensus Banner](/sensus/images/GitHubBanner.png)

Sensus is an end-to-end system for mobile sensing using iOS and Android devices. Sensus is able to monitor a wide range of sensors and data events, and it is designed to interact with the user to solicit additional information via scheduled or sensor-triggered surveys. It coordinates the collection, anonymization, and storage of this information locally on the device and remotely on cloud servers. Sensus has been designed and implemented at the University of Virginia [Predictive Technology Laboratory](http://ptl.sys.virginia.edu/ptl) for research in public health and personal safety. However, Sensus is a general-purpose system. Researchers in other fields, industrial companies, and individuals interested in self-monitoring might find it useful.

Sensus's key features include the following:

* **Cross-platform with native performance** Most apps like Sensus are available for either Android or iOS but not both. This limits the target audience and threatens the validity of any study that requires a representative population of smartphone users. Sensus runs on Android and iOS and emphasizes a uniform user experience.
* **Comprehensive sensing** A complete list of internal hardware and software sensors, as well as external wearable and beacon-based sensors, can be found [here](xref:Sensus.Probes.Probe).
* **Device-initiated custom surveys** Sensus can prompt users to complete custom surveys that are scheduled or triggered by sensed data. For example, surveys can be triggered on the basis of GPS location, proximity to points of interest, speed, acceleration, light levels, sound levels, after phone calls or text messages, and so on. Virtually any [probed data](https://github.com/predictive-technology-laboratory/sensus/wiki/Probe#current-sensus-probes) can be used to trigger surveys, or you can schedule surveys to run during particular time blocks.
* **Fine-grained anonymization controls** Each type of sensed data has several fine-grained facets that can be individually anonymized.
* **Minimal infrastructure requirements** Sensus is entirely self-contained. No external servers are required unless you would like to push data to the cloud (e.g., Amazon) for centralized storage, which is supported with minimal effort through configuration scripts that are provided with Sensus.
* **Analytics** Sensus is paired with an analytics library written in R ([[SensusR]]), making data ingest, display, and analysis straightforward.
* **In-app configuration of sensing plans** There is only one app to install. You use Sensus to configure a plan for sensors and surveys. This plan can be immediately distributed to participants for execution. No programming experience is needed, and you do not need to release your own version of Sensus to the Android and iOS stores.
* **Randomized controlled trials of sensing plans** Multiple sensing plans can be combined into a bundle, which is then delivered to participants. Upon receipt, participants are randomly assigned to one of the plans. This supports randomized controlled trials of plan variations.

# Obtaining and Using Sensus
Sensus is available for Android from [the Google Play Store](https://play.google.com/store/apps/details?id=edu.virginia.sie.ptl.sensus) and for iOS from [iTunes](https://itunes.apple.com/us/app/sensus-uva/id1053498740). We are developing an end-user [[Manual|Sensus Manual]]. Take a look at our [[Common Problems]] and [[Frequently Asked Questions]] pages for tips on using Sensus. You can also take a look at the [[Contributed Resources]] from others who have used Sensus.

If you would like to try out the beta (early release) versions of Sensus for Android, you can join the testing program [here](https://play.google.com/apps/testing/edu.virginia.sie.ptl.sensus).

# Citing Sensus
If you are using Sensus in your research and would like to cite it, please use the following citation:

"[H. Xiong, Y. Huang, L. E. Barnes, and M. S. Gerber. Sensus: A Cross-Platform, General-Purpose System for Mobile Crowdsensing in Human-Subject Studies. In _Proceedings of the 2016 ACM International Joint Conference on Pervasive and Ubiquitous Computing_, pages 415-426. ACM, 2016](https://dl.acm.org/citation.cfm?id=2971711)"

# Support
Have questions or suggestions? Interested in using Sensus in one of your studies? Try one of the following.
* Post your question to the [Sensus Google Group](https://groups.google.com/forum/#!forum/sensus-mobile-sensing).
* Email us at uva.ptl@gmail.com.
* Check the [[Common Problems]] and [[Frequently Asked Questions]] pages.
* Log an [issue](https://github.com/predictive-technology-laboratory/sensus/issues/new) on GitHub.

# Developing Sensus
Sensus has been developed under the Apache 2.0 license, and you are welcome to contribute to its development. Below, you will find some useful information:
* [[Configuring a Development System]]
* [[Source Code Architecture]]
* [[Developing Sensus]]
* [Issues](https://github.com/MatthewGerber/sensus/issues)
* [[Releasing Sensus]]