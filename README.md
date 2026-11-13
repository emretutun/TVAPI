# TVAPI

Reji programlarının (NTR HT, NTR HTSPOR, NTR Show, TickerControl, MCR) veritabanına doğrudan bağlanmak yerine
veriyi tek bir yerden almasını sağlayan ASP.NET Core Web API.

- **Yalnızca okur.** API'de veri değiştiren hiçbir uç ve hiçbir sorgu yoktur.
- **Viz Engine'e dokunmaz.** Grafik kumandası reji programlarında kalır; API yalnızca veri verir.
- **Şu an yalnızca yerelde çalışır.** Geliştirme, bu bilgisayardaki MySQL (`localhost:3306`) ile yapıldı.
  Canlı veritabanına hiçbir aşamada bağlanılmadı.

## Durum

| Veri | Kaynak şema | Uç | Durum |
|---|---|---|---|
| Spor (Broadage) | `webnews_broadage` | 13 | Gerçek verinin yerel kopyasıyla doğrulandı; NTR HTSPOR bu uçlara bağlandı |
| Sahne şablonları | `htsportv` | 2 | Yerelde örnek veriyle doğrulandı |
| Playlist ve kayıtlı sahneler | `htsportv` | 3 | Yerelde örnek veriyle doğrulandı |
| Hava durumu | `htsportv` | 2 | Yerelde örnek veriyle doğrulandı |
| Haber | `htsportv_news` | 3 | Yerelde örnek veriyle doğrulandı |

"Örnek veriyle doğrulandı" şu demek: tablo tanımları canlıdan `SHOW CREATE TABLE` ile alındı, içine uydurma satırlar
kondu ve uçlar onlarla denendi. Bu uçlar gerçek veriyle henüz karşılaşmadı.

Yapılmayanlar: kayan yazı ve son dakika, ayar ve log tabloları, bütün yazma işlemleri (sahne kaydetme, playlist
düzenleme), haber arşivi, canlıya kurulum, kimlik doğrulama.

## Klasörler

```
NTRAPI/
├── README.md
├── TVAPI/                    çözüm (TVAPI.slnx)
│   ├── TVAPI/                Presentation: controller'lar, hata yönetimi, Program.cs
│   ├── TVAPI.Application/    kullanım senaryoları (servisler), cevap modelleri (DTO), arayüzler
│   ├── TVAPI.Domain/         iş modelleri ve kuralları; hiçbir şeye bağımlı değildir
│   ├── TVAPI.Infrastructure/ MySQL sorguları, JSON çözücüler, önbellek
│   └── TVAPI.Tests/          birim testleri (veritabanı gerektirmez)
├── istemci-ornek/            reji programına kopyalanacak örnek C# dosyaları
└── yerel-kurulum/            yerel MySQL'e tablo kurmak için SQL dosyaları
```

## Mimari

Temiz Mimari, dört katman. Bağımlılıklar hep içe doğrudur:

```
TVAPI (Presentation) ──► Application ──► Domain
        │                    ▲
        └──► Infrastructure ─┘
```

- **Domain:** `Match`, `Squad`, `SceneTemplate` gibi modeller ve onlara ait kurallar (örnek: kadro sıralaması,
  kadronun son maçlardan çıkarılması).
- **Application:** servisler (`MatchService`, `PlaylistService`...), dışarıya dönen DTO'lar ve veriye erişim
  arayüzleri (`IMatchRepository`...). Veritabanını tanımaz.
- **Infrastructure:** arayüzlerin MySQL ile çalışan gerçek halleri. Dapper ve MySqlConnector kullanır.
- **Presentation:** controller'lar ince kalır: isteği alır, servisi çağırır, HTTP cevabına çevirir.

Katmanların birbirine bağlandığı tek yer `Program.cs`'tir (`AddApplication()`, `AddInfrastructure()`).

### Her veri alanında aynı düzen

Yeni bir veri eklenirken izlenen kalıp:

1. `Domain/<Alan>/` içine model.
2. `Application/<Alan>/` içine repository arayüzü, servis ve DTO.
3. `Infrastructure/Persistence/` içine SQL'i yazan repository.
4. `Infrastructure/Caching/` içine onu saran önbellekli repository (decorator).
5. `Controllers/<Alan>/` içine controller.
6. `Tests/<Alan>/` içine servis testleri.

### Veritabanları

Üç ayrı bağlantı vardır; her biri kendi sınıfıyla temsil edilir ve hepsi `MySqlDatabase` tabanından türer.
Bu taban dışarıya yalnızca sorgu sonucu döndüren iki metot açar; veri değiştiren bir metodu bilerek yoktur.

| Sınıf | Ayar | İçerik | Zorunlu mu |
|---|---|---|---|
| `BroadageDatabase` | `ConnectionStrings:Broadage` | Spor verisi | Evet; yoksa API açılmaz |
| `ChannelDatabase` | `ConnectionStrings:Channel` | Kanalın kendi veritabanı: sahne, playlist, hava durumu | Hayır |
| `NewsDatabase` | `ConnectionStrings:News` | Kanalın haber veritabanı | Hayır |

Zorunlu olmayan bir bağlantı tanımlı değilse API yine açılır; yalnızca o veriyi kullanan uçlar 503 döner.

Her kanalın veritabanı ayrı olduğu için tasarım şudur: tek kod, kanal başına ayrı kurulum. Her kurulum kendi
ayar dosyasıyla tek bir kanalın veritabanlarına bağlanır.

### Canlıdaki stored procedure'ler

Eski programlar bazı verileri prosedürlerle okuyor (`sp_GetPlaylistSavedScenes`, `sp_GetRundownsAtDate`,
`sp_GetRundownStories`, `sp_GetNewsSavedScenes`). API bunları çağırmaz; içlerindeki `SELECT` sorgularının aynısı
repository'lere yazıldı. Böylece API'nin veritabanında prosedür çalıştırma yetkisine ihtiyacı olmaz, yalnızca
okuma yetkisi yeter.

## Çalıştırma

Gerekenler: .NET 10 SDK, yerelde MySQL 8.

1. Yerel MySQL'de şemalar kurulu olmalı (aşağıda "Yerel veritabanı").
2. Bağlantı bilgileri user-secrets deposunda durur; kod deposuna ya da `appsettings.json`'a yazılmaz.
   Visual Studio'da TVAPI projesine sağ tık → **Manage User Secrets**:

   ```json
   {
     "ConnectionStrings": {
       "Broadage": "Server=localhost;Port=3306;Database=webnews_broadage;User Id=...;Password=...;",
       "Channel": "Server=localhost;Port=3306;Database=htsportv;User Id=...;Password=...;",
       "News": "Server=localhost;Port=3306;Database=htsportv_news;User Id=...;Password=...;"
     }
   }
   ```

3. Visual Studio'dan başlat ya da çözüm klasöründe:

   ```bash
   dotnet run --project TVAPI --launch-profile http
   ```

Adresler: `http://localhost:5149` ve (https profilinde) `https://localhost:7058`.
https profiliyle başlatıldığında http adresine gelen istekler https'e yönlendirilir (307); istemcide doğru adresi
kullanmak her çağrının iki istek olmasını önler.

Örnek isteklerin tamamı `TVAPI/TVAPI/TVAPI.http` dosyasındadır.

## Uçlar

Bütün uçlar `GET`'tir.

### Spor — `Controllers/Broadage`

| Uç | Ne döner |
|---|---|
| `/api/v1/soccer/tournaments` | Turnuva listesi |
| `/api/v1/soccer/tournaments/{id}/current` | Aktif sezon ve hafta |
| `/api/v1/soccer/tournaments/{id}/standings?round=` | Puan durumu (genel, iç saha, deplasman) |
| `/api/v1/soccer/tournaments/{id}/matches?round=` | Haftanın maçları |
| `/api/v1/soccer/tournaments/{id}/teams` | Sezonun takımları |
| `/api/v1/soccer/teams/{id}` | Takım künyesi |
| `/api/v1/soccer/teams/{id}/players` | Takım kadrosu |
| `/api/v1/soccer/matches?tournamentId=&from=&to=` | Tarihe göre maçlar (`from` dahil, `to` hariç; en çok 31 gün, 20 turnuva) |
| `/api/v1/soccer/matches/{id}` | Tek maç: skor, dakika, durum |
| `/api/v1/soccer/matches/{id}/lineups` | Maç kadroları |
| `/api/v1/soccer/matches/{id}/events` | Maç olayları |
| `/api/v1/soccer/matches/{id}/statistics` | Maç istatistikleri |

`round` verilmezse aktif hafta kullanılır.

### Sahneler — `Controllers/Scenes`

| Uç | Ne döner |
|---|---|
| `/api/v1/scenes/templates?directoryId=&name=` | Klasöre ve/veya ada göre şablonlar (en az bir süzgeç zorunlu) |
| `/api/v1/scenes/templates/{id}` | Tek şablon |
| `/api/v1/scenes/playlists?categoryId=` | Kategoriye göre playlist'ler |
| `/api/v1/scenes/playlists/{id}` | Playlist'in kayıtlı sahneleri ve kullandıkları şablonlar |
| `/api/v1/scenes/saved/{id}` | Tek kayıtlı sahne, şablonuyla |

Her şablon alanları (`elements`) ve animasyonlarıyla (`directors`) birlikte gelir.

### Hava durumu — `Controllers/Weather`

| Uç | Ne döner |
|---|---|
| `/api/v1/weather/cities?id=` | Yerler, isteğin sırasıyla |
| `/api/v1/weather/forecasts?cityId=&from=&days=` | Yerlerin günlük tahminleri (`from` yoksa bugün, `days` yoksa 3) |

### Haber — `Controllers/News`

| Uç | Ne döner |
|---|---|
| `/api/v1/news/rundowns?date=` | Günün yayın akışları (`date` yoksa bugün) |
| `/api/v1/news/stories?date=&directoryId=&timeSlotId=` | Bir akıştaki haberler |
| `/api/v1/news/stories/{id}/scenes` | Habere bağlı kayıtlı sahneler ve şablonları |

### Sistem

| Uç | Ne döner |
|---|---|
| `/health` | API'nin ayakta olduğu bilgisi. Veritabanına bakmaz. |

## Cevap kuralları

- **Bulunamayan kayıt:** 404.
- **Kayıt var ama veri henüz oluşmamış:** 200 ve içi boş cevap. Örnek: maç var ama kadro açıklanmamışsa
  `home` ve `away` `null`; playlist var ama boşsa `scenes` boş liste.
- **Geçersiz parametre:** 400; hangi alanın hatalı olduğu cevapta yazar.
- **Veritabanına ulaşılamıyor:** 503. İstemci elindeki son veriyi kullanmalıdır.
- **Kaynaktaki veri bozuk (çözülemeyen JSON):** 500.

Hata cevaplarının hepsi aynı biçimdedir (ProblemDetails): `title`, `status`, `detail`, `traceId`.

### Tazelik başlıkları

Veri okuyan her cevapta iki başlık bulunur:

```
X-Data-Fetched-At: 2026-11-13T08:59:52.3044680+00:00
X-Data-Stale: false
```

- `X-Data-Fetched-At`: cevapta kullanılan en eski verinin kaynaktan okunduğu an (UTC).
- `X-Data-Stale`: `true` ise veritabanına ulaşılamamış ve bellekteki son başarılı veri dönmüştür.

## Önbellek

Her repository bir önbellek katmanıyla sarılıdır (`ResilientCache`):

1. Veri tazeyse veritabanına gidilmez.
2. Süresi dolduysa yeniden okunur.
3. Veritabanı cevap vermezse ya da bozuk veri verirse, bellekteki son başarılı veri `X-Data-Stale: true` ile döner.
4. Son başarılı veri 12 saat tutulur; veritabanı bu süre boyunca hiç dönmezse veri silinir ve istemci 503 alır.

Süreler `appsettings.json` içindedir:

| Veri | Süre |
|---|---|
| Maç listesi, tek maç, olaylar, istatistikler | 5 sn |
| Aktif hafta, puan durumu, maç kadrosu | 30 sn |
| Takımlar, kadro, turnuva listesi | 5 dk |
| Sahne şablonları | 60 sn |
| Playlist'ler | 30 sn |
| Kayıtlı sahneler (playlist ve haber) | 5 sn |
| Hava durumu | 5 dk |
| Yayın akışları | 30 sn |
| Akıştaki haberler | 10 sn |

Önbellek bellektedir; API yeniden başlatıldığında boşalır.

## Testler

114 birim testi; hiçbiri veritabanı gerektirmez ve tamamı bir saniyenin altında çalışır.

```bash
dotnet test
```

Sınananlar: önbelleğin çökme davranışı, JSON çözücüler (bozuk veri dahil), servis kuralları, kadro kuralları.
Sınanmayanlar: SQL sorguları (yalnızca yerel veritabanıyla elle denendi) ve HTTP katmanı.

## Yerel veritabanı

`webnews_broadage` şeması gerçek verinin yerel bir kopyasıdır. Diğer şemalar `yerel-kurulum/` klasöründeki
dosyalarla kurulur. Tablo tanımları canlıdakiyle aynıdır; içindeki satırlar uydurmadır.

| Dosya | Oluşturduğu |
|---|---|
| `htsportv-sahne-sablonlari.sql` | `htsportv` şeması; `tbl_gui`, `tbl_gui_elements`, `tbl_gui_director` |
| `htsportv-playlist.sql` | `tbl_playlist_group`, `tbl_saved_scenes` |
| `htsportv-hava-durumu.sql` | `tbl_hava_merkez`, `tbl_hava_tahmin`, `def_hava_durum` |
| `htsportv_news-haber.sql` | `htsportv_news` şeması; yayın akışı, haber ve haber sahnesi tabloları |

Dosyalar bu sırayla ve **yalnızca yerel MySQL'de** (MySQL Workbench, `localhost:3306`) çalıştırılır. Her dosyanın
ilk sorgusu bağlı olunan bilgisayarın adını gösterir; beklenmeyen bir ad çıkarsa devam edilmez.

### Yeni bir tablo gerektiğinde

1. Kaynak veritabanında, yalnızca okuma yetkili bir hesapla tablonun tanımı alınır: `SHOW CREATE TABLE tablo_adi;`
2. Çıkan tanım yerelde çalıştırılır ve içine örnek satır eklenir.
3. Uç yerel veritabanına karşı yazılır ve denenir.

## Reji programlarını bağlama

`istemci-ornek/` klasöründe .NET Framework 4.7.2 ile uyumlu iki örnek dosya vardır:

- `CSporApiTypes.cs`: spor uçlarının cevap sınıfları.
- `CSporApiMap.cs`: bu cevapları NTR HTSPOR'un mevcut sınıflarına (`TeamsType`, `TTeam`, `MatchEvent`...) çeviren metotlar.

Geçişte izlenen yöntem: eski metodun adı `_DB` ekiyle korunur, yanına API'den okuyan yenisi yazılır ve bir anahtar
(`ApiKullan`) hangisinin çalışacağını seçer. Böylece istenildiği an eski yola dönülebilir.

Sahne, hava durumu ve haber uçları için henüz örnek istemci dosyası yazılmadı.

## Güvenlik kuralları

Bu proje boyunca uyulan ve uyulmaya devam edilmesi gereken kurallar:

- API yalnızca okuma yetkili bir veritabanı hesabıyla çalışacak biçimde tasarlandı; yazma yetkisine ihtiyacı yoktur.
- Geliştirme yalnızca yerel veritabanıyla yapılır; canlı veritabanına geliştirme ortamından bağlanılmaz.
- Hiçbir bağlantı bilgisi bu depoya yazılmaz.
- Yayındaki reji programlarında yapılacak değişiklikleri programın sahibi yapar.
- Bağlantı bilgileri yalnızca user-secrets deposunda (ileride: sunucunun gizli ayar deposunda) durur.

## Bilinen sınırlar

- **Tabloyu baştan sona tarayan sorgular.** `soccer_matches`'te `match_id`, `soccer_teams`'te `team_id` tek başına
  index'li değildir; tek maç, tek takım ve "son maç kadroları" sorguları tabloyu tarar. Önbellek bunları seyrekleştirir.
  Canlıda tablolar büyükse index eklenmesi gerekir.
- **Haber arşivi yok.** Güncel tablolarda bulunmayan eski tarihler için boş liste döner.
- **Haftası olmayan turnuvalar** (hazırlık maçları gibi) kaynakta tek bir "hafta" altında tutulur; bunlar için
  haftalık maç ucu yerine tarihe göre maç ucu kullanılmalıdır.
- **Kimlik doğrulama yok.** API'ye ağdan ulaşabilen herkes çağırabilir.
- **`/health` veritabanını kontrol etmez.**
- **Önbellek bellektedir** ve aynı anda süresi dolan isteklerin hepsi veritabanına gider.
- **Bazı 400 ve 404 mesajları İngilizcedir** (framework'ün varsayılan metinleri).
- **Eski koddan bilinçli farklar:** API adları büyük harfe çevirmez (istemcinin işi); playlist listesinde silinmiş
  kayıtları hiçbir kategoride döndürmez; şablon listesinde alanların hepsini türüyle birlikte verir.

## Sırada ne var

1. Kayan yazı ve son dakika uçları (`tbl_kayan_yazi`, `viz_scroller_groups`, `viz_scroller_group_items`,
   `tbl_son_dakika_state`).
2. Sahne, hava durumu ve haber için örnek istemci dosyaları ve reji programlarının bu uçlara bağlanması.
3. Yazma uçları: sahne kaydetme, playlist düzenleme, habere sahne bağlama, log.
4. Canlıya kurulum: API'ye özel yalnızca okuma yetkili servis hesabı, kimlik doğrulama, veritabanını da
   kontrol eden sağlık ucu.
