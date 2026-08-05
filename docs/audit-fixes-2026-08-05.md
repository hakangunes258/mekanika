# Mekanika — AI Test Raporu Düzeltmeleri

**Tarih:** 05.08.2026
**Kaynak:** `mekanika_test_raporu_v1.pdf` (03.08.2026, üç modül) ve
`mekanika_test_raporu.md` (04.08.2026, tüm modüller + UX + kaynak kodu eki)
**Başlangıç durumu:** `source` dalı, commit `c471ba6` (27.07.2026)

İki bağımsız yapay zeka test raporunda tespit edilen 20 bulgunun tamamı ele alındı.
Her düzeltme, raporun kendi test senaryosu tarayıcıda tekrar çalıştırılarak
doğrulandı; el hesabıyla doğrulanmış sayılar referans alındı.

---

## 1. Yönetici Özeti

| # | Bulgu | Önem | Durum |
|---|---|---|---|
| SB-1 | Cıvata yorulma analizi bağlanmamış — sahte FAIL | Kritik | ✅ |
| UX-1 | Derin/paylaşım linkleri alıcıda açılmıyor | Yüksek | ✅ |
| PK-1 | Kama göbek basıncı ~%9 iyimser (emniyetsiz taraf) | Yükseltildi | ✅ |
| IF-1 | 3D görüntüleyici varsayılan ölçüleri gösteriyor | Yükseltildi | ✅ |
| UX-3 | Mobilde navigasyona erişilemiyor + yatay taşma | Yükseltildi | ✅ |
| CS-1 | "Back to Input" sonrası bayat sarım sayısı | Orta | ✅ |
| CS-2 | Blok gerilmesi kontrolü gösterilmiyor | Orta | ✅ |
| ES-1 | Kanca kesme gerilmesi emniyete dahil değil | Orta | ✅ |
| TS-1 | Burulma yayı rijitliğine bacak katkısı yok (~%6) | Orta | ✅ |
| UX-2 | Boş formda Calculate sessiz (10 modül) | Orta | ✅ |
| RB-1 | Makaralı rulmanda elle giriş sessizce çalışmıyor | Orta | ✅ |
| SB-2 | Özet tabloda n = 0,00 | Orta | ✅ |
| SB-3 | F_KR = 0,00 gösterimi | Orta | ✅ (teşhis düzeltildi) |
| SB-4 | Yerleşme değerleri senaryolar arası tutarsız | Orta | ✅ |
| BB-1 | "Modified life" sadeleştirmesi belirtilmemiş | Orta | ✅ |
| BB-2 | Elle girişte limit devri kontrolü yok | Orta | ✅ |
| SB-5 | SF_clamp = ∞ gösterimi | Düşük | ✅ (daha derin hata bulundu) |
| TS-2 | Doğrulanamayan doğal frekans çıktısı | Düşük | ✅ |
| CC-1 | Render edilmeyen `M@recommendedClamp.BoltSize` | Düşük | ✅ |
| GS-1 | Clamp Connection'da hatalı DIN 703 atfı | Düşük | ✅ |
| PK-2 | Standart atfı eksik (DIN 6892) | Bilgi | ✅ |

**Kapsam dışı bırakılan:** GS-4 (Methodology/Verification sayfası) — ayrı bir içerik
işi. UX-1'in SEO kısmı — aşağıda "Açık kalan" bölümüne bakınız.

**Değişiklik hacmi:** 28 dosya, ~1215 ekleme / ~335 silme, 2 yeni bileşen.

---

## 2. Tekrar Eden Hata Sınıfı

Bulguların çoğu tek bir şekle sahipti: **motor doğru hesaplıyor, arayüz sonucu
tüketmiyor.** Formüllerde sistematik hata bulunmadı.

| Bulgu | Kopukluk |
|---|---|
| SB-1 | `input.LoadType` hiç set edilmiyor → yorulma dalı hiç çalışmıyor |
| SB-2 | Yanlış değişken okunuyor (`input.` yerine `result.`) |
| CS-2 | `SafetyFactorSolid` hesaplanıyor, hiçbir yerde okunmuyor |
| ES-1 | `HookShearStress` üretiliyor, hiçbir karşılaştırmaya girmiyor |
| IF-1 | JSON anahtarı serileştirmede düşüyor → sessizce varsayılana dönüyor |
| UX-3 | `.sidebar.open` tanımlı, sınıfı ekleyen kod yok |
| UX-2 | Doğrulama başarısız, kullanıcıya mesaj yok |

Bu, gelecekteki triyajda ilk bakılacak yer olarak not edildi.

---

## 3. Modül Bazında Düzeltmeler

### 3.1 Tek Cıvata (VDI 2230) — SB-1 … SB-5

**SB-1 (Kritik).** `Pages/SingleBolt.razor` `input.LoadType`'ı hiçbir yerde set
etmiyordu. Varsayılan `LoadType.Static` kaldığı için `BoltCalculationEngine.cs`
içindeki `if (_input.LoadType != LoadType.Static) CalculateFatigue();` koşulu hiç
sağlanmıyor, σa/σm/σASV/SF_fatigue varsayılan 0'da kalıyor ve sonuç **FAIL** olarak
görünüyordu.

- Yük tipi artık yük aralığından türetiliyor (`DetermineLoadType`): salınım yoksa
  Static, `min < 0 < max` ise Alternating, aksi halde Pulsating.
- Raporun önerdiği kural (`min == 0 → Pulsating`, aksi halde Alternating) kendi test
  senaryosunu (1.000 → 5.000 N, ikisi de pozitif) yanlış sınıflandırdığı için
  kullanılmadı.
- Motordaki **iki paralel yorulma yolu tek yola indirildi**. `CalculateFatigue`
  (σ'lar, SF) ve `CalculateFatigueAnalysis` (ömür, hasar, çevrim) farklı kod
  yollarında farklı Static testleriyle çalışıyordu; kartın iki yarısı birbiriyle
  çelişebiliyordu. Artık tek metot koşulsuz çalışıyor ve salınım yoksa "uygulanamaz"
  diyor.

| Çıktı | Önce | Sonra | Beklenen |
|---|---|---|---|
| σa | 0,0 MPa | **2,0 MPa** | ≈2 |
| σASV | 0,0 MPa | **50,0 MPa** | ≈51 |
| N_design | 1 | **1,0 × 10⁶** | 10⁶ |
| SF_fatigue | 0,00 → FAIL | **24,60 → ✓ OK** | ≈25 |

**SB-2.** Bolt Assembly tablosu `input.LoadIntroductionFactor` (otomatik moddayken
kullanılmayan manuel override, 0.0) okuyordu; artık `result.LoadIntroductionFactor`
okuyor. n = 0,00 → **0,37**, hₙ = 0,00 → **11,14 mm**.

**SB-3 — teşhis düzeltildi.** Rapor "satır bağlanmamış" diyordu. Gerçekte
`FKR_min = max(FKR_slip, FKR_separation)` ve kesme yükü olmayan, konsantrik, salt
eksenel bir bağlantıda bu değer VDI 2230'a göre **gerçekten 0**'dır. Testçinin
beklediği 10,28 kN aslında F_M,min'dir. Eksik olan sayı değil, girdiydi:

- Yeni girdi: *Required Residual Clamp Force (F_Kerf)* — sızdırmazlık veya arayüzün
  açılmaması için serviste kalması gereken kenetleme kuvveti.
- Satır "Required Clamp Force in Service" olarak yeniden etiketlendi; 0 olduğunda
  nedeni açıklanıyor.

**SB-4.** Yerleşme (fZ) senaryolar arasında `sqrt(F/FM_max)` ile ölçekleniyordu.
VDI 2230 Tablo 5.4/1 f_Z'yi yalnızca yüzey pürüzlülüğü ve yük tipinden verir; ön
gerilmeye bağlı değildir. Ölçekleme kaldırıldı: üç senaryo da **23,5 μm / 5,57 kN**
(önce 29,5 / 18,6 / 23,5 μm).

**SB-5 — raporun görmediği daha derin bir hata.** `FKR_min = 0` iken `SF_clamp` bir
kayan nokta bıçak sırtındaydı: `FM_min := FKR_min + FZ + FPA` olarak boyutlandığı
için `FK_min = FM_min − FPA − FZ` tam olarak 0'a düşer. Eski
`FK_min > 0 ? 999 : 0` ifadesi **aynı bağlantı için bazen ∞, bazen 0,00 FAIL +
"Joint opens under load!" hatası** üretiyordu — SB-1 ile aynı sınıftan ikinci bir
sahte FAIL. Testçi canlıda ∞ tarafını, doğrulama harness'ı 0 tarafını gördü.

- Kenetleme kuvveti şartı yoksa kontrol "uygulanamaz" (n/a) sayılıyor.
- "Joint opens" hatası yalnızca gerçekten bir şart varken tetikleniyor.
- Her hesapta tetiklenen `SF_clamp < 1.2` uyarısı kaldırıldı (yapısal olarak hep
  1,00 olduğu için saf gürültüydü).

Gerçek yetersizlikler etkilenmedi: kesme yüklü kontrol senaryosunda SF_yield = 0,53
→ hata doğru üretiliyor.

### 3.2 Kamalı Bağlantı (DIN 6885 / DIN 6892) — PK-1, PK-2

**PK-1.** Göbek tarafı taşıma yüksekliği `t₂` kullanıyordu. Kama göbeğe ancak
milden dışarı çıkan kadarıyla bastırabilir: **h − t₁**. Göbek kanalı, kama üst
yüzeyine değil yanaklarına otursun diye bilerek daha derin açılır, yani
`t₂ > h − t₁` tasarım gereğidir.

Rapor bunu "Düşük" işaretlemişti; **emniyetsiz taraftaki** bir hata ve modülün
manşet çıktısında olduğu için önceliği yükseltildi.

| | Önce | Sonra |
|---|---|---|
| Taşıma yüksekliği (göbek) | 3,30 mm (t₂) | **3,00 mm (h − t₁)** |
| Temas alanı A_hub | 191,4 mm² | **174,0 mm²** |
| Yüzey basıncı p_hub | 104,5 MPa | **114,9 MPa** |
| Emniyet SF_hub | 3,70 | **3,37** |

Eski değer %9,0 düşüktü. Mil tarafı (69,0 MPa / SF 5,61) ve kesme (28,7 MPa /
SF 8,98) değişmedi.

- Kenar durum: `min(h − t₁, t₂)` — biri t₂'yi çıkıntıdan sığ girerse kama göbek
  kanalının dibine oturur ve taşıyan yükseklik t₂ olur.
- Sonuç tablosuna taşıma yüksekliği satırları eklendi; etiket hangisinin belirleyici
  olduğunu yazıyor.

**PK-2.** DIN 6885 boyut standardıdır; mukavemet hesabı DIN 6892'yi izler. Modül
açıklaması, anahtar kelimeleri ve `VerificationStandards` güncellendi.

### 3.3 Bası Yayı (EN 13906-1) — CS-1, CS-2

**CS-2.** Motor `SafetyFactorSolid`'i hesaplıyordu ama repo genelinde başka hiçbir
yerde okunmuyordu. Blokta akacak bir yay yalnızca çalışma noktası gerilmesine
bakılarak "✓ OK" görünüyordu.

Raporun "gösterilen emniyet = min(çalışma, blok)" önerisi **uygulanmadı**: kod
yorumu statik emniyetin bilerek çalışma noktasına çekildiğini, eskiden blok
gerilmesini kullandığını ve karıştırıldığını anlatıyor. İkisi farklı sorular, ayrı
satırlar olarak gösterildi.

Raporun Senaryo D'si (d=2, Dm=30, L₀=150, n=10, L₁=120):

| | Önce | Sonra |
|---|---|---|
| SF_static | 4,04 ✓ OK | 4,04 ✓ OK *(değişmedi)* |
| SF_c (bloğa basılırsa) | *gösterilmiyordu* | **0,96 ❌ FAIL** |
| Design Guidelines | *satır yoktu* | **τc/τzul = 795/765 MPa ❌ FAIL** |

**CS-1.** `TotalCoils` yalnızca `<= 0` iken türetiliyordu; sayfa tek bir engine
örneği kullandığı için ilk hesaptan sonra yapışıp kalıyordu. Koşulsuz türetmeye
çevrildi (formda manuel nt alanı yok, güvenli).

| n değişimi | Önce | Sonra |
|---|---|---|
| 8,5 → 10,0 | nt = 10,5'te takılı | **nt = 12,0**, Lc = 24,0, Ld = 1131 mm, m = 27,9 g |
| 10,0 → 8,5 | — | **nt = 10,5**, Lc = 21,0, Ld = 990 mm, m = 24,4 g |

### 3.4 Çekme Yayı (EN 13906-2) — ES-1

τH hesaplanıyor ve gösteriliyordu ama hiçbir karşılaştırmaya girmiyordu;
`SafetyFactorHook` yalnızca eğilmeden geliyordu. EN 13906-2 kancada iki kontrol
ister. Artık `SafetyFactorHook = min(eğilme, kesme)`, iki bileşen ayrı ayrı da
gösteriliyor ve hangisinin belirlediği yazıyor.

Raporun senaryosu (d=2, Dm=15, n=20, Alman kancası, s₁=5, s₂=15):

| | Önce | Sonra |
|---|---|---|
| SF_hook,eğilme | — | 2,59 ✓ OK |
| SF_hook,kesme | *hesaplanmıyordu* | **1,53** |
| SF_hook | 2,59 | **1,53 (belirleyici: Shear)** |
| Critical Location | **Body (1,66)** | **Hook (1,53)** |

Aşırı yük senaryosunda (s₂ = 60 mm) fark daha belirgin: eğilme 1,10 ⚠️ Marginal,
kesme **0,65 ❌ FAIL**. Raporun gördüğü 1,10 tam olarak eğilme satırıydı.

### 3.5 Burulma Yayı (EN 13906-3) — TS-1, TS-2

**TS-1.** Rijitlik yalnızca `ActiveCoils` ile hesaplanıyordu. EN 13906-3 bacakların
katkısını eşdeğer sarım sayısıyla katar: `ne = n + (L₁+L₂)/(3π·Dm)`. Motor bacakları
moment kolu ve tel boyu için kullanıyordu ama rijitlikte kullanmıyordu.

| | Önce | Sonra |
|---|---|---|
| n_e | *yoktu* | **5,28** |
| R | 686,67 Nmm/rad | **649,89 Nmm/rad** |
| σ1 / σ2 | 508 / 1016 MPa | **481 / 962 MPa** |
| SF1 / SF2 | 2,34 / 1,17 | **2,47 / 1,24** |

Gerilme için eski değer konservatifti; ancak "bu tork için kaç derece gerekir"
sorusunda emniyetsiz taraftaydı.

**TS-2.** Doğal frekans çıktısı kaldırıldı. EN 13906-3 burulma yayları için bir
dalgalanma frekansı standardize etmiyor; kullanılan formül bası yayı formülünün elle
uyarlanmış hâliydi ve sıradan bir yay için ~4,5 kHz veriyordu. Her satırı
doğrulanabilir bir tabloda doğrulanamayan bir sayı, yokluğundan daha maliyetli.
Yerine eşdeğer sarım sayısı satırı kondu.

### 3.6 Rulmanlar (ISO 281 / ISO 76) — BB-1, BB-2, RB-1

**RB-1.** Kök neden raporun tespitinden daha derindi: C ve C₀ düzenlenebilir olduğu
için elle giriş davet ediliyordu, ama **konik rulmanda e ve Y için hiç alan yoktu**
— yalnızca katalog seçiminden geliyorlardı. Elle giriş yapısal olarak imkânsızdı;
mesaj eklemek tek başına yetmezdi.

- Konik seçilince görünen **e, Y ve Y₀** alanları eklendi.
- Statik eşdeğer yük `selectedTaperedBearing?.Y0` okuyordu; elle girişte sessizce 0
  oluyordu. Artık alandan okunuyor.

Doğrulama (raporun Senaryo 2'si, tamamen elle): P = 6000 N, L10 = 2521,53 ×10⁶ dev,
P₀ = 6,00 kN, S₀ = 11,53 — raporun el hesabıyla birebir.

**BB-1.** `L_nah` sembolü a₁ **ve** a_ISO'yu ima eder; gerçekte yalnızca a₁
uygulanıyor. Sembol `L_na` olarak düzeltildi, etiket "(a₁ only)" oldu ve ISO
281:2007'nin tam a_ISO hesabının (κ, e_C, C_u) dahil olmadığını açıklayan bir metot
notu eklendi. Her iki rulman modülüne de uygulandı.

**BB-2.** Elle girişte `n_lim = 0` kalıyor ve hız kontrolü `> 0` şartına bağlı
olduğu için hiç tetiklenmiyordu. Artık limit devri "not known" yazıyor ve kontrolün
**yapılmadığını** açıkça söyleyen bir bilgi kutusu çıkıyor.

### 3.7 3D Görüntüleyici — IF-1

Rapor bunu "Düşük" işaretlemiş ve tek modülde görmüştü. Kök neden dört
görüntüleyiciyi birden etkiliyordu.

Blazor'ın `IJSRuntime`'ından çıkan yakalandı:

```
C# gönderiyor:  { d, L, di, Da, pressure }
JS'e ulaşan:    { "d":50, "l":60, "di":0, "da":90, "pressure":59.8 }
```

Değerler doğru; anahtar adları `JsonSerializerDefaults.Web` tarafından camelCase'e
çevriliyor. Builder `p.L` / `p.Da` okuduğu için `undefined` alıp `num()`'un sabit
varsayılanlarına düşüyordu (`L→40`, `Da→d×1.6=80`).

Etkilenen anahtarlar: interference-fit (`L`, `Da`), taper-fit (`L`, `Da`),
key-connection (`Dhub`), **compression-spring** (`Dm`, `L0`, `Lc`, `L1`, `L2` —
neredeyse tüm geometri).

Çözüm: `viewer3d.js` içinde parametreler artık büyük/küçük harf duyarsız okunuyor
(`caseInsensitiveParams`). Tek yerde, dört modülü birden düzeltiyor ve kuralı
hatırlama yükünü kaldırıyor. Yalnızca harf durumuyla ayrışan iki anahtar gelirse
konsola uyarı düşüyor.

| Modül | Önce | Sonra |
|---|---|---|
| interference-fit | Ø50,0 × **40,0** · hub Ø**80,0** | Ø50,0 × **60,0** · hub Ø**90,0** |
| compression-spring | varsayılan yay (d=3, Dm=25, L₀=80) | d=4,00 · Dm=30,0 · L₀=60,0 · n=8,50/10,5 |
| taper-fit | boy varsayılan 40 | Ø47,0 → Ø53,0, **60,0 mm** boyunca |
| key-connection | `Dhub` düşüyordu | Ø40,0 · kama 12,0×8,00×70,0 |

Bu, projede kayıtlı serileştirme tuzağının **üçüncü** tekrarıydı (jsonb round-trip
ve custom material'lardan sonra); CLAUDE.md'ye kural olarak eklendi.

### 3.8 Sıkmalı Bağlantı — CC-1

`Pages/ClampConnection.razor:41`'deki `M@recommendedClamp.BoltSize` parantezlendi.
Razor harften sonra gelen `@`'i e-posta adresi kuralına takıp düz metin basıyordu.

---

## 4. Site Geneli Düzeltmeler

### 4.1 UX-1 — GitHub Pages derin link el sıkışması (Yüksek)

`wwwroot/404.html` yönlendirmeyi tek başına `<meta http-equiv="refresh">`'e
bırakıyordu. Tarayıcılar bu ipucunu geciktirmekte veya yok saymakta serbesttir;
takıldığında tarayıcı `/` için **hiç istek göndermiyor**. Sonuç: her Share Link ve
her Google girişi sessizce kırılıyordu.

- Yönlendirme `location.replace('/')` ile script tabanlı yapıldı; meta refresh
  yalnızca `<noscript>` yedeği.
- `sessionStorage` erişimleri `try/catch`'e alındı. Bu `index.html` tarafında
  kritikti: depolama engelli bir tarayıcıda o exception Blazor açılmadan önce
  çalışıp tüm siteyi o ziyaretçi için öldürüyordu.
- `location.pathname === '/'` iken yönlendirme yapılmıyor (sonsuz döngü koruması).
- Gövdeye tıklanabilir yedek link eklendi.
- Deploy iş akışına el sıkışmanın her iki yarısını da grep'leyen regresyon koruması
  eklendi — bu hata sessiz olduğu için kimse şikayet etmez, sadece trafik kaybolur.

**Doğrulama.** Dev sunucusu bu hatayı gizler (`dotnet run` bilinmeyen yolları
`index.html`'e yönlendirir, 404.html yerelde hiç çalışmaz). GitHub Pages davranışını
birebir taklit eden bir statik sunucu yazıldı ve Release publish çıktısı onun
üzerinden test edildi:

| Test | Sonuç |
|---|---|
| `GET /key-connection` | HTTP **404** → uygulama açıldı, URL korundu |
| Uygulamada üretilen gerçek Share Link, temiz sekmede | Hesaplayıcı açıldı, girdiler birebir geri geldi |
| Hesap sonucu | Göbek basıncı 104,5 MPa — raporun doğruladığı değer |
| `/auth/callback#access_token=…` | Token'ları fragment'ten okudu (sahte token, beklenen yerde durdu) |

### 4.2 UX-3 — Mobil (Yükseltildi)

**İki ayrı sorun vardı, ikisi de ölçümle bulundu.**

*Navigasyona erişilemiyordu.* `.sidebar.open` CSS'te tanımlıydı ama `open` sınıfını
ekleyen hiçbir şey yoktu — ne NavMenu'de ne MainLayout'ta hamburger butonu vardı.

- 44×44 px hamburger butonu (açıkken X'e dönüyor), `aria-expanded` / `aria-label` /
  `aria-controls` ile.
- Yarı saydam backdrop; tıklayınca kapanıyor.
- `LocationChanged` aboneliği: linke dokununca drawer kapanıyor.
- Mobilde `.main-content`'e 68px üst boşluk.

*Yatay taşma.* Kaynağı ızgara değildi: ana sayfadaki logo `width: 450px` ile sabitti
ve `max-width`'i yoktu. 390px ekranda tek başına `.main-content`'i 538px'e itiyordu
(flex öğeleri varsayılan `min-width:auto` ile içeriğinin altına inmez).

- Logo `.hero-logo` sınıfına taşındı, global `img { max-width: 100% }` eklendi.
- `.main-content`'e `min-width: 0`.
- **Raporun test etmediği ek bulgu:** veritabanı sayfalarında üç farklı sarmalayıcı
  adı kullanılıyordu ve sınıf tabanlı olanların ikisi de stilsizdi — Materials
  (`.table-container`) ve Bolt Database (`.table-responsive`) taşıyordu; Bearings
  yalnızca satır içi `overflow-x` sayesinde çalışıyordu. Üçü tek kuralda birleştirildi.

| Sayfa (390px) | Önce | Sonra |
|---|---|---|
| `/` | 149px taşma | **0** |
| `/materials` | 355px taşma | **0** (tablo kendi içinde kayıyor) |
| `/bearings`, `/boltdatabase` | taşma | **0** |
| Modül sayfaları | 0 | **0** |

Masaüstünde (1280px) hiçbir değişiklik yok: buton `display:none`, sidebar yerinde,
`margin-left:300px`.

### 4.3 UX-2 — Sessiz form doğrulama

Yeni `Shared/ValidationAlert.razor`. On modülün her birinde `Calculate()` artık
sessizce `return` etmek yerine hangi alanın eksik olduğunu isimlendiriyor. Mesajlar
her denemenin başında ve `ClearForm`'da temizleniyor; başarılı hesaptan sonra
kalmıyor.

Moment of Inertia ve Gear Pair'e dokunulmadı — kullanılabilir varsayılanlarla
geliyorlar ve ilkinin zaten kendi mekanizması var.

### 4.4 GS-1 — Hatalı standart atfı

DIN 703 ayar bilezikleri (Stellringe) standardıdır; sıkmalı göbek bağlantısıyla
ilgisizdir. Dört yerden kaldırıldı (`Index.razor`, `About.razor` ×2,
`ModuleMetadataService.cs`) ve motor sınıfının XML yorumu düzeltildi. Yerine
VDI 2230 (cıvata tarafı) kondu; About sayfasının standart listesine DIN 6892 eklendi.

---

## 5. Önbellek Konvansiyonu

Doğrulama sırasında düzeltmelerin uygulanmadığı sanıldı; meğer tarayıcı
`index.html`'i önbellekten servis ediyordu. Site zaten `css/app.css?v=N` şeklinde
sürümlüyor:

- `app.css` → `?v=7` (bu turdaki CSS değişiklikleri).
- `viewer3d.js`'te sürümleme **yoktu**; eklendi (`?v=2`). IF-1 düzeltmesi tam da bu
  dosyaya bağlı ve bayat bir görüntüleyici görünür şekilde hata vermek yerine
  *yanlış model çiziyor*.

---

## 6. Açık Kalan Maddeler

**UX-1'in SEO kısmı düzelmedi.** Yapılan değişiklik akışı insanlar için kurtarıyor,
ama sunucu hâlâ `/single-bolt` için HTTP 404 dönüyor; Google 404 durum kodu gören
bir URL'i JS yönlendirmesi olsa da indekslemez.

Gerçek çözüm publish sırasında her rota için statik bir `index.html` üretmek.
Yapılmadı çünkü GitHub Pages `/single-bolt` → `/single-bolt/` şeklinde 301 atar ve
Blazor router'ının sondaki eğik çizgiyi eşleyip eşlemediği yerelde doğrulanamıyor;
yanlış giderse on iki modülün tamamının yönlendirmesi kırılır. Ayrıca sitemap ve
sayfaların `CanonicalUrl` değerleri eğik çizgisiz. Önce gerçek host üzerinde tek bir
rota denenmeli.

**GS-4 (Methodology / Verification sayfası)** yapılmadı — bir hata değil, içerik
işi. Üç modül hâlâ `IsVerified = false` ve nedenleri yalnızca kod yorumlarında.

**Raporlarda olmayan, yol üstünde fark edilen:** `SingleBolt.razor`'da Share Link,
Save to account ve 3D viewer yok — CLAUDE.md paylaşım linklerinin "on iki
hesaplayıcının hepsinde" olduğunu söylüyor, test raporu da aksiyon setinin tüm
modüllerde aynı olduğunu yazmış; ikisi de bu modül için yanlış.

**Önerilen sonraki adım:** raporlardaki el hesabıyla doğrulanmış ~60 sayıdan
golden-value birim testleri (xUnit) çıkarmak. CLAUDE.md'de "hiç çalıştırılmadığı
için kaldırıldı" diye kayıtlı bir doğrulama çatısı var; bu sayılar tam olarak onun
eksik olan girdisi.

---

## 7. Doğrulama Yöntemi

Her düzeltme için raporun kendi test senaryosu tarayıcıda tekrar çalıştırıldı ve
çıktılar raporun el hesabıyla doğruladığı değerlerle karşılaştırıldı. Ek olarak:

- **Cıvata motoru** için ayrı bir konsol harness'ı yazıldı (motor dosyaları link
  edilerek), beş senaryo çalıştırıldı. SF_clamp bıçak sırtı hatası bu sayede
  bulundu — tarayıcıda görünmüyordu.
- **UX-1** için GitHub Pages'in 404 davranışını taklit eden bir statik sunucu
  yazıldı; dev sunucusu bu hatayı yapısal olarak gizliyor.
- Her turda konsol hataları kontrol edildi; `sendGAEvent` dışında (yerelde GA yok,
  mevcut ve ilgisiz) hata yok.
- Her değişiklikten sonra `dotnet build` — 0 uyarı, 0 hata.
