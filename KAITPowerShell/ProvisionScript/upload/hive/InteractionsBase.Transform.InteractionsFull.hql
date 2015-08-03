DROP TABLE IF EXISTS InteractionsFull;
CREATE TABLE InteractionsFull
(
	TrackingId BIGINT,
	Action STRING,
	DeviceSelection STRING,
	DeviceSelectionState STRING,
	Duration Timestamp,
	KioskState STRING, 
	Age TINYINT,
	Gender STRING,
	GenderConfidence FLOAT,
	UnixTimestamp INT,
	Timestamp TIMESTAMP,
	TimeInterval STRING,
	Joint STRING,
	X FLOAT,
	Y FLOAT,
	Z FLOAT,
	Orientation FLOAT
);

INSERT INTO TABLE InteractionsFull
SELECT	i.TrackingId AS TrackingId,
		i.Action AS Action, 
		i.DeviceSelection AS DeviceSelection,
		i.DeviceSelectionState AS DeviceSelectionState,
		i.Duration AS Duration,
		i.KioskState AS KioskState, 
		d.Age AS Age,
		d.Gender AS Gender,
		d.GenderConfidence AS GenderConfidence,
		UNIX_Timestamp(CONCAT(SUBSTR(i.Timestamp, 1, 10), ' ', SUBSTR(i.Timestamp, 12, 8))) AS UnixTimestamp,
		i.Timestamp AS Timestamp,
		s.TimeInterval AS TimeInterval,
		s.Joint AS Joint,
		s.X AS X,
		s.Y AS Y,
		s.Z AS Z,
		s.Orientation AS Orientation
FROM Demographics d
	INNER JOIN Interactions i ON d.TrackingId = i.TrackingId
	INNER JOIN SkeletonTrack s ON d.TrackingId = s.TrackingId
WHERE s.Joint IN('Head','HandLeft','HandRight');

