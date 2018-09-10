var util = require('util');
var crypto = require('crypto');

var url = 'XXXXURLXXXX';                                       // for example:  https://sensus-notifications.servicebus.windows.net/sensus-notifications/messages
var sharedAccessKeyName = 'DefaultFullSharedAccessSignature';  // the key with full shared access to the notification hub
var sharedAccessKey = 'XXXXKEYXXXX';                           // the value of the DefaultFullSharedAccessSignature key
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
