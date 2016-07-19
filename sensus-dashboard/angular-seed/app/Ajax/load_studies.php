<?php
// only allow access from dashboard
header('Access-Control-Allow-Origin: http://ec2-107-22-158-28.compute-1.amazonaws.com/sensus_dashboard/*');

// need to do this in order for the script to run... not sure why
ini_set('display_errors', 1);

// set up and check connection
$handle = pg_connect("host = sensus.cq86dmznaris.us-east-1.rds.amazonaws.com port = 5432 dbname = sensus_portal user = postgres password = sensus_dev");
if (!$handle) {
        echo 'Connection failed.';
        exit();
}

// load studies
$query = "SELECT name FROM study";
$result = pg_query($handle, $query);
if ($result) {
	while ($row = pg_fetch_assoc($result))
		$values[] = $row;
	$json = json_encode($values);
	echo $json;
} else {
	echo "Query failed.\n";
}

// close connection
pg_close($handle);
?>
