// usage:  node ./get-sas.js [azure namespace] [azure hub] [key]
// purpose:  generates a cryptographically signed shared access signature for the
// azure push notification hub, with a specified expiration duration.
var util = require('util');
var crypto = require('crypto');

// get args to script (first two are "node" and script name)
var args = process.argv.slice(2);

var url = 'https://' + args[0] + '.servicebus.windows.net/' + args[1] + '/messages';

var expiry = new Date(); 
expiry.setMinutes(expiry.getMinutes() + 5);
var expiryEpoch = expiry instanceof Date ? expiry.getTime() / 1000 : expiry;

var data = util.format('%s\n%s', encodeURIComponent(url), expiryEpoch);
var algorithm = crypto.createHmac('sha256', args[2]);
algorithm.update(data);
var signature = algorithm.digest('base64');

var token = util.format('SharedAccessSignature sr=%s&sig=%s&se=%s&skn=%s',
			encodeURIComponent(url),
			encodeURIComponent(signature),
			encodeURIComponent(expiryEpoch),
			encodeURIComponent('DefaultFullSharedAccessSignature')
		       );

console.log(token);
