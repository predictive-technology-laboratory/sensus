---
uid: protocol_distribution
---

# Protocol Distribution
Once a Protocol has been configured for Sensus, it must be distributed to participant devices. First, you must obtain 
the Protocol .json file (e.g., by tapping the Protocol within the Sensus app, selecting Share, and sending 
the Protocol to yourself via email). You can then distribute the Protocol .json file in one of several ways:

  1. Email attachment:  You can send the Protocol .json file to participants via email attachment. Upon receipt the 
     participant will tap the attachment to load it into Sensus. Note that not all email apps are compatible with this 
     approach.
     
  1. Via HTTPS URL:  You can post the Protocol .json file to web-accessible server (e.g., Dropbox) and distribute the 
     file's URL to participants. From within the Sensus app, the participant can add the Protocol by URL.
     
  1. Via QR-coded HTTPS URL:  This is similar to the previous approach, but relies on the Sensus QR code scanner to 
     obtain the URL of the protocol. This supports a convenient use case in which participants are provided a printed 
     paper containing the QR code. The user need only open the Sensus app and add the Protocol using the built-in QR 
     code scanner. The content of the QR code must be exactly as follows:
     ```
     sensus-protocol:URL
     ```
     where URL is the HTTPS URL location of the Protocol file to download.

## Common Distribution Issues
* If participants are using an iPhone with the Gmail app, they will not be able to load protocols via email attachment 
or URL embedded within an email. Instead, they will need to copy (press and hold) the URL in their email, open Safari, 
and paste the URL into the address bar. This is a limitation of iOS, and we have not found a solution for it. Please 
email us at uva.ptl@gmail.com if you encounter this problem.

## Locking and Unlocking a Protocol
As the experimenter, you can lock a Protocol (i.e., make it read-only) by tapping the Protocol, selecting "Lock", and 
entering a password. Recipients of the Protocol will be able to load the locked Protocol into Sensus and start/stop it; 
however, they will not be able to edit the Protocol's configuration without the password.

## Preventing Protocol Sharing
As the experimenter, you might wish to supply a Protocol to study participants via a webpage that you maintain. This 
can be achieved by following the tips above (sharing the file with yourself and posting it to a webpage accessible to 
your study participants). Furthermore, you might wish to ensure that participants cannot share the Protocol from their 
Sensus app after they have loaded it and joined the study. That is, you may wish to ensure that participants only join 
your study by loading the Protocol from an HTTPS hyperlink served up from your study's webpage. This might be necessary, 
for example, if your website provides consent information that participants must view before joining the study. To 
accomplish this, disable <xref:Sensus.Protocol.Shareable> and then lock the Protocol with a password. You can then post 
the Protocol to your study's website for loading into Sensus via HTTPS hyperlink, but study participants will not be able 
to directly share the loaded Protocol with others.

## Changing A Protocol's Encryption Key
For security, Sensus protocols are encrypted prior to leaving a device. Upon receipt at another device, Sensus decrypts
the protocol and loads it. As Sensus has grown, others have [redeployed](xref:redeploying) the Sensus codebase as alternative 
applications. Because the encryption/decryption key is specific to a particular application, protocols are not compatible 
between application deployments by default. If you possess the key for an app, then you can make a protocol residing in 
another app compatible with yours by setting the protocol's encryption key. To do this, edit the protocol and select 
Change Encryption Key. Then enter the key for your app. The protocol will be reencrypted and the resulting file will be 
shared. You should be able to load the shared file into your app after changing the encryption key. Read more about 
encryption keys [here](xref:Sensus.SensusServiceHelper.ENCRYPTION_KEY).