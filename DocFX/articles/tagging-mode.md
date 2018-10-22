---
uid:  tagging_mode
---

# Tagging
Experimenters and researchers often desire to generate manually tagged data to support the creation of supervised
machine learning models. For example, if one were interested in generating accelerometry data while walking
in order to create a walking recognition model, then these data would need to be tagged with ground truth 
labels (walking and not walking) in order to be used for supervised machine learning. Furthermore, the experimenter
would likely need many repeated runs of the activity to obtain representative data. Sensus supports this tagging
process in a straightforward way that alleviates most of the tedious work associated with keeping track of the readings 
taken during such activities. Configure tagging as follows:

1. Allow tagging for the protocol by enabling [tagging](xref:Sensus.Protocol.AllowTagging).
1. Enter a list of tags in the [AvailableTags](xref:Sensus.Protocol.AvailableTags) field.
1. Start the protocol.
1. Tap the protocol and select "Tag Data".
1. Select the tags you wish to apply (optionally adding new tags if desired).
1. Press the Start button.

Immediately upon pressing the Start button, each [Datum](xref:Sensus.Datum) that is stored will have the following
two fields set:

1. [TaggedEventId](xref:Sensus.Datum.TaggedEventId):  A unique identifier for the tagging session that begins
with tapping the Start button and ends with tapping the Stop button. All data collected during this interval will
have the same TaggedEventId.
1. [TaggedEventTags](xref:Sensus.Datum.TaggedEventTags):  The tags selected.

For example, if one wished to gather many examples of raising the phone up to the ear, then the following
steps should be taken:

1. Configure tagging as described above, selecting "raise-phone" (or any other name) as the tag.
1. Tap the Start button immediately before raising the phone.
1. Tap the Stop button immediately after raising the phone.

All data stored within the interval from Start to Stop will have set the two fields mentioned above. An example
setting of these fields is shown below:

```
...

"TaggedEventId": "9efb76ca-ceff-478a-9a99-4996fd100ba8",
"TaggedEventTags": {
  "$type": "System.Collections.Generic.List`1[[System.String, mscorlib]], mscorlib",
  "$values": [
    "raise-phone"
  ]
}

...
```