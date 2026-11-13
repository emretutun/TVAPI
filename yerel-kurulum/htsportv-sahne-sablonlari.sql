-- =====================================================================================================
--  YALNIZCA KENDİ BİLGİSAYARINDAKİ (localhost:3306) MySQL'DE ÇALIŞTIR.
--  Canlı sunucuya bağlı bir pencerede ÇALIŞTIRMA.
--
--  Ne yapar: yerelde "htsportv" adında boş bir şema ve sahne şablonu tablolarını oluşturur,
--  API uçlarını deneyebilmen için içine birkaç UYDURMA örnek satır ekler.
--  Tablo tanımları, canlıda SHOW CREATE TABLE ile aldığın metinlerin aynısıdır.
-- =====================================================================================================

-- Güvenlik kontrolü: aşağıdaki sorgu bilgisayarının adını göstermeli. Başka bir sunucu adı görürsen DUR.
SELECT @@hostname AS bu_bilgisayar_mi, @@port AS port;

CREATE DATABASE IF NOT EXISTS `htsportv` CHARACTER SET utf8 COLLATE utf8_turkish_ci;
USE `htsportv`;

CREATE TABLE IF NOT EXISTS `tbl_gui` (
 `gui_id` int(11) NOT NULL AUTO_INCREMENT,
 `gui_name` varchar(100) COLLATE utf8_turkish_ci NOT NULL,
 `gui_description` text COLLATE utf8_turkish_ci NOT NULL,
 `dir_id` int(11) NOT NULL DEFAULT '0',
 `in_director_name` varchar(100) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `out_director_name` varchar(100) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `intra_director_name` varchar(100) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `scene_full_path` varchar(512) COLLATE utf8_turkish_ci NOT NULL,
 `scene_reset_enabled` tinyint(1) unsigned NOT NULL DEFAULT '0',
 `conflicting_gui_id` varchar(2048) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `target_layer` smallint(6) NOT NULL DEFAULT '1',
 `creation_time` datetime NOT NULL,
 `modification_time` datetime NOT NULL,
 `status` tinyint(1) unsigned NOT NULL DEFAULT '1',
 `intra_director_delay` decimal(10,3) NOT NULL DEFAULT '0.000',
 PRIMARY KEY (`gui_id`),
 KEY `IX_tbl_gui_dir_id` (`dir_id`),
 KEY `IX_tbl_gui_scene_full_path` (`scene_full_path`(255))
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

CREATE TABLE IF NOT EXISTS `tbl_gui_elements` (
 `gui_element_id` int(11) NOT NULL AUTO_INCREMENT,
 `gui_id` int(11) NOT NULL,
 `element_name` varchar(255) COLLATE utf8_turkish_ci NOT NULL,
 `element_type` smallint(6) NOT NULL,
 `element_gui_title` varchar(50) COLLATE utf8_turkish_ci NOT NULL,
 `element_default_value` text COLLATE utf8_turkish_ci NOT NULL,
 `max_size` smallint(6) NOT NULL DEFAULT '0',
 `creation_time` datetime NOT NULL,
 `modification_time` datetime NOT NULL,
 `sira_no` int(11) NOT NULL DEFAULT '0',
 `use_default` tinyint(1) unsigned NOT NULL DEFAULT '0' COMMENT 'The element must always use the default element value (not editable).',
 `status` tinyint(1) unsigned NOT NULL DEFAULT '1',
 PRIMARY KEY (`gui_element_id`),
 KEY `gui_id` (`gui_id`),
 CONSTRAINT `FK_tbl_gui_elements_tbl_gui` FOREIGN KEY (`gui_id`) REFERENCES `tbl_gui` (`gui_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

CREATE TABLE IF NOT EXISTS `tbl_gui_director` (
 `director_id` int(11) NOT NULL AUTO_INCREMENT,
 `gui_id` int(11) NOT NULL,
 `director_name` varchar(100) COLLATE utf8_turkish_ci NOT NULL,
 `director_title` varchar(100) COLLATE utf8_turkish_ci NOT NULL,
 `director_length` decimal(10,3) DEFAULT NULL,
 `creation_time` datetime NOT NULL,
 `modification_time` datetime NOT NULL,
 `status` tinyint(1) unsigned NOT NULL DEFAULT '1',
 PRIMARY KEY (`director_id`),
 KEY `gui_id` (`gui_id`),
 CONSTRAINT `FK_tbl_gui_director_tbl_gui` FOREIGN KEY (`gui_id`) REFERENCES `tbl_gui` (`gui_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

-- -----------------------------------------------------------------------------------------------------
--  UYDURMA ÖRNEK VERİ (gerçek kayıt değildir; yalnızca uçları denemek için).
--  İstersen bu bölümü çalıştırma ve tabloları kendi verinle doldur.
-- -----------------------------------------------------------------------------------------------------
INSERT INTO `tbl_gui`
    (`gui_id`, `gui_name`, `gui_description`, `dir_id`, `in_director_name`, `out_director_name`, `intra_director_name`,
     `scene_full_path`, `scene_reset_enabled`, `conflicting_gui_id`, `target_layer`, `creation_time`, `modification_time`,
     `status`, `intra_director_delay`)
VALUES
    (1, 'ORNEK_KELEBEK_TEK',  'Tek satır kelebek (örnek)',  157, 'IN', 'OUT', '',      'ORNEK/KJ/KELEBEK_TEK',  1, '2',  1, '2026-01-10 10:00:00', '2026-09-01 12:00:00', 1, 0.000),
    (2, 'ORNEK_KELEBEK_CIFT', 'Çift satır kelebek (örnek)', 157, 'IN', 'OUT', 'DEGIS', 'ORNEK/KJ/KELEBEK_CIFT', 0, '1',  1, '2026-01-10 10:05:00', '2026-09-01 12:00:00', 1, 0.500),
    (3, 'ORNEK_SKOR_BANDI',   'Skor bandı (örnek)',         175, 'IN', 'OUT', '',      'ORNEK/SPOR/SKOR_BANDI', 0, '',   2, '2026-02-01 09:00:00', '2026-02-01 09:00:00', 1, 0.000),
    (4, 'ORNEK_SILINMIS',     'Silinmiş şablon (örnek)',    157, 'IN', 'OUT', '',      'ORNEK/KJ/ESKI',         0, '',   1, '2025-01-01 09:00:00', '2025-06-01 09:00:00', 0, 0.000);

INSERT INTO `tbl_gui_elements`
    (`gui_element_id`, `gui_id`, `element_name`, `element_type`, `element_gui_title`, `element_default_value`, `max_size`,
     `creation_time`, `modification_time`, `sira_no`, `use_default`, `status`)
VALUES
    (1, 1, 'txt_baslik',   1, 'Başlık',     '',         40, '2026-01-10 10:00:00', '2026-01-10 10:00:00', 1, 0, 1),
    (2, 1, 'img_logo',     2, 'Logo',       'logo.png',  0, '2026-01-10 10:00:00', '2026-01-10 10:00:00', 2, 1, 1),
    (3, 1, 'txt_eski',     1, 'Eski alan',  '',         20, '2026-01-10 10:00:00', '2026-03-01 10:00:00', 3, 0, 0),
    (4, 2, 'txt_ust',      1, 'Üst satır',  '',         40, '2026-01-10 10:05:00', '2026-01-10 10:05:00', 1, 0, 1),
    (5, 2, 'txt_alt',      1, 'Alt satır',  '',         60, '2026-01-10 10:05:00', '2026-01-10 10:05:00', 2, 0, 1),
    (6, 3, 'txt_ev',       1, 'Ev sahibi',  '',         20, '2026-02-01 09:00:00', '2026-02-01 09:00:00', 1, 0, 1),
    (7, 3, 'txt_deplasman',1, 'Deplasman',  '',         20, '2026-02-01 09:00:00', '2026-02-01 09:00:00', 2, 0, 1);

INSERT INTO `tbl_gui_director`
    (`director_id`, `gui_id`, `director_name`, `director_title`, `director_length`, `creation_time`, `modification_time`, `status`)
VALUES
    (1, 1, 'IN',    'Giriş',    1.250, '2026-01-10 10:00:00', '2026-01-10 10:00:00', 1),
    (2, 1, 'OUT',   'Çıkış',    0.800, '2026-01-10 10:00:00', '2026-01-10 10:00:00', 1),
    (3, 2, 'IN',    'Giriş',    1.250, '2026-01-10 10:05:00', '2026-01-10 10:05:00', 1),
    (4, 2, 'DEGIS', 'Değişim',  NULL,  '2026-01-10 10:05:00', '2026-01-10 10:05:00', 1);

-- Kontrol: 4 şablon, 7 alan, 4 animasyon görmelisin.
SELECT (SELECT COUNT(*) FROM tbl_gui) AS sablon, (SELECT COUNT(*) FROM tbl_gui_elements) AS alan, (SELECT COUNT(*) FROM tbl_gui_director) AS animasyon;
