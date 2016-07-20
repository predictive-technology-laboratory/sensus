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
$researcherLastName;
$researcherFirstName;
$researcherEmailAddress;
$researcherPassword;
if (empty($_POST['researcherLastName']) || empty($_POST['researcherFirstName']) || empty($_POST['researcherEmailAddress']) || empty($_POST['researcherPassword']))
        echo "Incomplete form";
if(!get_magic_quotes_gpc()) {
        $researcherLastName = addslashes($_POST['researcherLastName']);
        $researcherFirstName = addslashes($_POST['researcherFirstName']);
        $researcherEmailAddress = addslashes($_POST['researcherEmailAddress']);
	$researcherPassword = addslashes($_POST['researcherPassword']);
} else {
	$researcherLastName = $_POST['researcherLastName'];
        $researcherFirstName = $_POST['researcherFirstName'];
        $researcherEmailAddress = $_POST['researcherEmailAddress'];
        $researcherPassword = $_POST['researcherPassword'];
}

// build query
//if (empty($_POST['endDate'])) {
        $query = "INSERT INTO researcher (lastname, firstname, emailaddress, password) VALUES ('$researcherLastName', '$researcherFirstName', '$researcherEmailAddress', '$researcherPassword');";
//} else {
//      $query = "INSERT INTO study (name, startdate) VALUES ('$studyName', '$studyStartDate', '$studyEndDate')";
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
