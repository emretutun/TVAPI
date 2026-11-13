-- =====================================================================================================
--  YALNIZCA KENDİ BİLGİSAYARINDAKİ (localhost:3306) MySQL'DE ÇALIŞTIR.
--  Canlı sunucuya bağlı bir pencerede ÇALIŞTIRMA.
--
--  Ne yapar: yerelde "htsportv_news" adında yeni bir şema ve haber tablolarını oluşturur,
--  API uçlarını deneyebilmen için içine UYDURMA örnek satırlar ekler. Örnek yayın akışı bugünün tarihindedir.
--
--  Tablo tanımları canlıda SHOW CREATE TABLE ile aldığın metinlerdir; yalnızca iki yerde değiştirildi,
--  çünkü yereldeki MySQL 8 bunları kabul etmiyor (sorguları etkilemez):
--    1) '0000-00-00' varsayılan tarihleri kaldırıldı (tbl_dir_structure.creation_time, tbl_records.giris_tarihi).
--    2) tbl_records'taki bölümleme (PARTITION BY ...) kaldırıldı.
-- =====================================================================================================

-- Güvenlik kontrolü: aşağıdaki sorgu bilgisayarının adını göstermeli. Başka bir sunucu adı görürsen DUR.
SELECT @@hostname AS bu_bilgisayar_mi, @@port AS port;

CREATE DATABASE IF NOT EXISTS `htsportv_news` CHARACTER SET utf8 COLLATE utf8_turkish_ci;
USE `htsportv_news`;

CREATE TABLE IF NOT EXISTS `tbl_yayin_saatleri` (
 `id` int(11) NOT NULL AUTO_INCREMENT,
 `yayin_tarihi` date NOT NULL,
 `dir_id` int(11) NOT NULL,
 `saat_id` int(11) NOT NULL,
 `status` tinyint(1) NOT NULL DEFAULT '1',
 `global_single_key` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Aynı anda sadece bir kullanıcı bu anahtarı alabilir',
 PRIMARY KEY (`id`),
 UNIQUE KEY `IX_tarih_saat` (`yayin_tarihi`,`saat_id`) USING BTREE,
 KEY `saat_dilimi` (`dir_id`),
 KEY `saat_id` (`saat_id`),
 KEY `status` (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

CREATE TABLE IF NOT EXISTS `def_yayin_saatleri` (
 `id` int(11) NOT NULL AUTO_INCREMENT,
 `saat` varchar(10) COLLATE utf8_turkish_ci NOT NULL,
 `is_main_saat` tinyint(1) NOT NULL DEFAULT '1',
 PRIMARY KEY (`id`),
 UNIQUE KEY `IX_saat` (`saat`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

CREATE TABLE IF NOT EXISTS `tbl_dir_structure` (
 `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
 `pid` bigint(20) unsigned NOT NULL DEFAULT '0',
 `name` varchar(128) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `details` varchar(255) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `created_by` int(10) unsigned NOT NULL DEFAULT '0',
 `creation_time` datetime NOT NULL,
 `modification_time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Record modification time',
 `sira` int(11) NOT NULL DEFAULT '0',
 `status` tinyint(3) unsigned NOT NULL DEFAULT '1',
 PRIMARY KEY (`id`),
 KEY `pid` (`pid`),
 KEY `status` (`status`),
 KEY `sira` (`sira`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

CREATE TABLE IF NOT EXISTS `tbl_records` (
 `auto_id` int(10) unsigned NOT NULL AUTO_INCREMENT,
 `parent_id` int(10) NOT NULL DEFAULT '0',
 `haber_id` varchar(32) COLLATE utf8_turkish_ci NOT NULL,
 `akis_haber_id` varchar(32) COLLATE utf8_turkish_ci DEFAULT NULL,
 `remote_record_id` bigint(15) NOT NULL DEFAULT '0',
 `user_id` int(10) unsigned DEFAULT NULL,
 `dir_id` int(10) unsigned NOT NULL DEFAULT '0',
 `baslik` varchar(255) COLLATE utf8_turkish_ci NOT NULL,
 `haber` text COLLATE utf8_turkish_ci,
 `kam_spk` text COLLATE utf8_turkish_ci,
 `giris_tarihi` date NOT NULL,
 `giris_saati` time DEFAULT NULL,
 `kelime` int(11) NOT NULL,
 `sure` int(11) NOT NULL,
 `sure_alt` varchar(12) COLLATE utf8_turkish_ci NOT NULL,
 `text1` varchar(80) COLLATE utf8_turkish_ci NOT NULL,
 `text2` varchar(80) COLLATE utf8_turkish_ci NOT NULL,
 `goruntu` tinyint(4) DEFAULT NULL,
 `beta` tinyint(4) DEFAULT NULL,
 `ses_tipi` tinyint(4) NOT NULL DEFAULT '0',
 `status` tinyint(4) NOT NULL DEFAULT '1',
 `kayit_zamani` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
 `yayin_tarihi` date NOT NULL,
 `sira` int(11) DEFAULT NULL,
 `saat` int(11) DEFAULT NULL,
 `yalanci_haber` tinyint(1) NOT NULL DEFAULT '0',
 `haber_bitti` tinyint(1) unsigned NOT NULL DEFAULT '0' COMMENT 'Haberin bittiğini gösterir',
 PRIMARY KEY (`auto_id`,`giris_tarihi`),
 UNIQUE KEY `haber_id` (`haber_id`,`giris_tarihi`),
 KEY `user_id` (`user_id`),
 KEY `giris_tarihi` (`giris_tarihi`),
 KEY `dir_id` (`dir_id`),
 KEY `dt_index` (`giris_tarihi`,`giris_saati`),
 KEY `tarih` (`yayin_tarihi`),
 KEY `remote_record_id` (`remote_record_id`),
 KEY `akis_haber_id` (`akis_haber_id`),
 KEY `baslik` (`baslik`),
 KEY `parent_id` (`parent_id`),
 KEY `tbl_records_multi_yilbasi_idx` (`status`,`yayin_tarihi`,`dir_id`,`saat`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

CREATE TABLE IF NOT EXISTS `tbl_news_saved_scene` (
 `saved_scene_id` bigint(20) NOT NULL AUTO_INCREMENT,
 `scene_gui_id` int(11) NOT NULL,
 `description` varchar(255) COLLATE utf8_turkish_ci NOT NULL,
 `scene_parameters` text COLLATE utf8_turkish_ci,
 `scene_metadata` text COLLATE utf8_turkish_ci,
 `scene_haber_id` int(10) unsigned NOT NULL DEFAULT '0',
 `sira_no` int(11) NOT NULL DEFAULT '0',
 `modification_time` datetime NOT NULL,
 `status` tinyint(3) unsigned NOT NULL DEFAULT '1',
 PRIMARY KEY (`saved_scene_id`),
 KEY `scene_gui_id` (`scene_gui_id`),
 KEY `status` (`status`),
 KEY `scene_haber_id` (`scene_haber_id`,`sira_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

CREATE TABLE IF NOT EXISTS `viz_saved_scenes` (
 `saved_scene_id` bigint(20) NOT NULL AUTO_INCREMENT,
 `scene_gui_id` int(11) NOT NULL,
 `description` varchar(255) COLLATE utf8_turkish_ci NOT NULL,
 `scene_haber_id` int(10) unsigned NOT NULL DEFAULT '0',
 `sira_no` int(11) NOT NULL DEFAULT '0',
 `modification_time` datetime NOT NULL,
 `status` tinyint(3) unsigned NOT NULL DEFAULT '1',
 PRIMARY KEY (`saved_scene_id`),
 KEY `scene_gui_id` (`scene_gui_id`),
 KEY `status` (`status`),
 KEY `scene_haber_id` (`scene_haber_id`,`sira_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

-- -----------------------------------------------------------------------------------------------------
--  UYDURMA ÖRNEK VERİ (gerçek kayıt değildir; yalnızca uçları denemek için).
--  Sahnelerdeki scene_gui_id değerleri (1, 2, 3) htsportv şemasındaki örnek şablonlardır.
-- -----------------------------------------------------------------------------------------------------
INSERT INTO `def_yayin_saatleri` (`id`, `saat`, `is_main_saat`) VALUES
    (1, '07:00', 1),
    (2, '19:00', 1);

INSERT INTO `tbl_dir_structure` (`id`, `pid`, `name`, `details`, `created_by`, `creation_time`, `sira`, `status`) VALUES
    (10, 0,  'ORNEK Haber Bültenleri', '', 0, '2026-01-01 09:00:00', 1, 1),
    (20, 10, 'ORNEK Ana Haber',        '', 0, '2026-01-01 09:00:00', 1, 1),
    (21, 10, 'ORNEK Sabah Bülteni',    '', 0, '2026-01-01 09:00:00', 2, 1);

-- Bugün iki akış (07:00 ve 19:00), yarın bir akış.
INSERT INTO `tbl_yayin_saatleri` (`id`, `yayin_tarihi`, `dir_id`, `saat_id`, `status`) VALUES
    (1, CURDATE(),                           20, 2, 1),
    (2, CURDATE(),                           21, 1, 1),
    (3, DATE_ADD(CURDATE(), INTERVAL 1 DAY), 20, 2, 1);

-- Bugünün 19:00 akışı (dir_id 20, saat 2). Listeye girmesi gerekenler: 1002, 1001, 1003 (sira sırasıyla).
INSERT INTO `tbl_records`
    (`auto_id`, `haber_id`, `akis_haber_id`, `dir_id`, `baslik`, `giris_tarihi`, `kelime`, `sure`, `sure_alt`, `text1`, `text2`,
     `status`, `yayin_tarihi`, `sira`, `saat`, `yalanci_haber`)
VALUES
    (1001, 'ornek-1001', 'akis-1001', 20, 'ORNEK Transfer gündemi',          CURDATE(), 0, 0, '', '', '', 1, CURDATE(), 2, 2, 0),
    (1002, 'ornek-1002', 'akis-1002', 20, 'ORNEK Derbi öncesi son durum',    CURDATE(), 0, 0, '', '', '', 1, CURDATE(), 1, 2, 0),
    (1003, 'ornek-1003', 'akis-1003', 20, 'ORNEK Milli takım kadrosu',       CURDATE(), 0, 0, '', '', '', 1, CURDATE(), 3, 2, 0),
    (1004, 'ornek-1004', 'akis-1004', 20, 'ORNEK Yer tutucu (yalancı haber)',CURDATE(), 0, 0, '', '', '', 1, CURDATE(), 4, 2, 1),
    (1005, 'ornek-1005', NULL,        20, 'ORNEK Akışa bağlı olmayan haber', CURDATE(), 0, 0, '', '', '', 1, CURDATE(), 5, 2, 0),
    (1006, 'ornek-1006', 'akis-1006', 20, 'ORNEK Silinmiş haber',            CURDATE(), 0, 0, '', '', '', 0, CURDATE(), 6, 2, 0),
    (1007, 'ornek-1007', 'akis-1007', 21, 'ORNEK Sabah bülteni haberi',      CURDATE(), 0, 0, '', '', '', 1, CURDATE(), 1, 1, 0);

-- Haber 1001: iki reji kaydı (+ bir silinmiş). Haber 1002: reji kaydı yok.
INSERT INTO `tbl_news_saved_scene`
    (`saved_scene_id`, `scene_gui_id`, `description`, `scene_parameters`, `scene_metadata`, `scene_haber_id`, `sira_no`, `modification_time`, `status`)
VALUES
    (1, 1, 'Muhabir adı',  'ornek: txt_baslik=ALİ VELİ',                 NULL, 1001, 2, '2026-11-13 17:00:00', 1),
    (2, 2, 'Konuk adı',    'ornek: txt_ust=AYŞE YILMAZ; txt_alt=YORUMCU', NULL, 1001, 1, '2026-11-13 17:05:00', 1),
    (3, 1, 'Silinmiş',     'ornek: txt_baslik=ESKİ',                     NULL, 1001, 3, '2026-11-13 16:00:00', 0);

-- Haber 1002: yalnızca Viz kaydı var. Haber 1001'in Viz kaydı da var ama reji kaydı olduğu için kullanılmamalı.
INSERT INTO `viz_saved_scenes`
    (`saved_scene_id`, `scene_gui_id`, `description`, `scene_haber_id`, `sira_no`, `modification_time`, `status`)
VALUES
    (1, 3, 'Skor bandı',            1002, 1, '2026-11-13 18:00:00', 1),
    (2, 3, 'Kullanılmaması gereken', 1001, 1, '2026-11-13 18:00:00', 1);

-- Kontrol: 3 akış, 7 haber, 3 reji kaydı, 2 Viz kaydı görmelisin.
SELECT (SELECT COUNT(*) FROM tbl_yayin_saatleri) AS akis, (SELECT COUNT(*) FROM tbl_records) AS haber,
       (SELECT COUNT(*) FROM tbl_news_saved_scene) AS reji_kaydi, (SELECT COUNT(*) FROM viz_saved_scenes) AS viz_kaydi;
