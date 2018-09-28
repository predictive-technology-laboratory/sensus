var util = require('util');
var crypto = require('crypto');

var url = 'XXXXURLXXXX';                                       // https://NAMESPACE.servicebus.windows.net/HUB/messages -- where NAMESPACE is 
                                                               // your Azure push notification namespace name and HUB is your Azure push 
                                                               // notification HUB name.

var sharedAccessKeyName = 'DefaultFullSharedAccessSignature';  // the key with full shared access to the notification hub. this should be the 
                                                               // default value. if you use another key name, replace this value.

var sharedAccessKey = 'XXXXKEYXXXX';                           // the value of the DefaultFullSharedAccessSignature key (e.g., cVRantasldfkjaslkj3flkjelfrz+a3lkjflkj=)

var expiry = new Date(); 
expiry.setMinutes(expiry.getMinutes() + 5);                    // the signature will be valid for 5 minutes
var expiryEpoch = expiry instanceof Date ? expiry.getTime() / 1000 : expiry;

var data = util.format('%s\n%s', encodeURIComponent(url), expiryEpoch);
var algorithm = crypto.createHmac('sha256', sharedAccessKey);
algorithm.update(data);
var signature = algorithm.digest('base64');

var token = util.format('SharedAccessSignature sr=%s&sig=%s&se=%s&skn=%s',
			encodeURIComponent(url),
			encodeURIComponent(signature),
			encodeURIComponent(expiryEpoch),
			encodeURIComponent(sharedAccessKeyName)
		       );

console.log(token);
