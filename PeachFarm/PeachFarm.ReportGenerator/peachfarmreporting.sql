CREATE DATABASE  IF NOT EXISTS `peachfarmreporting` /*!40100 DEFAULT CHARACTER SET utf8 */;
USE `peachfarmreporting`;
-- MySQL dump 10.13  Distrib 5.6.13, for Win32 (x86)
--
-- Host: 10.0.1.39    Database: peachfarmreporting
-- ------------------------------------------------------
-- Server version	5.6.14

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `jobs`
--

DROP TABLE IF EXISTS `jobs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `jobs` (
  `jobs_id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `jobid` varchar(12) DEFAULT NULL,
  `target` varchar(100) DEFAULT NULL,
  `startdate` datetime DEFAULT NULL,
  `mongoid` varchar(100) DEFAULT NULL,
  `pitfilename` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`jobs_id`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `metrics_faults`
--

DROP TABLE IF EXISTS `metrics_faults`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `metrics_faults` (
  `metrics_faults_id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  `iteration` int(10) unsigned DEFAULT NULL,
  `bucket` varchar(100) DEFAULT NULL,
  `state` varchar(100) DEFAULT NULL,
  `actionname` varchar(100) DEFAULT NULL,
  `dataelement` varchar(100) DEFAULT NULL,
  `mutator` varchar(100) DEFAULT NULL,
  `dataset` varchar(100) DEFAULT NULL,
  `mongoid` varchar(100) DEFAULT NULL,
  `jobs_id` int(10) unsigned DEFAULT NULL,
  `datamodel` varchar(100) DEFAULT NULL,
  `parameter` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`metrics_faults_id`),
  KEY `bucket_idx` (`bucket`),
  KEY `state_idx` (`state`),
  KEY `action_idx` (`actionname`),
  KEY `element_idx` (`dataelement`),
  KEY `mutator_idx` (`mutator`),
  KEY `dataset_idx` (`dataset`),
  KEY `datamodel_idx` (`datamodel`),
  KEY `parameter_idx` (`parameter`),
  KEY `jobs_idx` (`jobs_id`),
  CONSTRAINT `faults_jobs` FOREIGN KEY (`jobs_id`) REFERENCES `jobs` (`jobs_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=21641 DEFAULT CHARSET=utf8 PACK_KEYS=1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `metrics_iterations`
--

DROP TABLE IF EXISTS `metrics_iterations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `metrics_iterations` (
  `metrics_iterations_id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `state` varchar(100) DEFAULT NULL,
  `actionname` varchar(100) DEFAULT NULL,
  `dataelement` varchar(100) DEFAULT NULL,
  `mutator` varchar(100) DEFAULT NULL,
  `dataset` varchar(100) DEFAULT NULL,
  `iterationcount` int(10) unsigned DEFAULT NULL,
  `parameter` varchar(100) DEFAULT NULL,
  `jobs_id` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`metrics_iterations_id`),
  UNIQUE KEY `unique_idx` (`state`,`actionname`,`dataelement`,`mutator`,`dataset`,`parameter`,`jobs_id`),
  KEY `state_idx` (`state`),
  KEY `action_idx` (`actionname`),
  KEY `element_idx` (`dataelement`),
  KEY `mutator_idx` (`mutator`),
  KEY `dataset_idx` (`dataset`),
  KEY `parameter_idx` (`parameter`),
  KEY `jobs_idx` (`jobs_id`),
  CONSTRAINT `iterations_jobs` FOREIGN KEY (`jobs_id`) REFERENCES `jobs` (`jobs_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=65 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `metrics_states`
--

DROP TABLE IF EXISTS `metrics_states`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `metrics_states` (
  `metrics_states_id` int(11) NOT NULL AUTO_INCREMENT,
  `state` varchar(100) NOT NULL,
  `executioncount` int(10) unsigned NOT NULL,
  `jobs_id` int(10) unsigned NOT NULL,
  PRIMARY KEY (`metrics_states_id`),
  KEY `state_idx` (`state`),
  KEY `states_jobs_idx` (`jobs_id`),
  CONSTRAINT `states_jobs` FOREIGN KEY (`jobs_id`) REFERENCES `jobs` (`jobs_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Temporary table structure for view `viewbuckettrend`
--

DROP TABLE IF EXISTS `viewbuckettrend`;
/*!50001 DROP VIEW IF EXISTS `viewbuckettrend`*/;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
/*!50001 CREATE TABLE `viewbuckettrend` (
  `target` tinyint NOT NULL,
  `jobid` tinyint NOT NULL,
  `startdate` tinyint NOT NULL,
  `bucketcount` tinyint NOT NULL
) ENGINE=MyISAM */;
SET character_set_client = @saved_cs_client;

--
-- Temporary table structure for view `viewfaulttrend`
--

DROP TABLE IF EXISTS `viewfaulttrend`;
/*!50001 DROP VIEW IF EXISTS `viewfaulttrend`*/;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
/*!50001 CREATE TABLE `viewfaulttrend` (
  `target` tinyint NOT NULL,
  `jobid` tinyint NOT NULL,
  `startdate` tinyint NOT NULL,
  `faultcount` tinyint NOT NULL
) ENGINE=MyISAM */;
SET character_set_client = @saved_cs_client;

--
-- Temporary table structure for view `viewmetricsbybucket`
--

DROP TABLE IF EXISTS `viewmetricsbybucket`;
/*!50001 DROP VIEW IF EXISTS `viewmetricsbybucket`*/;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
/*!50001 CREATE TABLE `viewmetricsbybucket` (
  `jobid` tinyint NOT NULL,
  `target` tinyint NOT NULL,
  `pitfilename` tinyint NOT NULL,
  `startdate` tinyint NOT NULL,
  `mutator` tinyint NOT NULL,
  `dataelementcount` tinyint NOT NULL,
  `iterationcount` tinyint NOT NULL,
  `bucketcount` tinyint NOT NULL,
  `faultcount` tinyint NOT NULL
) ENGINE=MyISAM */;
SET character_set_client = @saved_cs_client;

--
-- Temporary table structure for view `viewmetricsbymutator`
--

DROP TABLE IF EXISTS `viewmetricsbymutator`;
/*!50001 DROP VIEW IF EXISTS `viewmetricsbymutator`*/;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
/*!50001 CREATE TABLE `viewmetricsbymutator` (
  `jobid` tinyint NOT NULL,
  `startdate` tinyint NOT NULL,
  `mutator` tinyint NOT NULL,
  `dataelementcount` tinyint NOT NULL,
  `iterationcount` tinyint NOT NULL,
  `bucketcount` tinyint NOT NULL,
  `faultcount` tinyint NOT NULL
) ENGINE=MyISAM */;
SET character_set_client = @saved_cs_client;

--
-- Dumping routines for database 'peachfarmreporting'
--
/*!50003 DROP PROCEDURE IF EXISTS `jobs_insert` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8 */ ;
/*!50003 SET character_set_results = utf8 */ ;
/*!50003 SET collation_connection  = utf8_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'STRICT_TRANS_TABLES,NO_AUTO_CREATE_USER,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`matt`@`10.0.1.%` PROCEDURE `jobs_insert`(
in jobid VARCHAR(12),
in target VARCHAR(100),
in startdate DATETIME,
in mongoid VARCHAR(100),
in pitfilename VARCHAR(100),
out rowid INT UNSIGNED
)
BEGIN

SET rowid = NULL;

SELECT j.jobs_id
INTO rowid
FROM jobs as j
WHERE j.jobid = jobid;

IF rowid IS NULL THEN
	INSERT INTO jobs
	(jobid, target, startdate, mongoid, pitfilename)
	VALUES(jobid, target, startdate, mongoid, pitfilename);

	SELECT LAST_INSERT_ID()
	INTO rowid;
END IF;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `metrics_faults_insert` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8 */ ;
/*!50003 SET character_set_results = utf8 */ ;
/*!50003 SET collation_connection  = utf8_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'STRICT_TRANS_TABLES,NO_AUTO_CREATE_USER,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`matt`@`10.0.1.%` PROCEDURE `metrics_faults_insert`(
jobs_id INT(11),
bucket varchar(100),
iteration int unsigned,
state varchar(100),
actionname varchar(100),
dataelement varchar(100),
mutator varchar(100),
dataset varchar(100),
datamodel varchar(100),
parameter varchar(100)
)
BEGIN
	INSERT INTO metrics_faults
	(jobs_id,iteration,bucket,state,actionname,dataelement,mutator,dataset,datamodel,parameter)
	VALUES(jobs_id,iteration,bucket,state,actionname,dataelement,mutator,dataset,datamodel,parameter);
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `metrics_iterations_insert` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8 */ ;
/*!50003 SET character_set_results = utf8 */ ;
/*!50003 SET collation_connection  = utf8_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'STRICT_TRANS_TABLES,NO_AUTO_CREATE_USER,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`matt`@`10.0.1.%` PROCEDURE `metrics_iterations_insert`(
in jobs_id INT(11),
in state varchar(100),
in actionname varchar(100),
in parameter varchar(100),
in dataelement varchar(100),
in mutator varchar(100),
in dataset varchar(100),
in iterationcount int unsigned
)
BEGIN
	DECLARE rowid, oldcount int unsigned;
	SET rowid = NULL;
	SET oldcount = NULL;

	SELECT 
		metrics_iterations_id,iterationcount
	INTO
		rowid,oldcount
	FROM metrics_iterations AS mi
	WHERE
		mi.jobs_id = jobs_id
		AND mi.state = state
		AND mi.actionname = actionname
		AND mi.parameter = parameter
		AND mi.dataelement = dataelement
		AND mi.mutator = mutator
		AND mi.dataset = dataset;

	IF rowid IS NOT NULL THEN
		UPDATE metrics_iterations mi
		SET
			mi.iterationcount = oldcount + iterationcount
		where
			mi.metrics_iterations_id = rowid;
	ELSE
		INSERT INTO metrics_iterations
			  (jobs_id,state,actionname,parameter,dataelement,mutator,dataset,iterationcount)
		VALUES(jobs_id,state,actionname,parameter,dataelement,mutator,dataset,iterationcount);
	END IF;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `selectbuckettrend` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8 */ ;
/*!50003 SET character_set_results = utf8 */ ;
/*!50003 SET collation_connection  = utf8_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'STRICT_TRANS_TABLES,NO_AUTO_CREATE_USER,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`matt`@`10.0.1.%` PROCEDURE `selectbuckettrend`(
in jobid VARCHAR(12)
)
BEGIN
SELECT j.target, j.startdate
INTO @target, @startdate
FROM jobs as j
WHERE j.jobid = jobid;

SELECT
	b.startdate,
	b.bucketcount
FROM viewbuckettrend as b
WHERE 
	b.target = @target
	AND b.startdate <= @startdate
ORDER BY b.startdate;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `selectfaulttrend` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8 */ ;
/*!50003 SET character_set_results = utf8 */ ;
/*!50003 SET collation_connection  = utf8_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'STRICT_TRANS_TABLES,NO_AUTO_CREATE_USER,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`matt`@`10.0.1.%` PROCEDURE `selectfaulttrend`(
in jobid VARCHAR(12)
)
BEGIN
SELECT j.target, j.startdate
INTO @target, @startdate
FROM jobs as j
WHERE j.jobid = jobid;

SELECT
	v.startdate,
	v.faultcount
FROM viewfaulttrend as v
WHERE 
	v.target = @target
	AND v.startdate <= @startdate
ORDER BY v.startdate;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `selectmetricsbymutator` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8 */ ;
/*!50003 SET character_set_results = utf8 */ ;
/*!50003 SET collation_connection  = utf8_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'STRICT_TRANS_TABLES,NO_AUTO_CREATE_USER,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`matt`@`10.0.1.%` PROCEDURE `selectmetricsbymutator`(
in jobid varchar(12)
)
BEGIN

SELECT j.target, j.startdate
INTO @target, @startdate
FROM jobs as j
WHERE j.jobid = jobid;

SELECT j.jobid
INTO @previousjobid
FROM jobs as j
WHERE 
	j.startdate < @startdate
	AND target = @target
ORDER BY j.startdate DESC
LIMIT 1;

select
	v1.jobid,
	v1.startdate,
	v1.mutator,
	v1.dataelementcount,
	v1.iterationcount,
	v1.bucketcount,
	(v1.bucketcount - v2.bucketcount) as buckettrend,
	v1.faultcount,
	(v1.faultcount - v2.faultcount) as faulttrend
from viewmetricsbymutator as v1
join viewmetricsbymutator as v2
on v2.jobid = @previousjobid
and v2.mutator = v1.mutator
where v1.jobid = jobid;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;

--
-- Final view structure for view `viewbuckettrend`
--

/*!50001 DROP TABLE IF EXISTS `viewbuckettrend`*/;
/*!50001 DROP VIEW IF EXISTS `viewbuckettrend`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8 */;
/*!50001 SET character_set_results     = utf8 */;
/*!50001 SET collation_connection      = utf8_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`matt`@`10.0.1.%` SQL SECURITY DEFINER */
/*!50001 VIEW `viewbuckettrend` AS select `j`.`target` AS `target`,`j`.`jobid` AS `jobid`,`j`.`startdate` AS `startdate`,count(distinct `f`.`bucket`) AS `bucketcount` from (`jobs` `j` join `metrics_faults` `f` on((`f`.`jobs_id` = `j`.`jobs_id`))) group by `j`.`target`,`j`.`jobid`,`j`.`startdate` */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `viewfaulttrend`
--

/*!50001 DROP TABLE IF EXISTS `viewfaulttrend`*/;
/*!50001 DROP VIEW IF EXISTS `viewfaulttrend`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8 */;
/*!50001 SET character_set_results     = utf8 */;
/*!50001 SET collation_connection      = utf8_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`matt`@`10.0.1.%` SQL SECURITY DEFINER */
/*!50001 VIEW `viewfaulttrend` AS select `j`.`target` AS `target`,`j`.`jobid` AS `jobid`,`j`.`startdate` AS `startdate`,count(`f`.`metrics_faults_id`) AS `faultcount` from (`jobs` `j` join `metrics_faults` `f` on((`f`.`jobs_id` = `j`.`jobs_id`))) group by `j`.`target`,`j`.`jobid`,`j`.`startdate` */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `viewmetricsbybucket`
--

/*!50001 DROP TABLE IF EXISTS `viewmetricsbybucket`*/;
/*!50001 DROP VIEW IF EXISTS `viewmetricsbybucket`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8 */;
/*!50001 SET character_set_results     = utf8 */;
/*!50001 SET collation_connection      = utf8_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`matt`@`10.0.1.%` SQL SECURITY DEFINER */
/*!50001 VIEW `viewmetricsbybucket` AS select `j`.`jobid` AS `jobid`,`j`.`target` AS `target`,`j`.`pitfilename` AS `pitfilename`,`j`.`startdate` AS `startdate`,`i`.`mutator` AS `mutator`,count(distinct `i`.`dataelement`) AS `dataelementcount`,sum(`i`.`iterationcount`) AS `iterationcount`,count(distinct `f`.`bucket`) AS `bucketcount`,count(`f`.`metrics_faults_id`) AS `faultcount` from ((`jobs` `j` join `metrics_iterations` `i` on((`i`.`jobs_id` = `j`.`jobs_id`))) left join `metrics_faults` `f` on((`f`.`jobs_id` = `i`.`jobs_id`))) group by `j`.`jobid`,`j`.`target`,`j`.`pitfilename`,`j`.`startdate`,`i`.`mutator` */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;

--
-- Final view structure for view `viewmetricsbymutator`
--

/*!50001 DROP TABLE IF EXISTS `viewmetricsbymutator`*/;
/*!50001 DROP VIEW IF EXISTS `viewmetricsbymutator`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8 */;
/*!50001 SET character_set_results     = utf8 */;
/*!50001 SET collation_connection      = utf8_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`matt`@`10.0.1.%` SQL SECURITY DEFINER */
/*!50001 VIEW `viewmetricsbymutator` AS select `j`.`jobid` AS `jobid`,`j`.`startdate` AS `startdate`,`i`.`mutator` AS `mutator`,count(distinct `i`.`dataelement`) AS `dataelementcount`,sum(`i`.`iterationcount`) AS `iterationcount`,count(distinct `f`.`bucket`) AS `bucketcount`,count(`f`.`metrics_faults_id`) AS `faultcount` from ((`jobs` `j` join `metrics_iterations` `i` on((`i`.`jobs_id` = `j`.`jobs_id`))) left join `metrics_faults` `f` on(((`f`.`jobs_id` = `j`.`jobs_id`) and (`f`.`mutator` = `i`.`mutator`)))) group by `j`.`jobid`,`j`.`startdate`,`i`.`mutator` */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2013-11-07 16:45:17
