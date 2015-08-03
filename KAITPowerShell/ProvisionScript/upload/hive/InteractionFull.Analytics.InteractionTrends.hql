DROP TABLE IF EXISTS TempMaleCount;
CREATE TABLE TempMaleCount
(
	Day STRING,
	Hour INT,
	TimeInterval STRING,
	MinAge INT,
	MaxAge INT,
	AvgAge FLOAT,
	StdDevAge FLOAT,
	PersonCount INT
);

INSERT INTO TABLE TempMaleCount
SELECT	TO_DATE(CONCAT(SUBSTR(CAST(i.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(i.Timestamp AS STRING),12,8))) AS Day,
		HOUR(i.Timestamp) AS Hour,
		i.TimeInterval AS TimeInterval,
		MIN(i.Age) AS MinAge,
		MAX(i.Age) AS MaxAge,
		AVG(i.Age) AS AvgAge,
		STDDEV(i.Age) AS StdDevAge,
		COUNT(DISTINCT i.TrackingId) AS PersonCount
FROM InteractionsFull i
WHERE i.Gender IN('0','Male')
GROUP BY i.KioskState, TO_DATE(CONCAT(SUBSTR(CAST(i.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(i.Timestamp AS STRING),12,8))), HOUR(i.Timestamp), i.TimeInterval;

DROP TABLE IF EXISTS TempFemaleCount;
CREATE TABLE TempFemaleCount
(
	Day STRING,
	Hour INT,
	TimeInterval STRING,
	MinAge INT,
	MaxAge INT,
	AvgAge FLOAT,
	StdDevAge FLOAT,
	PersonCount INT
);

INSERT INTO TABLE TempFemaleCount
SELECT	TO_DATE(CONCAT(SUBSTR(CAST(i.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(i.Timestamp AS STRING),12,8))) AS Day,
		HOUR(i.Timestamp) AS Hour,
		i.TimeInterval AS TimeInterval,
		MIN(i.Age) AS MinAge,
		MAX(i.Age) AS MaxAge,
		AVG(i.Age) AS AvgAge,
		STDDEV(i.Age) AS StdDevAge,
		COALESCE(COUNT(DISTINCT i.TrackingId),0L) AS PersonCount
FROM InteractionsFull i
WHERE i.Gender IN('1','Female')
GROUP BY i.KioskState, TO_DATE(CONCAT(SUBSTR(CAST(i.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(i.Timestamp AS STRING),12,8))), HOUR(i.Timestamp), i.TimeInterval;

DROP TABLE IF EXISTS TempUnknownCount;
CREATE TABLE TempUnknownCount
(
	Day STRING,
	Hour INT,
	TimeInterval STRING,
	MinAge INT,
	MaxAge INT,
	AvgAge FLOAT,
	StdDevAge FLOAT,
	PersonCount INT
);

INSERT INTO TABLE TempUnknownCount
SELECT	TO_DATE(CONCAT(SUBSTR(CAST(i.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(i.Timestamp AS STRING),12,8))) AS Day,
		HOUR(i.Timestamp) AS Hour,
		i.TimeInterval AS TimeInterval,
		MIN(i.Age) AS MinAge,
		MAX(i.Age) AS MaxAge,
		AVG(i.Age) AS AvgAge,
		STDDEV(i.Age) AS StdDevAge,
		COALESCE(COUNT(DISTINCT i.TrackingId),0L) AS PersonCount
FROM InteractionsFull i
WHERE i.Gender IN('2','Unknown')
GROUP BY i.KioskState, TO_DATE(CONCAT(SUBSTR(CAST(i.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(i.Timestamp AS STRING),12,8))), HOUR(i.Timestamp), i.TimeInterval;

DROP TABLE IF EXISTS InteractionGenderTrends;
CREATE TABLE InteractionGenderTrends
(
	Day STRING,
	Hour INT,
	TimeInterval STRING,
	MaleCount INT,
	FemaleCount INT,
	UnknownCount INT,
	TotalGenderCount INT,
	PercentMale FLOAT,
	MinMaleAge INT,
	MaxMaleAge INT,
	AvgMaleAge FLOAT,
	StdDevMaleAge FLOAT,
	PercentFemale FLOAT,
	MinFemaleAge INT,
	MaxFemaleAge INT,
	AvgFemaleAge FLOAT,
	StdDevFemaleAge FLOAT,
	PercentUnknown FLOAT,
	MinUnknownAge INT,
	MaxUnknownAge INT,
	AvgUnknownAge FLOAT,
	StdDevUnknownAge FLOAT
);

INSERT INTO TABLE InteractionGenderTrends
SELECT  COALESCE(tmc.Day,tfc.Day,tuc.Day) AS Day,
		COALESCE(tmc.Hour,tfc.Hour,tuc.Hour) AS Hour,
		COALESCE(tmc.TimeInterval,tfc.TimeInterval,tuc.TimeInterval) AS TimeInterval,
		COALESCE(tmc.PersonCount,0L) AS MaleCount,		
		COALESCE(tfc.PersonCount,0L) AS FemaleCount,		
		COALESCE(tuc.PersonCount,0L) AS UnknownCount,
		(COALESCE(tmc.PersonCount,0L) + COALESCE(tfc.PersonCount,0L) + COALESCE(tuc.PersonCount,0L)) AS TotalGenderCount,		
		CASE -- Make sure we don't divide by 0
			WHEN (COALESCE(tmc.PersonCount,0L) + COALESCE(tfc.PersonCount,0L) + COALESCE(tuc.PersonCount,0L)) = 0
				THEN 0
			ELSE 
				(COALESCE(tmc.PersonCount,0L) / (COALESCE(tmc.PersonCount,0L) + COALESCE(tfc.PersonCount,0L) + COALESCE(tuc.PersonCount,0L)))
		END	AS PercentMale,
		COALESCE(tmc.MinAge,0L) AS MinMaleAge,
		COALESCE(tmc.MaxAge,0L) AS MaxMaleAge,
		COALESCE(tmc.AvgAge,0L) AS AvgMaleAge,
		COALESCE(tmc.StdDevAge,0L) AS StdDevMaleAge,
		CASE
			WHEN (COALESCE(tmc.PersonCount,0L) + COALESCE(tfc.PersonCount,0L) + COALESCE(tuc.PersonCount,0L)) = 0
				THEN 0
			ELSE
				(COALESCE(tfc.PersonCount,0L) / (COALESCE(tmc.PersonCount,0L) + COALESCE(tfc.PersonCount,0L) + COALESCE(tuc.PersonCount,0L)))
		END AS PercentFemale,
		COALESCE(tfc.MinAge,0L) AS MinFemaleAge,
		COALESCE(tfc.MaxAge,0L) AS MaxFemaleAge,
		COALESCE(tfc.AvgAge,0L) AS AvgFemaleAge,
		COALESCE(tfc.StdDevAge,0L) AS StdDevFemaleAge,
		CASE
			WHEN (COALESCE(tmc.PersonCount,0L) + COALESCE(tfc.PersonCount,0L) + COALESCE(tuc.PersonCount,0L)) = 0
				THEN 0
			ELSE		
				(COALESCE(tuc.PersonCount,0L) / (COALESCE(tmc.PersonCount,0L) + COALESCE(tfc.PersonCount,0L) + COALESCE(tuc.PersonCount,0L)))
		END AS PercentUnknown,	
		COALESCE(tuc.MinAge,0L) AS MinUnknownAge,
		COALESCE(tuc.MaxAge,0L) AS MaxUnknownAge,
		COALESCE(tuc.AvgAge,0L) AS AvgUnknownAge,
		COALESCE(tuc.StdDevAge,0L) AS StdDevUnknownAge
FROM TempMaleCount tmc
	FULL OUTER JOIN TempFemaleCount tfc ON (tmc.Day = tfc.Day AND tmc.Hour = tfc.Hour AND tmc.TimeInterval = tfc.TimeInterval)
	FULL OUTER JOIN TempUnknownCount tuc ON (tmc.Day = tuc.Day AND tmc.Hour = tuc.Hour AND tmc.TimeInterval = tuc.TimeInterval);

DROP TABLE IF EXISTS InteractionTrends;
CREATE TABLE InteractionTrends
(
	Zone STRING,
	Day STRING,
	Hour INT,
	TimeInterval STRING,
	PersonsInZone INT,	
	PercentMale FLOAT,
	PercentFemale FLOAT,
	PercentUnknown FLOAT,
	TotalGenderCount INT,
	MinMaleAge INT,
	MaxMaleAge INT,
	AvgMaleAge FLOAT,
	StdDevMaleAge FLOAT,	
	MinFemaleAge INT,
	MaxFemaleAge INT,
	AvgFemaleAge FLOAT,
	StdDevFemaleAge FLOAT,	
	MinUnknownAge INT,
	MaxUnknownAge INT,
	AvgUnknownAge FLOAT,
	StdDevUnknownAge FLOAT	
);

INSERT INTO TABLE InteractionTrends
SELECT  i.KioskState AS Zone,
		TO_DATE(CONCAT(SUBSTR(CAST(i.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(i.Timestamp AS STRING),12,8))) AS Day,
		HOUR(i.Timestamp) AS Hour,
		i.TimeInterval AS TimeInterval,		
		COUNT(DISTINCT i.TrackingId) AS PersonsInZone,		
		AVG(g.PercentMale) AS PercentMale,
		AVG(g.PercentFemale) AS PercentFemale,
		AVG(g.PercentUnknown) AS PercentUnknown,
		AVG(g.TotalGenderCount) AS TotalGenderCount,
		AVG(g.MinMaleAge) AS MinMaleAge,
		AVG(g.MaxMaleAge) AS MaxMaleAge,
		AVG(g.AvgMaleAge) AS AvgMaleAge,
		AVG(g.StdDevMaleAge) AS StdDevMaleAge,
		AVG(g.MinFemaleAge) AS MinFemaleAge,
		AVG(g.MaxFemaleAge) AS MaxFemaleAge,
		AVG(g.AvgFemaleAge) AS AvgFemaleAge,
		AVG(g.StdDevFemaleAge) AS StdDevFemaleAge,
		AVG(g.AvgUnknownAge) AS AvgUnknownAge,
		AVG(g.MinUnknownAge) AS MinUnknownAge,
		AVG(g.MaxUnknownAge) AS MaxUnknownAge,
		AVG(g.StdDevUnknownAge) AS StdDevUnknownAge
FROM InteractionsFull i	
	INNER JOIN InteractionGenderTrends g ON (g.Day = TO_DATE(CONCAT(SUBSTR(CAST(i.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(i.Timestamp AS STRING),12,8))) AND g.Hour = HOUR(i.Timestamp) AND g.TimeInterval = i.TimeInterval)
GROUP BY TO_DATE(CONCAT(SUBSTR(CAST(i.Timestamp AS STRING),0,10), ' ',SUBSTR(CAST(i.Timestamp AS STRING),12,8))), HOUR(i.Timestamp), i.TimeInterval, i.KioskState;

-- clean up temp tables
DROP TABLE IF EXISTS TempMaleCount;
DROP TABLE IF EXISTS TempFemaleCount;
DROP TABLE IF EXISTS TempUnknownCount;
