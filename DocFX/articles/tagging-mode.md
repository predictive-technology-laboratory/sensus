---
uid:  tagging_mode
---

# Tagging
Experimenters and researchers often desire to generate manually tagged data to support the creation of supervised
machine learning models. For example, if one were interested in generating accelerometry data while walking
in order to create a walking recognition model, then each <xref:Sensus.Probes.Movement.AccelerometerDatum>
would need to be tagged with ground truth labels (walking and not walking) in order to be used for supervised machine learning. 
Furthermore, the experimenter would need many replications of the activity to obtain representative data. Sensus supports this tagging
process in a straightforward way that alleviates most of the tedious work associated with keeping track of the readings
taken during such activities. Configure tagging as follows:

1. Allow tagging for the protocol by enabling <xref:Sensus.Protocol.AllowTagging>.
1. Enter a list of tags in the <xref:Sensus.Protocol.AvailableTags> field. These tags should describe the target 
activities. Ultimately, these tags will likely become the class labels to be predicted using supervised machine learning.
1. Start the protocol.
1. Tap the protocol and select "Tag Data".
1. Select the tags you wish to apply (optionally adding new tags if desired).
1. Press the Start button.
1. Engage in the target activity.
1. Press the Stop button and confirm the tagging.

Immediately upon pressing the Start button, each <xref:Sensus.Datum> that is stored will have the following
two fields set:

1. <xref:Sensus.Datum.TaggedEventId>:  A unique identifier for the tagging session that begins
with tapping the Start button and ends with tapping the Stop button. All <xref:Sensus.Datum> objects collected during 
this interval will have the same <xref:Sensus.Datum.TaggedEventId>.
1. <xref:Sensus.Datum.TaggedEventTags>:  The tags selected above.

Immediately upon pressing the Stop button, data will cease to be tagged and the above two fields will have empty 
values for all subsequent <xref:Sensus.Datum> objects. Note that the protocol will continuously collect data before 
the tagging starts and after the tagging ends. The only effect of tagging is to set the two fields above for all 
data within the start-stop interval.

After you have generated one or more taggings, tap the export button to send yourself a CSV file summarizing the tagged data.
The format of the CSV file is as follows:

```
tagged-event-id   start  end     tags
XXXX              XXXX   XXXX    A|B
```

The example above shows a single tagged event with placeholder values for the identifier and start/end times. This 
event indicates that two tags (A and B) were applied to all data generated during this interval. The CSV file does 
not contain the actual raw data, which will be stored either locally (if you have 
[configured local-only storage](xref:local_only_storage)) or in your <xref:Sensus.DataStores.Remote.RemoteDataStore>.