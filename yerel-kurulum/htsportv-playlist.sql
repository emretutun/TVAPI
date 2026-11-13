-- =====================================================================================================
--  YALNIZCA KENDİ BİLGİSAYARINDAKİ (localhost:3306) MySQL'DE ÇALIŞTIR.
--  Canlı sunucuya bağlı bir pencerede ÇALIŞTIRMA.
--
--  Önce htsportv-sahne-sablonlari.sql çalıştırılmış olmalı (bu tablolar tbl_gui'ye bağlıdır).
--
--  Ne yapar: yereldeki "htsportv" şemasına playlist ve kayıtlı sahne tablolarını ekler,
--  API uçlarını deneyebilmen için içine birkaç UYDURMA örnek satır koyar.
--  Tablo tanımları, canlıda SHOW CREATE TABLE ile aldığın metinlerin aynısıdır.
-- =====================================================================================================

-- Güvenlik kontrolü: aşağıdaki sorgu bilgisayarının adını göstermeli. Başka bir sunucu adı görürsen DUR.
SELECT @@hostname AS bu_bilgisayar_mi, @@port AS port;

USE `htsportv`;

CREATE TABLE IF NOT EXISTS `tbl_playlist_group` (
 `group_id` int(11) NOT NULL AUTO_INCREMENT,
 `name` varchar(255) COLLATE utf8_turkish_ci NOT NULL,
 `user_id` int(11) NOT NULL DEFAULT '0',
 `plc_id` int(11) NOT NULL DEFAULT '1' COMMENT 'Playlist category ID',
 `fixed_gui_dir_id` int(11) NOT NULL DEFAULT '0' COMMENT 'Gui dir ID to restrict the saved scenes',
 `fixed_gui_id` int(11) NOT NULL DEFAULT '0' COMMENT 'Gui ID to restrict saved scenes',
 `create_date` datetime NOT NULL,
 `status` tinyint(1) unsigned NOT NULL DEFAULT '1',
 PRIMARY KEY (`group_id`),
 KEY `playlist_plc_id` (`plc_id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

CREATE TABLE IF NOT EXISTS `tbl_saved_scenes` (
 `saved_scene_id` bigint(20) NOT NULL AUTO_INCREMENT,
 `scene_gui_id` int(11) NOT NULL,
 `description` varchar(255) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `group_id` int(11) NOT NULL,
 `sira_no` int(11) NOT NULL DEFAULT '0',
 `scene_parameters` text COLLATE utf8_turkish_ci,
 `scene_metadata` text COLLATE utf8_turkish_ci COMMENT 'Additional field to use for custom metadata',
 `modification_time` datetime NOT NULL,
 `status` tinyint(1) unsigned NOT NULL DEFAULT '1',
 `is_migrated` tinyint(1) unsigned NOT NULL DEFAULT '1',
 `create_date` datetime DEFAULT CURRENT_TIMESTAMP,
 PRIMARY KEY (`saved_scene_id`),
 KEY `scene_gui_id` (`scene_gui_id`),
 KEY `group_id` (`group_id`,`sira_no`),
 CONSTRAINT `FK_tbl_saved_scenes_group` FOREIGN KEY (`group_id`) REFERENCES `tbl_playlist_group` (`group_id`) ON DELETE CASCADE,
 CONSTRAINT `FK_tbl_saved_scenes_tbl_gui` FOREIGN KEY (`scene_gui_id`) REFERENCES `tbl_gui` (`gui_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

-- -----------------------------------------------------------------------------------------------------
--  UYDURMA ÖRNEK VERİ (gerçek kayıt değildir; yalnızca uçları denemek için).
--  scene_parameters sütununun gerçek biçimini bilmiyorum; buradaki metin yalnızca yer tutucudur.
-- -----------------------------------------------------------------------------------------------------
INSERT INTO `tbl_playlist_group`
    (`group_id`, `name`, `user_id`, `plc_id`, `fixed_gui_dir_id`, `fixed_gui_id`, `create_date`, `status`)
VALUES
    (1, 'ORNEK İSİMLER',     0, 3, 157, 0, '2026-01-15 09:00:00', 1),
    (2, 'ORNEK SPOR',        0, 2,   0, 3, '2026-02-01 09:00:00', 1),
    (3, 'ORNEK BOŞ LİSTE',   0, 3,   0, 0, '2026-03-01 09:00:00', 1),
    (4, 'ORNEK SİLİNMİŞ',    0, 3,   0, 0, '2025-01-01 09:00:00', 0);

INSERT INTO `tbl_saved_scenes`
    (`saved_scene_id`, `scene_gui_id`, `description`, `group_id`, `sira_no`, `scene_parameters`, `scene_metadata`,
     `modification_time`, `status`, `is_migrated`, `create_date`)
VALUES
    (1, 1, 'Ali Veli',              1, 2, 'ornek: txt_baslik=ALİ VELİ',                      NULL, '2026-11-10 10:00:00', 1, 1, '2026-11-10 10:00:00'),
    (2, 2, 'Ayşe Yılmaz - Yorumcu', 1, 1, 'ornek: txt_ust=AYŞE YILMAZ; txt_alt=YORUMCU',     NULL, '2026-11-10 10:05:00', 1, 1, '2026-11-10 10:05:00'),
    (3, 1, 'Mehmet Demir',          1, 3, 'ornek: txt_baslik=MEHMET DEMİR',                  NULL, '2026-11-11 09:00:00', 1, 1, '2026-11-11 09:00:00'),
    (4, 1, 'Silinmiş kayıt',        1, 4, 'ornek: txt_baslik=ESKİ',                          NULL, '2026-11-01 09:00:00', 0, 1, '2026-11-01 09:00:00'),
    (5, 4, 'Şablonu silinmiş',      1, 5, 'ornek: txt_baslik=ŞABLONSUZ',                     NULL, '2026-11-01 09:00:00', 1, 1, '2026-11-01 09:00:00'),
    (6, 3, 'GS - FB',               2, 1, 'ornek: txt_ev=GALATASARAY; txt_deplasman=FENERBAHÇE', 'ornek: mac=1', '2026-11-12 20:00:00', 1, 1, '2026-11-12 19:00:00');

-- Kontrol: 4 playlist ve 6 kayıtlı sahne görmelisin.
SELECT (SELECT COUNT(*) FROM tbl_playlist_group) AS playlist, (SELECT COUNT(*) FROM tbl_saved_scenes) AS kayitli_sahne;
