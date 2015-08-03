DROP TABLE IF EXISTS DemographicsRaw;
CREATE EXTERNAL TABLE DemographicsRaw(
	raw_json STRING
)
STORED AS TEXTFILE LOCATION 'wasb://telemetry@${hiveconf:RawDataStorageAccount}.blob.core.windows.net/demographics/';

DROP TABLE IF EXISTS Demographics;
CREATE TABLE Demographics
(	
	TrackingId BIGINT,
	Gender STRING,
	Age TINYINT,
	GenderConfidence FLOAT,
	FaceID STRING,
	FaceMatch STRING,
	FaceConfidence FLOAT,
	FaceScore FLOAT,
	FrontalFaceScore FLOAT,
	HeadConfidence FLOAT
);

INSERT INTO TABLE Demographics
SELECT	CAST(GET_JSON_OBJECT(d.raw_json,'$.TrackingId') AS BIGINT) AS TrackingId,
		GET_JSON_OBJECT(d.raw_json,'$.Gender') AS Gender,
		CAST(GET_JSON_OBJECT(d.raw_json,'$.Age') AS TINYINT) AS Age,
		CAST(GET_JSON_OBJECT(d.raw_json,'$.GenderConfidence') AS FLOAT) AS GenderConfidence,
		GET_JSON_OBJECT(d.raw_json,'$.FaceID') AS FaceID,
		GET_JSON_OBJECT(d.raw_json,'$.FaceMatch') AS FaceMatch,
		CAST(GET_JSON_OBJECT(d.raw_json,'$.FaceConfidence') AS FLOAT) AS FaceConfidence,
		CAST(GET_JSON_OBJECT(d.raw_json,'$.FaceScore') AS FLOAT) AS FaceScore,
		CAST(GET_JSON_OBJECT(d.raw_json,'$.FrontalFaceScore') AS FLOAT) AS FrontalFaceScore,
		CAST(GET_JSON_OBJECT(d.raw_json,'$.HeadConfidence') AS FLOAT) AS HeadConfidence
FROM DemographicsRaw d;		
