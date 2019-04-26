---
uid: authentication_servers
---

# Authentication Servers
Authentication servers allow backend infrastructure to manage accounts and
credentials that are used by Sensus.

## Required REST Endpoints

## Updating Protocols Mid-Study
Since the authentication server is able to serve up new versions of protocols
mid-study, care should be taken when editing protocols for distribution. The
steps below constitute the recommended approach for editing a protocol mid-study
when using an authentication server.

1. Delete all existing protocols from Sensus.
1. Scan a study QR code to pull down the most current version of the protocol
from the authentication server.
1. Edit the protocol as desired.
1. Run the edited protocol on only your phone for at least 1 day to test 
the new settings. It is important to test changes before distributing the new
protocol version to participants.
1. Having tested the new settings, set the protocol's identifier from within 
the protocol settings. Use a random identifier to mark the new version. Setting 
a new protocol identifier is crucial, as it signals participants' devices to 
update their version once the update process described herein is complete. If 
multiple protocols are used within a single study (e.g., one for Android and 
one for iOS), ensure that the identifiers match across protocols.
1. From within Sensus on your phone, share the protocol with the person 
responsible for uploading protocols into the authentication server. The best way
to share the protocol is to edit it and tap the "Share" button from within the
protocol settings.
1. After confirming upload into the authentication server, delete the protocol 
from your phone, rescan the study QR code to pull down the new version from the
authentication server, inspect the protocol to confirm that it reflects your 
edits, and run the new version of the protocol to test it.
