---
uid: anonymization
---

# Anonymization

Once Sensus is configured with a <xref:Sensus.Protocol>, it will be able to collect, save, and upload information 
from the device on which it is running. Sensus provides anonymization controls on a per-probe basis. For example, 
the <xref:Sensus.Probes.Location.ListeningLocationProbe> emits latitude-longitude coordinates. Each of these values 
can be anonymized by rounding off decimal places, thereby reducing the precision of the coordinates. For 
example, 73.438373758 degrees west longitude might become 73.43 degrees west longitude, resulting in a precision 
reduction of approximately 0.72 kilometers. See the individual <xref:Sensus.Probes.Probe> pages for more information 
about anonymization controls that are specific to each Probe. In general the following anonymization methods are available:

* `Anonymous Timeline`:  This anonymizer places timestamps on a timeline that is arbitrarily anchored at some starting time. Timestamps 
  thus anonymized do not have any absolute meaning. They are only meaningful relative to each other.
* `Round to Thousandths/Hundredths/Tenths/Ones/Tens/Hundreds`:  Rounds numbers to the associated place.
* `Hash`:  Computes a cryptographic, one-way hash of text.
* `Omit`:  Omits data value entirely.