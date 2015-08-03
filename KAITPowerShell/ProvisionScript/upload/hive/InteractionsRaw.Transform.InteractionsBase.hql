DROP TABLE IF EXISTS InteractionsRaw;
CREATE EXTERNAL TABLE InteractionsRaw(
	raw_json STRING
)
STORED AS TEXTFILE LOCATION 'wasb://telemetry@${hiveconf:RawDataStorageAccount}.blob.core.windows.net/interactions/';

DROP TABLE IF EXISTS Interactions;
CREATE TABLE Interactions
(	
	TrackingId BIGINT,
	KioskState STRING,
	Action STRING,
	Duration STRING,
	Timestamp TIMESTAMP,
	TimeInterval STRING,
	DeviceSelection STRING,
	DeviceSelectionState STRING
);

INSERT INTO TABLE Interactions
SELECT	CAST(GET_JSON_OBJECT(i.raw_json,'$.TrackingId') AS BIGINT) AS TrackingId,
		GET_JSON_OBJECT(i.raw_json,'$.KioskState') AS KioskState,
		GET_JSON_OBJECT(i.raw_json,'$.Action') AS Action,
		GET_JSON_OBJECT(i.raw_json,'$.Duration') AS Duration,
		CAST(CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),0,10), ' ',SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,10)) AS TIMESTAMP) AS Timestamp,
		CASE
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),0,10), ' ',SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,10)) AS TIMESTAMP)) < 8) 
				THEN CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,2), ':00:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),0,10), ' ',SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,10)) AS TIMESTAMP)) >= 8 
				AND MINUTE(CAST(CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),0,10), ' ',SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,10)) AS TIMESTAMP)) < 23) 
					THEN CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,2), ':15:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),0,10), ' ',SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,10)) AS TIMESTAMP)) >= 23 
				AND MINUTE(CAST(CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),0,10), ' ',SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,10)) AS TIMESTAMP)) < 38) 
					THEN CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,2), ':30:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),0,10), ' ',SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,10)) AS TIMESTAMP)) >= 38 
				AND MINUTE(CAST(CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),0,10), ' ',SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,10)) AS TIMESTAMP)) < 53)
					THEN CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,2), ':45:00')
			WHEN (MINUTE(CAST(CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),0,10), ' ',SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,10)) AS TIMESTAMP)) >= 53)
				THEN CONCAT(SUBSTR(GET_JSON_OBJECT(i.raw_json,'$.TimeStamp'),12,2), ':00:00')
		END AS TimeInterval,
		GET_JSON_OBJECT(i.raw_json,'$.DeviceSelection') AS DeviceSelection,
		GET_JSON_OBJECT(i.raw_json,'$.DeviceSelectionState') AS DeviceSelectionState	
FROM InteractionsRaw i;
