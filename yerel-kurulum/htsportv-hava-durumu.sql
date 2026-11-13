-- =====================================================================================================
--  YALNIZCA KENDİ BİLGİSAYARINDAKİ (localhost:3306) MySQL'DE ÇALIŞTIR.
--  Canlı sunucuya bağlı bir pencerede ÇALIŞTIRMA.
--
--  Ne yapar: yereldeki "htsportv" şemasına hava durumu tablolarını ekler ve içine örnek satır koyar.
--  Tablo tanımları, canlıda SHOW CREATE TABLE ile aldığın metinlerin aynısıdır.
--  Hava durumu tanımları (def_hava_durum) senin yapıştırdığın gerçek listeden alındı;
--  yerler ve tahminler UYDURMADIR. Tahminler bugünden başlayıp 5 günü kapsar.
-- =====================================================================================================

-- Güvenlik kontrolü: aşağıdaki sorgu bilgisayarının adını göstermeli. Başka bir sunucu adı görürsen DUR.
SELECT @@hostname AS bu_bilgisayar_mi, @@port AS port;

USE `htsportv`;

CREATE TABLE IF NOT EXISTS `tbl_hava_merkez` (
 `merkez_id` int(11) NOT NULL AUTO_INCREMENT,
 `merkez_tanim_en` varchar(50) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `merkez_tanim` varchar(50) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `merkez_p` int(11) NOT NULL,
 `merkez_ilkodu` int(11) NOT NULL,
 `acc_city_code` varchar(20) COLLATE utf8_turkish_ci NOT NULL DEFAULT '' COMMENT 'Accuweather city code',
 PRIMARY KEY (`merkez_id`),
 UNIQUE KEY `IX_tbl_hava_merkez_tanim_p` (`merkez_p`,`merkez_tanim_en`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

CREATE TABLE IF NOT EXISTS `def_hava_durum` (
 `hd_id` int(11) NOT NULL AUTO_INCREMENT,
 `hd_kisa` varchar(10) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `hd_adi` varchar(50) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `hd_video` varchar(60) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 PRIMARY KEY (`hd_id`),
 UNIQUE KEY `IX_def_hava_durum_kisa` (`hd_kisa`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

CREATE TABLE IF NOT EXISTS `tbl_hava_tahmin` (
 `tahmin_id` int(11) NOT NULL AUTO_INCREMENT,
 `merkez_id` int(11) NOT NULL,
 `tarih` date NOT NULL,
 `en_yuksek_derece` int(11) NOT NULL,
 `en_dusuk_derece` int(11) NOT NULL,
 `hava_durum` varchar(10) COLLATE utf8_turkish_ci NOT NULL,
 `ruzgar_yon` varchar(10) COLLATE utf8_turkish_ci NOT NULL DEFAULT '',
 `ruzgar_hiz` smallint(6) DEFAULT NULL,
 `nem_orani` smallint(6) DEFAULT NULL,
 `hd_id` int(11) NOT NULL,
 PRIMARY KEY (`tahmin_id`),
 UNIQUE KEY `IX_tbl_hava_tahmin_tarih_merkez` (`merkez_id`,`tarih`),
 KEY `IX_tbl_hava_tahmin_hd_id` (`hd_id`),
 KEY `IX_tbl_hava_tahmin_merkez_id` (`merkez_id`),
 CONSTRAINT `tbl_hava_tahmin_ibfk_1` FOREIGN KEY (`merkez_id`) REFERENCES `tbl_hava_merkez` (`merkez_id`) ON DELETE CASCADE,
 CONSTRAINT `tbl_hava_tahmin_ibfk_2` FOREIGN KEY (`hd_id`) REFERENCES `def_hava_durum` (`hd_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_turkish_ci;

-- Hava durumu tanımları: canlıdaki listenin bir bölümü (hd_id, hd_kisa, hd_adi, hd_video).
INSERT INTO `def_hava_durum` (`hd_id`, `hd_kisa`, `hd_adi`, `hd_video`) VALUES
    (1,  '1',  'Açık',                          '1'),
    (2,  '2',  'Az Bulutlu Güneşli',            '1'),
    (3,  '3',  'Parçalı Bulutlu',               '2'),
    (6,  '6',  'Bulutlu',                       '3'),
    (11, '11', 'Sisli',                         '16'),
    (12, '12', 'Yağmurlu',                      '5'),
    (15, '15', 'Gök Gürültülü Sağanak Yağışlı', '5'),
    (19, '19', 'Karlı',                         '10'),
    (32, '32', 'Rüzgarlı',                      '3');

-- UYDURMA yerler.
INSERT INTO `tbl_hava_merkez` (`merkez_id`, `merkez_tanim_en`, `merkez_tanim`, `merkez_p`, `merkez_ilkodu`, `acc_city_code`) VALUES
    (1, 'Istanbul', 'İstanbul', 0, 34, ''),
    (2, 'Ankara',   'Ankara',   0, 6,  ''),
    (3, 'Izmir',    'İzmir',    0, 35, '');

-- UYDURMA tahminler: bugünden başlayarak. İzmir için bilerek yalnızca iki gün var.
INSERT INTO `tbl_hava_tahmin`
    (`merkez_id`, `tarih`, `en_yuksek_derece`, `en_dusuk_derece`, `hava_durum`, `ruzgar_yon`, `ruzgar_hiz`, `nem_orani`, `hd_id`)
VALUES
    (1, CURDATE(),                             14,  8, '6',  '',   NULL, NULL, 6),
    (1, DATE_ADD(CURDATE(), INTERVAL 1 DAY),   15,  9, '12', 'KD', 18,   72,   12),
    (1, DATE_ADD(CURDATE(), INTERVAL 2 DAY),   12,  7, '3',  '',   NULL, NULL, 3),
    (1, DATE_ADD(CURDATE(), INTERVAL 3 DAY),   11,  6, '1',  '',   NULL, NULL, 1),
    (1, DATE_ADD(CURDATE(), INTERVAL 4 DAY),   13,  7, '2',  '',   NULL, NULL, 2),
    (2, CURDATE(),                              9,  1, '11', '',   NULL, NULL, 11),
    (2, DATE_ADD(CURDATE(), INTERVAL 1 DAY),    7, -2, '19', '',   NULL, NULL, 19),
    (2, DATE_ADD(CURDATE(), INTERVAL 2 DAY),    8,  0, '6',  '',   NULL, NULL, 6),
    (3, CURDATE(),                             19, 12, '1',  '',   NULL, NULL, 1),
    (3, DATE_ADD(CURDATE(), INTERVAL 1 DAY),   20, 13, '2',  '',   NULL, NULL, 2);

-- Kontrol: 3 yer, 9 tanım, 10 tahmin görmelisin.
SELECT (SELECT COUNT(*) FROM tbl_hava_merkez) AS yer, (SELECT COUNT(*) FROM def_hava_durum) AS tanim, (SELECT COUNT(*) FROM tbl_hava_tahmin) AS tahmin;
