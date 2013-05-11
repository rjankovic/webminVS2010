-- phpMyAdmin SQL Dump
-- version 3.3.2deb1ubuntu1
-- http://www.phpmyadmin.net
--
-- Hostiteľ: localhost
-- Vygenerované:: 08.Máj, 2013 - 15:06
-- Verzia serveru: 5.1.69
-- Verzia PHP: 5.3.2-1ubuntu4.19

SET SQL_MODE="NO_AUTO_VALUE_ON_ZERO";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Databáza: `webmin`
--

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `access_rights`
--

CREATE TABLE IF NOT EXISTS `access_rights` (
  `id_access_rights` int(11) NOT NULL AUTO_INCREMENT,
  `id_user` int(11) NOT NULL,
  `id_project` int(11) DEFAULT NULL,
  `access` int(11) NOT NULL,
  PRIMARY KEY (`id_access_rights`),
  UNIQUE KEY `id_user_2` (`id_user`,`id_project`),
  KEY `id_user` (`id_user`),
  KEY `id_project` (`id_project`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=40 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `controls`
--

CREATE TABLE IF NOT EXISTS `controls` (
  `id_control` int(11) NOT NULL AUTO_INCREMENT,
  `id_panel` int(11) NOT NULL,
  `content` longtext NOT NULL,
  PRIMARY KEY (`id_control`),
  KEY `id_panel` (`id_panel`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=10958 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `fields`
--

CREATE TABLE IF NOT EXISTS `fields` (
  `id_field` int(11) NOT NULL AUTO_INCREMENT,
  `id_panel` int(11) NOT NULL,
  `content` longtext NOT NULL,
  PRIMARY KEY (`id_field`),
  KEY `id_panel` (`id_panel`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=17972 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `hierarchy_nav_tables`
--

CREATE TABLE IF NOT EXISTS `hierarchy_nav_tables` (
  `id_item` int(11) NOT NULL,
  `id_control` int(11) NOT NULL,
  `id_parent` int(11) DEFAULT NULL,
  `caption` varchar(255) NOT NULL,
  `id_nav` int(11) DEFAULT NULL,
  PRIMARY KEY (`id_item`,`id_control`),
  KEY `id_control` (`id_control`),
  KEY `id_parent` (`id_parent`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `locks`
--

CREATE TABLE IF NOT EXISTS `locks` (
  `id_lock` int(11) NOT NULL AUTO_INCREMENT,
  `id_owner` int(11) NOT NULL,
  `id_project` int(11) NOT NULL,
  `lock_type` int(11) NOT NULL,
  PRIMARY KEY (`id_lock`),
  UNIQUE KEY `id_project_2` (`id_project`,`lock_type`),
  KEY `id_project` (`id_project`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=153 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `log_db`
--

CREATE TABLE IF NOT EXISTS `log_db` (
  `id_log` bigint(20) NOT NULL AUTO_INCREMENT,
  `query` text NOT NULL,
  `total_time` int(11) NOT NULL,
  `count` int(11) NOT NULL,
  `max_time` int(11) NOT NULL,
  `timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_log`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=6831 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `log_db_archive`
--

CREATE TABLE IF NOT EXISTS `log_db_archive` (
  `id_log` bigint(20) NOT NULL AUTO_INCREMENT,
  `query` text NOT NULL,
  `total_time` int(11) NOT NULL,
  `count` int(11) NOT NULL,
  `max_time` int(11) NOT NULL,
  `timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `comment` varchar(255) NOT NULL,
  PRIMARY KEY (`id_log`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `log_users`
--

CREATE TABLE IF NOT EXISTS `log_users` (
  `id_log` bigint(20) NOT NULL AUTO_INCREMENT,
  `id_user` int(11) NOT NULL,
  `id_panel` int(11) NOT NULL,
  `action` varchar(50) NOT NULL,
  `param` tinyblob NOT NULL,
  `timestamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `pid` int(11) NOT NULL,
  PRIMARY KEY (`id_log`),
  KEY `id_user` (`id_user`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 AUTO_INCREMENT=1 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `my_aspnet_applications`
--

CREATE TABLE IF NOT EXISTS `my_aspnet_applications` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(256) DEFAULT NULL,
  `description` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM  DEFAULT CHARSET=utf8 AUTO_INCREMENT=2 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `my_aspnet_membership`
--

CREATE TABLE IF NOT EXISTS `my_aspnet_membership` (
  `userId` int(11) NOT NULL DEFAULT '0',
  `Email` varchar(128) DEFAULT NULL,
  `Comment` varchar(255) DEFAULT NULL,
  `Password` varchar(128) NOT NULL,
  `PasswordKey` char(32) DEFAULT NULL,
  `PasswordFormat` tinyint(4) DEFAULT NULL,
  `PasswordQuestion` varchar(255) DEFAULT NULL,
  `PasswordAnswer` varchar(255) DEFAULT NULL,
  `IsApproved` tinyint(1) DEFAULT NULL,
  `LastActivityDate` datetime DEFAULT NULL,
  `LastLoginDate` datetime DEFAULT NULL,
  `LastPasswordChangedDate` datetime DEFAULT NULL,
  `CreationDate` datetime DEFAULT NULL,
  `IsLockedOut` tinyint(1) DEFAULT NULL,
  `LastLockedOutDate` datetime DEFAULT NULL,
  `FailedPasswordAttemptCount` int(10) unsigned DEFAULT NULL,
  `FailedPasswordAttemptWindowStart` datetime DEFAULT NULL,
  `FailedPasswordAnswerAttemptCount` int(10) unsigned DEFAULT NULL,
  `FailedPasswordAnswerAttemptWindowStart` datetime DEFAULT NULL,
  PRIMARY KEY (`userId`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COMMENT='2';

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `my_aspnet_profiles`
--

CREATE TABLE IF NOT EXISTS `my_aspnet_profiles` (
  `userId` int(11) NOT NULL,
  `valueindex` longtext,
  `stringdata` longtext,
  `binarydata` longblob,
  `lastUpdatedDate` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`userId`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `my_aspnet_roles`
--

CREATE TABLE IF NOT EXISTS `my_aspnet_roles` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `applicationId` int(11) NOT NULL,
  `name` varchar(255) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM  DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC AUTO_INCREMENT=30 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `my_aspnet_schemaversion`
--

CREATE TABLE IF NOT EXISTS `my_aspnet_schemaversion` (
  `version` int(11) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `my_aspnet_sessioncleanup`
--

CREATE TABLE IF NOT EXISTS `my_aspnet_sessioncleanup` (
  `LastRun` datetime NOT NULL,
  `IntervalMinutes` int(11) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `my_aspnet_sessions`
--

CREATE TABLE IF NOT EXISTS `my_aspnet_sessions` (
  `SessionId` varchar(255) NOT NULL,
  `ApplicationId` int(11) NOT NULL,
  `Created` datetime NOT NULL,
  `Expires` datetime NOT NULL,
  `LockDate` datetime NOT NULL,
  `LockId` int(11) NOT NULL,
  `Timeout` int(11) NOT NULL,
  `Locked` tinyint(1) NOT NULL,
  `SessionItems` longblob,
  `Flags` int(11) NOT NULL,
  PRIMARY KEY (`SessionId`,`ApplicationId`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `my_aspnet_users`
--

CREATE TABLE IF NOT EXISTS `my_aspnet_users` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `applicationId` int(11) NOT NULL,
  `name` varchar(256) NOT NULL,
  `isAnonymous` tinyint(1) NOT NULL DEFAULT '1',
  `lastActivityDate` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM  DEFAULT CHARSET=utf8 AUTO_INCREMENT=10 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `my_aspnet_usersinroles`
--

CREATE TABLE IF NOT EXISTS `my_aspnet_usersinroles` (
  `userId` int(11) NOT NULL DEFAULT '0',
  `roleId` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`userId`,`roleId`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `panels`
--

CREATE TABLE IF NOT EXISTS `panels` (
  `id_panel` int(11) NOT NULL AUTO_INCREMENT,
  `id_project` int(11) NOT NULL,
  `id_parent` int(11) DEFAULT NULL,
  `id_holder` int(11) DEFAULT NULL,
  `content` longtext NOT NULL,
  PRIMARY KEY (`id_panel`),
  KEY `id_project` (`id_project`),
  KEY `id_holder` (`id_holder`),
  KEY `id_parent` (`id_parent`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=5082 ;

-- --------------------------------------------------------

--
-- Štruktúra tabuľky pre tabuľku `projects`
--

CREATE TABLE IF NOT EXISTS `projects` (
  `id_project` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `connstring_web` varchar(255) NOT NULL,
  `connstring_information_schema` varchar(255) NOT NULL,
  `server_type` enum('MySQL','MSSQL','Oracle') NOT NULL,
  `version` int(11) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id_project`)
) ENGINE=InnoDB  DEFAULT CHARSET=utf8 AUTO_INCREMENT=18 ;

--
-- Obmedzenie pre exportované tabuľky
--

--
-- Obmedzenie pre tabuľku `access_rights`
--
ALTER TABLE `access_rights`
  ADD CONSTRAINT `access_rights_ibfk_1` FOREIGN KEY (`id_project`) REFERENCES `projects` (`id_project`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `controls`
--
ALTER TABLE `controls`
  ADD CONSTRAINT `controls_ibfk_1` FOREIGN KEY (`id_panel`) REFERENCES `panels` (`id_panel`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `fields`
--
ALTER TABLE `fields`
  ADD CONSTRAINT `fields_ibfk_1` FOREIGN KEY (`id_panel`) REFERENCES `panels` (`id_panel`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `hierarchy_nav_tables`
--
ALTER TABLE `hierarchy_nav_tables`
  ADD CONSTRAINT `hierarchy_nav_tables_ibfk_1` FOREIGN KEY (`id_control`) REFERENCES `controls` (`id_control`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `locks`
--
ALTER TABLE `locks`
  ADD CONSTRAINT `locks_ibfk_1` FOREIGN KEY (`id_project`) REFERENCES `projects` (`id_project`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Obmedzenie pre tabuľku `panels`
--
ALTER TABLE `panels`
  ADD CONSTRAINT `panels_ibfk_1` FOREIGN KEY (`id_project`) REFERENCES `projects` (`id_project`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `panels_ibfk_3` FOREIGN KEY (`id_parent`) REFERENCES `panels` (`id_panel`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `panels_ibfk_4` FOREIGN KEY (`id_holder`) REFERENCES `fields` (`id_field`) ON DELETE SET NULL ON UPDATE CASCADE;
