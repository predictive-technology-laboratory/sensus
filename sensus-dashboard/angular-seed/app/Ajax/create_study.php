<?php
// only allow access from dashboard
header('Access-Control-Allow-Origin: http://ec2-107-22-158-28.compute-1.amazonaws.com/sensus_dashboard/*');

ini_set('display_errors', 1);

// set up and check connection
$handle = pg_connect("host = sensus.cq86dmznaris.us-east-1.rds.amazonaws.com port = 5432 dbname = sensus_portal user = postgres password = sensus_dev");
if (!$handle) {
	echo 'Connection failed.';
	exit();
} else {
	echo 'Connection successful.';
}

// check POST values
$studyName;
$studyStartDate;
$studyEndDate;
if (empty($_POST['studyName']) || empty($_POST['studyStartDate']))
	echo "Incomplete form";
if(!get_magic_quotes_gpc()) {
	$studyName = addslashes($_POST['studyName']);
	$studyStartDate = addslashes($_POST['studyStartDate']);
	$studyEndDate = addslashes($_POST['studyEndDate']);
} else {
	$studyName = $_POST['studyName'];
	$studyStartDate = $_POST['studyStartDate'];
	$studyEndDate = $_POST['studyEndDate'];
}

// build query
//if (empty($_POST['endDate'])) {
	$query = "INSERT INTO study (name) VALUES ('$studyName');";
//} else {
//	$query = "INSERT INTO study (name, startdate) VALUES ('$studyName', '$studyStartDate', '$studyEndDate')";
//}

// insert values
$result = pg_query($handle, $query);
if (!$result) {
	echo "Could not insert.";
} else {
	echo "Insert successful.";
}

// close connection
pg_close($handle);
?>
