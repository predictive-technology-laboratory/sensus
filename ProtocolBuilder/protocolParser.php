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
	


	
//forceDownloadQR("https://s3.amazonaws.com/sensus-protocols/$name.json",$name);

	
	$data = [ 'res' => "https://s3.amazonaws.com/sensus-protocols/$name.json",'name' =>"$name.png" ]; 
	header('Content-Type: application/json');
	echo json_encode($data);

   

  /* // Download the contents of the object.
  $result = $s3Client->getObject([
      'Bucket' => 'mousetracker',
      'Key'    => 'data.txt'
  ]);
  echo $result['ObjectURL'] . "allo";
  // Print the body of the result by indexing into the result object.
  echo $result['Body']; */ 
  

  
   


  function forceDownloadQR($url,$name, $width = 300, $height = 300) {
    $url    = urlencode($url);
    $image  = 'http://chart.apis.google.com/chart?chs='.$width.'x'.$height.'&cht=qr&chl='.$url;
    $file = file_get_contents($image);
    header("Content-type: image/png");
    header("Content-Disposition: attachment; filename=".$name.".png;");
	header("Content-Transfer-Encoding: Binary");
	header('Cache-Control: must-revalidate, post-check=0, pre-check=0');
	header('Expires: 0');
    header("Content-length: " . strlen($file)); // tells file size
    header("Pragma: no-cache");
    echo $file;
    die;
}  

  
?>