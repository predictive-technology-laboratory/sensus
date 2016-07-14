import java.io.BufferedReader;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.SortedMap;
import java.util.TreeMap;
import java.util.HashMap;
import java.util.Timer;
import java.util.TimerTask;
import java.util.Date;
import java.text.SimpleDateFormat;
import java.util.concurrent.TimeUnit;
import org.json.JSONArray;
import org.json.JSONObject;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.ResultSetMetaData;
import java.sql.Statement;
import com.amazonaws.AmazonClientException;
import com.amazonaws.AmazonServiceException;
import com.amazonaws.auth.AWSCredentials;
import com.amazonaws.auth.profile.ProfileCredentialsProvider;
import com.amazonaws.regions.Region;
import com.amazonaws.regions.Regions;
import com.amazonaws.services.s3.AmazonS3;
import com.amazonaws.services.s3.AmazonS3Client;
import com.amazonaws.services.s3.model.Bucket;
import com.amazonaws.services.s3.model.GetObjectRequest;
import com.amazonaws.services.s3.model.ListObjectsRequest;
import com.amazonaws.services.s3.model.ObjectListing;
import com.amazonaws.services.s3.model.PutObjectRequest;
import com.amazonaws.services.s3.model.S3Object;
import com.amazonaws.services.s3.model.S3ObjectSummary;

public class DatabaseUpdater {
	
	private static HashMap<String, Integer> _encounteredFiles;
	
	public DatabaseUpdater() {
		_encounteredFiles = new HashMap<String, Integer>();
	}

	public static void main(String[] args) {
		DatabaseUpdater _databaseUpdater = new DatabaseUpdater();
		BufferedReader _reader;
		String _path = "";
		String _server = "";
		String _port = "";
		String _database = "";
		String _user = "";
		String _password = "";
		
		// get user input
		try {
			_reader = new BufferedReader(new InputStreamReader(System.in));
			// ask where to download files
			System.out.print("Download to directory: ");
			_path = _reader.readLine();
			// get PostgreSQL info
			System.out.print("PostgreSQL server: ");
			_server = _reader.readLine();
			System.out.print("PostgreSQL port: ");
			_port = _reader.readLine();
			System.out.print("PostgreSQL database: ");
			_database = _reader.readLine();
			System.out.print("PostgreSQL user: ");
			_user = _reader.readLine();
			System.out.print("PostgreSQL password: ");
			_password = _reader.readLine();
		} catch (Exception _ex) {
			System.out.println(_ex.getClass().getName() + ": " + _ex.getMessage());
			_ex.printStackTrace();
			System.exit(0);
		}
		
		// schedule data refresh
		Timer _timer = new Timer();
		_timer.scheduleAtFixedRate(new Refresh(_path, _server, _port, _database, _user, _password,  _databaseUpdater._encounteredFiles), 0, 120000);
	}
}

class Refresh extends TimerTask  {
	
	private String _path;
	private String _pgServer;
	private String _pgPort;
	private String _pgDatabase;
	private String _pgUser;
	private String _pgPassword;
	private static HashMap<String, Integer> _encounteredFiles;

	public Refresh(String path, String pgServer, String pgPort, String pgDatabase, String pgUser, String pgPassword, HashMap<String, Integer> encounteredFiles) {
		this._path = path;
		this._pgServer = pgServer;
		this._pgPort = pgPort;
		this._pgDatabase = pgDatabase;
		this._pgUser = pgUser;
		this._pgPassword = pgPassword;
		this._encounteredFiles = encounteredFiles;
	}

	public void run() {
		try {
			// get data from S3
			System.out.print("Syncing from S3");
			Process _process;
			String _command = "aws s3 cp --recursive s3://summertesting " + _path;
			ArrayList<File> _newFiles = new ArrayList<File>();
			ArrayList<String> _awsOutput = new ArrayList<String>();
			try {
				System.out.print(".");
				_process = Runtime.getRuntime().exec(_command);
				System.out.print(".");
//				_process.waitFor();
//				System.out.print(".");
				BufferedReader _reader = new BufferedReader(new InputStreamReader(_process.getInputStream()));
				String _output = null;
				while ((_output = _reader.readLine()) != null) {
				    _awsOutput.add(_output);
				}
				System.out.print(".");
				// read output to get list of expected files
				for (String _line : _awsOutput) {
					if (!_line.contains("download")) {
						continue;
					}
					String[] _split = _line.split(" ");
					String _path = _split[_split.length - 1];
					// if we haven't seen this file before, add it to _newFiles and _encounteredFiles
					if (!_encounteredFiles.keySet().contains(_path)) {
						_newFiles.add(new File("/Users/wesbonelli/Documents/research/rshiny/" + _path));
						_encounteredFiles.put(_path, 0);
					}
				}
				System.out.print(".");
				// TODO make sure we have the files we expect
//				System.out.println("Checking to make sure files exist...");
				
			} catch (Exception _ex) {
				System.err.println(_ex.getClass().getName() + ": " + _ex.getMessage());
				_ex.printStackTrace();
				System.exit(0);
			}
			
			// parse each file and update database
			System.out.println("");
			System.out.println("Updating database from files...");
			Thread.sleep(500);
			ArrayList<String> _types = new ArrayList<String>();		// keep track of encountered types so we know which tables to check for duplicates
			for (File _file : _newFiles) {
				// connect to PostgreSQL
				Connection _connection = null;
				Statement _statement = null;
				try {
			         Class.forName("org.postgresql.Driver");
			         _connection = DriverManager
			            .getConnection("jdbc:postgresql://" + _pgServer + ":" + _pgPort + "/" + _pgDatabase,
			            _pgUser, _pgPassword);
			      } catch (Exception _ex) {
			         System.err.println(_ex.getClass().getName() + ": "+ _ex.getMessage());
			         _ex.printStackTrace();
			         System.exit(0);
			      }
				_connection.setAutoCommit(false);
				_statement = _connection.createStatement();
				System.out.println("	" + _file.getName());
				String _type = null;			// type of Datum
				FileInputStream _stream = new FileInputStream(_file);
				byte[] _bytes = new byte[(int) _file.length()];
				_stream.read(_bytes);
				_stream.close();
				String _content = new String(_bytes, "UTF-8");
				JSONArray _jsonArray = new JSONArray(_content);
				
				// loop through each curly-bracketed json array (single data entry)
				PreparedStatement _prepared = null;
				boolean _first = true;
				for (int i = 0; i < _jsonArray.length(); i += 1) {
					if (_jsonArray.isNull(i)) {
						continue;
					}
					JSONObject _jsonObject = _jsonArray.getJSONObject(i);
					SortedMap<String, Object> _entry = new TreeMap<String, Object>();
					
					// loop through each value in the current entry
					for (Object _key : _jsonObject.keySet()) {
						String _keyStr = ((String) _key);
						Object _obj = _jsonObject.get(_keyStr);
						String _column = "";
						Object _value = null;
						switch (_keyStr) {
							case "$type":
								// set type so we can differentiate between shared cases below
								String[] _split1 = String.valueOf(_obj).split(" ");
								String[] _split2 = _split1[0].split("\\.");
								String _sub = _split2[_split2.length - 1];
								_type = _sub.substring(0, _sub.length() - 1).toLowerCase();
								_column = "type";
								if (!_types.contains(_type)) {
									_types.add(_type);
								}
								break;
							// "on" cannot be the name of a PostgreSQL column
							case "On":
								_column = "ison";
								_value = Boolean.parseBoolean(_obj.toString());
								break;
							default:
								_column = _keyStr.toLowerCase();	// PostgreSQL column names are all lowercase
								_value = _obj.toString();			// add everything as a string for now
								break;
						}
						if (!_column.equals("type") && !_column.isEmpty()) {
							_entry.put(_column, _value);
						}
					}
					
					// if first object in file, get column names and prepare insert statement
					String _query = null;
					SortedMap<String, Integer> _columns = new TreeMap<String, Integer>();
					if (_first) {
						_query = "SELECT * FROM " + _type;
						ResultSet _results = _statement.executeQuery(_query);
						ResultSetMetaData _resultsMetaData = _results.getMetaData();
						int _numColumns = _resultsMetaData.getColumnCount();
						_columns = new TreeMap<String, Integer>();
						for (int j = 1; j <= _numColumns; j += 1) {
							_columns.put(_resultsMetaData.getColumnName(j), _resultsMetaData.getColumnType(j));
						}
						_query = "INSERT INTO " + _type + " (";
						for (String _key : _columns.keySet()) {
							_query += _key + ", ";
						}
						_query = _query.substring(0, _query.length() - 2);		// remove last comma
						_query += ") SELECT ";
						for (int j = 0; j < _numColumns; j += 1) {
							_query += "?, ";
						}
						_query = _query.substring(0, _query.length() - 2);		// remove last comma
						_query += ";";
						_prepared = _connection.prepareStatement(_query);
						_first = false;
					}
					
					// assign parameter types and add batch
					int _index = 1;
					for (String _key : _columns.keySet()) {
						int _valueType = _columns.get(_key);
						if (_valueType == -5) {				// BIGINT
							try {
								_prepared.setInt(_index, Integer.parseInt(_entry.get(_key).toString()));
							} catch (Exception _ex) {
								_prepared.setNull(_index, java.sql.Types.NULL);
							}
						} else if (_valueType == 12) {		// VARCHAR
							try {
								_prepared.setString(_index, _entry.get(_key).toString());
							} catch (Exception _ex) {
								_prepared.setNull(_index, java.sql.Types.NULL);
							}
						} else if (_valueType == 16) {		// BOOLEAN
							try {
								_prepared.setBoolean(_index, Boolean.parseBoolean(_entry.get(_key).toString()));
							} catch (Exception _ex) {
								_prepared.setNull(_index, java.sql.Types.NULL);
							}
						} else if (_valueType == 93) {		// TIMESTAMP
							try {
								String _shortened = _entry.get(_key).toString().substring(0, 20);
								Date _timestamp = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss").parse(_shortened);
								_prepared.setTimestamp(_index, new java.sql.Timestamp(_timestamp.getTime()));
							} catch (Exception _ex) {
								_prepared.setNull(_index, java.sql.Types.NULL);
							}
						} else if (_valueType == 8) {		// DOUBLE
							try {
								_prepared.setDouble(_index, Double.parseDouble(_entry.get(_key).toString()));
							} catch (Exception _ex) {
								_prepared.setNull(_index, java.sql.Types.NULL);
							}
						} else {
							_prepared.setNull(_index, java.sql.Types.NULL);
						}
						_index += 1;
					}
					_prepared.addBatch();
				}
				int[] results = _prepared.executeBatch();
				for (int x : results) {
					if (x < 1) {
						System.out.println("Failed insert");
					}
				}
				_prepared.close();
				_connection.commit();
				_connection.close();
			}
			
//			// check for and remove duplicates
//			String _query = null;
//			for (String _type : _types) {
//				_query = "SELECT * FROM (SELECT id, ROW_NUMBER() OVER(PARTITION BY id ORDER BY id asc) AS ROW FROM " + _type + ") duplicates WHERE duplicates.Row > 1;";
//				_statement.executeUpdate(_query);
//			}
			
		System.out.println("Done.");
		} catch (Exception _ex) {
			System.err.println(_ex.getClass().getName() + ": " + _ex.getMessage());
			_ex.printStackTrace();
		}
	}
}
