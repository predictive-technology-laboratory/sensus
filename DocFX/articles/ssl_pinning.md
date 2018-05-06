---
uid:  ssl_pinning
---

# SSL Certificate Pinning
Sensus is able to transmit data from the device to remote servers (e.g., AWS S3). Sensus implements SSL certificate pinning 
to prevent man-in-the-middle attacks in which a third party poses as the remote server to intercept data. Certificate pinning 
is achieved by embedding the expected SSL public encryption key within the Sensus <xref:Sensus.Protocol>. Upon connecting to 
the AWS S3 server, Sensus receives the SSL public encryption key that the server wishes to use to secure the connection. If 
the server provides the expected public key (i.e., the one embedded within the <xref:Sensus.Protocol>, then we can be more 
confident that either (1) the server is actually our AWS S3 server, or (2) the server is a man-in-the-middle that is trying 
to use our public encryption key. In the latter case, as long as we have maintained control of the private key, the 
man-in-the-middle will not be able to decrypt data encrypted with the public encryption key. If the server does not provide 
the expected public key, Sensus will refuse to transmit data over the connection because it could potentially be decrypted 
by a man-in-the-middle server that holds the associated private encryption key.

Do the following to implement SSL certificate pinning within Sensus for the AWS S3 remote data store:
  * Set up an AWS CloudFront reverse proxy from a domain name that you own to the official AWS S3 endpoint (s3.amazonaws.com).
  * Install a new certificate into your CloudFront server using the AWS Certificate Manager.
  * Extract the public encryption key from your CloudFront endpoint (e.g., using the developer tools in Chrome on Windows).
  * Use your domain name as the <xref:Sensus.DataStores.Remote.AmazonS3RemoteDataStore.PinnedServiceURL>.
  * Use the extracted public key as the <xref:Sensus.DataStores.Remote.AmazonS3RemoteDataStore.PinnedPublicKey>.