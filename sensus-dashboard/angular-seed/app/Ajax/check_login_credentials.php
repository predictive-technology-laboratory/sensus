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

// check POST values
$loginEmailAddress;
$loginPassword;
if (empty($_POST['loginEmailAddress']) || empty($_POST['loginPassword']))
        echo "Incomplete form";
if(!get_magic_quotes_gpc()) {
        $loginEmailAddress = addslashes($_POST['loginEmailAddress']);
        $loginPassword = addslashes($_POST['loginPassword']);
} else {
        $loginEmailAddress = $_POST['loginEmailAddress'];
        $loginPassword = $_POST['loginPassword'];
}

// build query
$query = "SELECT FROM researcher WHERE emailaddress = '$loginEmailAddress' AND password = '$loginPassword';";

// check credentials
$result = pg_query($handle, $query);
if ($result) {
	if (pg_num_rows($result) == 0)
                echo "null";
        else {
        	echo "pass";
	}
} else {
        echo "Query failed";
}

// close connection
pg_close($handle);
?>
