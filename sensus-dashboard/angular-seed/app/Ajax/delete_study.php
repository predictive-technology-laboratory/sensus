<?php
// allow localhost access
header('Access-Control-Allow-Origin: http://ec2-107-22-158-28.compute-1.amazonaws.com/sensus_dashboard/*');

// need to do this in order for the script to run... not sure why
ini_set('display_errors', 1);

// set up and check connection
$handle = pg_connect("host = sensus.cq86dmznaris.us-east-1.rds.amazonaws.com port = 5432 dbname = sensus_portal user = postgres password = sensus_dev");
if (!$handle) {
        echo 'Connection failed.';
        exit();
}

// check POST values
$studyName;
if (empty($_POST['studyName']))
	echo "Incomplete form";
if(! get_magic_quotes_gpc() ) {
	$studyName = addslashes($_POST['studyName']);
} else {
	$studyName = $_POST['studyName'];
}

// delete study
$query = "DELETE FROM study WHERE name = '$studyName'";
$result = pg_query($handle, $query);
if (!$result) {
	echo "Query failed.";
}

// close connection
pg_close($handle);
?>
