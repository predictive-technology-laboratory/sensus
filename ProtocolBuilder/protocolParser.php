<?php

$myjson = json_encode($_POST);


 // allow access....

  require 'resources/aws-autoloader.php';
	
	$ctime= time();
	$study="";
	if ( $_SERVER['REQUEST_METHOD'] == 'POST' ) {
		$study=$_POST["study_name"];
	}
	$name = $study."_".$ctime;



  // Use the us-west-2 region and latest version of each client.
  $sharedConfig = [
    'version'     => 'latest',
    'region'      => 'us-east-1',
    'credentials' => [
       'key'    => 'xxx',
       'secret' => 'xxx',
     ],
	 'http'    => [
        'verify' => false
    ]
  ];

  // Create an SDK class used to share configuration across clients.
  $sdk = new Aws\Sdk($sharedConfig);

  // Create an Amazon S3 client using the shared configuration data.
  $s3Client = $sdk->createS3();
	 
  $key = $myjson;
  
  // Send a PutObject request and get the result object.
   $result = $s3Client->putObject([
      'Bucket' => 'sensus-protocols',
      'Key'    =>  "$name.json",
      'Body'   => "$myjson",
	  'ContentType'  => 'application/json',
    'ACL'          => 'public-read'
  ]); 
	



	
	$data = [ 'res' => "https://s3.amazonaws.com/sensus-protocols/$name.json",'name' =>"$name.png" ]; 
	header('Content-Type: application/json');
	echo json_encode($data);

   

 
  

  
 

  
?>