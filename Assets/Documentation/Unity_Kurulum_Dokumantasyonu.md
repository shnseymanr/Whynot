# Kurak Koye Su Getirme - Unity 3D Kurulum Dokumantasyonu

Bu proje 3D oyun prototipi olarak kurulmalidir. Onceki 2D fizik anlatimi kaldirildi. Artik karakter, dusman, projectile, trigger ve zemin kurulumlari Unity'nin 3D componentleriyle yapilir.

Kullanilacak temel 3D componentler:

- `Rigidbody`
- `BoxCollider`
- `CapsuleCollider`
- `SphereCollider`
- `Collider` trigger
- `Camera`
- `Canvas`

Kullanilmamasi gereken 2D componentler:

- `Rigidbody2D`
- `BoxCollider2D`
- `CircleCollider2D`
- `Collider2D`
- `Physics2D`

## 1. Oyun Mantigi

Oyuncu kurak koyden magaraya gider, magarada dusmanlardan gelen su saldirilarini emerek su toplar, cani bitmeden koye doner ve koyu gelistirir.

Akis:

1. Oyuncu `VillageScene` sahnesinde baslar.
2. Magara giris trigger'ina girince `CaveScene` yuklenir.
3. Magarada dusmanlar oyuncuya dogru yurur.
4. Dusmanlar oyuncuya su projectile firlatir.
5. Dusman projectile oyuncuya carparsa oyuncunun cani azalir ve su bari artar.
6. Oyuncu sol tik veya Ctrl ile su projectile atar.
7. Oyuncunun her atisi su barini azaltir.
8. Su bari dolunca oyuncu magara cikisindan koye donebilir.
9. Ilk donuste tarla sulanir.
10. Ikinci donuste baraj dolar ve elektrik gelir.
11. Fade sonrasi koy talan edilmis hale gecer.
12. Boss sahneye gelir.

## 2. Script Yapisi

Script sayisi az tutuldu. Bazi scriptler Inspector'daki `Role` alanina gore farkli gorev yapar.

| Script | Gorev |
| --- | --- |
| `GameManager.cs` | Sahne gecisi, kayitli can/su, koy asamasi, genel oyun akisi |
| `PlayerController.cs` | 3D hareket, ziplama, can/su sistemi, oyuncu atesi |
| `WaterProjectile.cs` | Oyuncu ve dusman su mermileri |
| `EnemyController.cs` | Normal dusman, boss ve spawner rolleri |
| `WorldController.cs` | HUD, koy progress, giris/cikis triggerlari, fade, kamera disi temizleme |
| `GameHUDBuilderTool.cs` | Editor menusunden HUD olusturma tool'u |

## 3. Sahne Listesi

Iki ana sahne gerekir:

- `VillageScene`
- `CaveScene`

Kurulum:

1. `Assets/Scenes` klasoru olustur.
2. `File > New Scene` ile sahne olustur.
3. Ilk sahneyi `Assets/Scenes/VillageScene.unity` olarak kaydet.
4. Ikinci sahneyi `Assets/Scenes/CaveScene.unity` olarak kaydet.
5. `File > Build Settings` ac.
6. Iki sahneyi de `Scenes In Build` listesine ekle.

Sahne adlari `GameManager` Inspector alanlariyla birebir ayni olmalidir.

## 4. Kamera Kurulumu

Bu 3D oyun icin kamera `Perspective` kalabilir.

Basit prototip kamera ayari:

1. `Main Camera` sec.
2. `Projection`: `Perspective`
3. Position:
   - X: 0
   - Y: 7
   - Z: -10
4. Rotation:
   - X: 35
   - Y: 0
   - Z: 0
5. `Field of View`: 60

Bu ayar sahneye yukaridan hafif egimli bakar. Side-scroller hissi istiyorsan kamerayi daha yandan konumlandirabilirsin:

- Position: `0, 4, -12`
- Rotation: `20, 0, 0`

Top-down hissi istiyorsan:

- Position: `0, 12, 0`
- Rotation: `90, 0, 0`

## 5. Tag ve Layer Onerileri

`Edit > Project Settings > Tags and Layers` ekranindan ekle.

Tag onerileri:

- `Player`
- `Enemy`
- `Boss`
- `Projectile`
- `Ground`
- `CaveExit`
- `VillageEntrance`

Layer onerileri:

- `Ground`
- `Player`
- `Enemy`
- `Projectile`
- `Trigger`

Player'in ziplama kontrolu icin zemin objelerinin `Ground` layer'inda olmasi onemlidir.

## 6. VillageScene Ana Objeleri

`VillageScene` icinde bulunmasi gereken objeler:

- `Main Camera`
- `Directional Light`
- `GameManager`
- `Player`
- `Ground`
- `VillageEntrance`
- `VillageProgress`
- `GameHUD_Canvas`
- `FadeController`

## 7. GameManager Kurulumu

1. `VillageScene` ac.
2. Hierarchy'de `Create Empty` sec.
3. Objeyi `GameManager` olarak adlandir.
4. `GameManager.cs` scriptini ekle.

Inspector ayarlari:

| Alan | Deger |
| --- | --- |
| Village Scene Name | `VillageScene` |
| Cave Scene Name | `CaveScene` |
| Max Health | 100 |
| Current Health | 100 |
| Max Water | 100 |
| Current Water | 0 |
| Required Water To Exit | 100 |
| Village Stage | Dry |

`GameManager`, `DontDestroyOnLoad` kullandigi icin sahne gecislerinde kaybolmaz.

## 8. Player Kurulumu

Player 3D obje olmalidir. Basit prototip icin Capsule kullanmak en rahati.

### Player Objesi

1. Hierarchy'de sag tikla.
2. `3D Object > Capsule` sec.
3. Objeyi `Player` olarak adlandir.
4. Position:
   - X: 0
   - Y: 1
   - Z: 0
5. Tag: `Player`
6. Layer: `Player`

### Player Componentleri

Player objesinde sunlar olmali:

- `CapsuleCollider`
- `Rigidbody`
- `PlayerController`

`Rigidbody` ayarlari:

| Alan | Deger |
| --- | --- |
| Use Gravity | Aktif |
| Is Kinematic | Kapali |
| Constraints | Freeze Rotation X, Y, Z |

Rotation freeze etmek karakterin carpisma veya ziplama sirasinda devrilmesini engeller.

### GroundCheck

Player altina bos obje ekle:

1. Player'a sag tikla.
2. `Create Empty` sec.
3. Adini `GroundCheck` yap.
4. Local Position:
   - X: 0
   - Y: -1
   - Z: 0

Bu obje zemine yakin durmali. `PlayerController`, burada kucuk bir sphere kontrolu yaparak yerde olup olmadigini anlar.

### FirePoint

Player altina bos obje ekle:

1. Player'a sag tikla.
2. `Create Empty` sec.
3. Adini `FirePoint` yap.
4. Local Position:
   - X: 0
   - Y: 1
   - Z: 1
5. FirePoint'in mavi oku yani local `Z` ekseni ileriye bakmali.

Projectile `FirePoint.forward` yonunde gider.

### PlayerController Inspector

Movement:

| Alan | Deger |
| --- | --- |
| Move Speed | 6 |
| Jump Force | 12 |
| Allow Depth Movement | Aktif |
| Face Move Direction | Aktif |
| Ground Check | `GroundCheck` |
| Ground Check Radius | 0.25 |
| Ground Layer | `Ground` |

Stats:

| Alan | Deger |
| --- | --- |
| Max Health | 100 |
| Current Health | 100 |
| Max Water | 100 |
| Current Water | 0 |
| Required Water For Exit | 100 |

Shooting:

| Alan | Deger |
| --- | --- |
| Fire Point | `FirePoint` |
| Projectile Prefab | `WaterProjectile` prefab'i |
| Water Cost Per Shot | 10 |
| Projectile Speed | 12 |
| Fire Cooldown | 0.25 |

Kontroller:

- A/D veya sol/sag ok: X ekseninde hareket
- W/S veya yukari/asagi ok: Z ekseninde hareket
- Space: ziplama
- Sol mouse veya Ctrl: ates

Side-scroller gibi tek eksende hareket istiyorsan `Allow Depth Movement` kapat.

## 9. Ground Kurulumu

Village ve Cave sahnelerinde zemin 3D obje olmalidir.

1. `3D Object > Cube` olustur.
2. Adini `Ground` yap.
3. Position:
   - X: 0
   - Y: 0
   - Z: 0
4. Scale:
   - X: 30
   - Y: 1
   - Z: 20
5. Layer: `Ground`

Cube ile birlikte `BoxCollider` otomatik gelir. Bu collider kalmali.

## 10. WaterProjectile Prefab Kurulumu

Projectile 3D fizik kullanir.

1. `3D Object > Sphere` olustur.
2. Objeyi `WaterProjectile` olarak adlandir.
3. Scale:
   - X: 0.25
   - Y: 0.25
   - Z: 0.25
4. Tag: `Projectile`
5. Layer: `Projectile`
6. Componentler:
   - `SphereCollider`
   - `Rigidbody`
   - `WaterProjectile`
7. `SphereCollider > Is Trigger`: aktif
8. `Rigidbody > Use Gravity`: kapali
9. Prefab olarak `Assets/Prefabs/Projectiles/WaterProjectile.prefab` konumuna surukle.
10. Sahneden orijinal objeyi silebilirsin.

Inspector ayarlari:

| Alan | Deger |
| --- | --- |
| Damage | 10 |
| Water Gain On Player Hit | 15 |
| Life Time | 4 |

Boss icin daha buyuk bir prefab:

1. `WaterProjectile` prefab'ini duplicate et.
2. Adini `BigWaterProjectile` yap.
3. Scale'i `0.6, 0.6, 0.6` yap.
4. `Damage`: 25
5. `Water Gain On Player Hit`: 25

## 11. Enemy Prefab Kurulumu

Normal dusman icin `EnemyController` kullanilir ve `Role = Enemy` secilir.

1. `3D Object > Capsule` olustur.
2. Adini `Enemy` yap.
3. Tag: `Enemy`
4. Layer: `Enemy`
5. Componentler:
   - `CapsuleCollider`
   - `Rigidbody`
   - `EnemyController`
6. `Rigidbody`:
   - Use Gravity: aktif
   - Is Kinematic: kapali
   - Constraints: Freeze Rotation X, Y, Z
7. Enemy altina `FirePoint` child objesi ekle.
8. `FirePoint` local position:
   - X: 0
   - Y: 1
   - Z: 1
9. `EnemyController > Role`: `Enemy`
10. `Fire Point`: `FirePoint`
11. `Projectile Prefab`: `WaterProjectile`
12. Prefab olarak kaydet.

Onerilen ayarlar:

| Alan | Deger |
| --- | --- |
| Max Health | 30 |
| Move Speed | 2 |
| Shoot Interval | 1.5 |
| Projectile Speed | 7 |

Dusmanlar engel aramaz; player'a dogru duz cizgide yurur.

## 12. Boss Prefab Kurulumu

Boss icin ayri script yoktur. `EnemyController` kullanilir, `Role = Boss` secilir.

1. `3D Object > Capsule` olustur.
2. Adini `Boss` yap.
3. Scale:
   - X: 2
   - Y: 2
   - Z: 2
4. Tag: `Boss`
5. Layer: `Enemy`
6. Componentler:
   - `CapsuleCollider`
   - `Rigidbody`
   - `EnemyController`
7. `EnemyController > Role`: `Boss`
8. Altina `FirePoint` ekle.
9. `Projectile Prefab`: `BigWaterProjectile`
10. Prefab olarak kaydet.

Onerilen ayarlar:

| Alan | Deger |
| --- | --- |
| Max Health | 300 |
| Move Speed | 1.5 |
| Shoot Interval | 2 |
| Projectile Speed | 8 |

## 13. HUD Kurulumu

HUD'u tool ile olustur.

1. `VillageScene` ac.
2. Ust menuden `Tools > GameJam > Build Game HUD` sec.
3. `GameHUD_Canvas` olusur.
4. Ayni islemi `CaveScene` icin de yap.

Tool su objeleri olusturur:

- `GameHUD_Canvas`
- `HUD_Panel`
- `Health_Slider`
- `Water_Slider`
- `Quest_Text`
- `VillageStage_Text`
- `CollectedWater_Text`
- `ActiveTask_Text`

`GameHUD_Canvas` uzerinde `WorldController` bulunur. `Role` degeri `Hud` olarak kalmali.

## 14. Fade Kurulumu

Fade icin UI Image ve `WorldController Role = Fade` kullanilir.

1. `GameHUD_Canvas` altina UI `Image` olustur.
2. Adini `Fade_Image` yap.
3. RectTransform:
   - Anchor Min: 0, 0
   - Anchor Max: 1, 1
   - Offset Min: 0, 0
   - Offset Max: 0, 0
4. Image rengini siyah yap.
5. Alpha degerini 0 yap.
6. Bos obje olustur: `FadeController`
7. `WorldController` ekle.
8. `Role`: `Fade`
9. `Fade Image`: `Fade_Image`
10. `Fade Duration`: 0.75
11. `Start Fade Clear`: aktif

Bu kurulumu iki sahnede de yap.

## 15. VillageEntrance Kurulumu

Magara girisi 3D trigger collider ile calisir.

1. `VillageScene` icinde `3D Object > Cube` olustur.
2. Adini `VillageEntrance` yap.
3. Magara kapisi olacak yere koy.
4. `BoxCollider > Is Trigger`: aktif
5. Mesh Renderer'i istersen kapatabilirsin.
6. `WorldController` ekle.
7. `Role`: `VillageEntrance`

Player bu trigger'a girince `CaveScene` yuklenir.

## 16. VillageProgress Kurulumu

Koyun gelisim asamalarini `WorldController Role = VillageProgress` yonetir.

1. Bos obje olustur: `VillageProgress`
2. `WorldController` ekle.
3. `Role`: `VillageProgress`
4. Sahnede placeholder objeler olustur:
   - `DryField`
   - `WateredField`
   - `EmptyDam`
   - `FullDam`
   - `ElectricityLights`
   - `RaidedVillageObjects`
   - `BossSpawnPoint`
5. Bu objeleri Inspector'daki ilgili alanlara ata.
6. `Boss Prefab` alanina Boss prefab'ini ata.
7. `Boss Spawn Point` alanina `BossSpawnPoint` objesini ata.

Asamalar:

| Stage | Gorunum |
| --- | --- |
| 0 - Dry | Kuru tarla, bos baraj |
| 1 - FieldWatered | Sulanmis tarla |
| 2 - DamFilled | Dolu baraj ve elektrik |
| 3 - Raided | Harap koy ve boss |

## 17. CaveScene Ana Kurulumu

`CaveScene` icinde bulunmasi gereken objeler:

- `Main Camera`
- `Directional Light`
- `Player`
- `Ground`
- `EnemySpawner`
- `CaveExit`
- `CameraCleanup`
- `GameHUD_Canvas`
- `FadeController`

Player ve Ground kurulumunu VillageScene ile ayni yapabilirsin. En pratik yontem Player'i prefab yapip iki sahnede kullanmaktir.

## 18. EnemySpawner Kurulumu

Spawner icin ayri script yoktur. `EnemyController Role = Spawner` kullanilir.

1. `CaveScene` icinde bos obje olustur: `EnemySpawner`
2. `EnemyController` ekle.
3. `Role`: `Spawner`
4. Spawn noktalari icin bos objeler olustur:
   - `EnemySpawn_01`
   - `EnemySpawn_02`
5. Bu noktalar magara yolunun kenarlarinda veya kameranin disinda olabilir.
6. `Spawn Points` listesine bu transformlari ata.
7. `Enemy Prefab` alanina Enemy prefab'ini ata.

Onerilen ayarlar:

| Alan | Deger |
| --- | --- |
| Spawn Interval | 2 |
| Max Alive Enemies | 5 |
| Spawn On Start | Aktif |

Spawner objesine `Rigidbody` veya Collider eklemek gerekmez.

## 19. CaveExit Kurulumu

Magara cikisi 3D trigger collider ile calisir.

1. `CaveScene` icinde `3D Object > Cube` olustur.
2. Adini `CaveExit` yap.
3. Cikis noktasina yerlestir.
4. `BoxCollider > Is Trigger`: aktif
5. Mesh Renderer'i istersen kapat.
6. `WorldController` ekle.
7. `Role`: `CaveExit`
8. `Require Full Water Bar`: aktif
9. `Required Water Override`: -1

`Required Water Override = -1`, gereken su miktarini Player/GameManager ayarlarindan al demektir.

## 20. CameraCleanup Kurulumu

Kamera disina cikan dusman ve projectile objelerini temizler.

1. `CaveScene` icinde bos obje olustur: `CameraCleanup`
2. `WorldController` ekle.
3. `Role`: `CameraCleanup`
4. `Target Camera`: `Main Camera`
5. `Viewport Padding`: 0.2
6. `Destroy Projectiles`: aktif
7. `Destroy Enemies`: aktif

## 21. Prefab Listesi

Minimum prefablar:

- `Player.prefab`
- `Enemy.prefab`
- `WaterProjectile.prefab`

Boss asamasi icin:

- `Boss.prefab`
- `BigWaterProjectile.prefab`

## 22. Ilk Test Sirasi

1. `VillageScene` ac.
2. Play'e bas.
3. Player W/A/S/D ile hareket ediyor mu?
4. Player Space ile zipliyor mu?
5. Sol tik veya Ctrl ile projectile atiyor mu?
6. HUD gorunuyor mu?
7. `VillageEntrance` trigger'ina girince `CaveScene` yukleniyor mu?
8. `CaveScene` icinde dusman spawn oluyor mu?
9. Dusman player'a dogru yuruyor mu?
10. Dusman projectile atiyor mu?
11. Dusman projectile player'a carptiginda can azaliyor ve su artiyor mu?
12. Player projectile dusmana hasar veriyor mu?
13. Su dolmadan `CaveExit` calismiyor mu?
14. Su dolunca `CaveExit` koye donduruyor mu?
15. Ilk donuste tarla sulaniyor mu?
16. Ikinci donuste baraj doluyor ve elektrik aciliyor mu?
17. Fade sonrasi harap koy ve boss geliyor mu?

## 23. Sik Sorunlar

### Player Hareket Etmiyor

- Player objesinde `Rigidbody` var mi?
- `Rigidbody` kinematic kapali mi?
- `PlayerController` ekli mi?
- Constraints sadece rotation'i mi freeze ediyor?

### Player Ziplamiyor

- Ground objesi `Ground` layer'inda mi?
- `PlayerController > Ground Layer` alaninda `Ground` secili mi?
- `GroundCheck` zemine yakin mi?
- Ground objesinde `BoxCollider` var mi?

### Triggerlar Calismiyor

- Trigger objesinde `BoxCollider` var mi?
- `Is Trigger` aktif mi?
- Player'da `CapsuleCollider` veya baska bir 3D collider var mi?
- En az bir tarafta `Rigidbody` var mi? Player'da zaten `Rigidbody` olmali.
- Yanlislikla `BoxCollider2D` kullanilmadigindan emin ol.

### Projectile Carpmiyor

- Projectile prefabinda `SphereCollider` var mi?
- `SphereCollider > Is Trigger` aktif mi?
- Projectile prefabinda `Rigidbody` var mi?
- `Rigidbody > Use Gravity` kapali mi?
- Player/Enemy objelerinde 3D collider var mi?

### Dusman Spawn Olmuyor

- `EnemySpawner` objesinde `EnemyController` var mi?
- `Role`: `Spawner` mi?
- `Enemy Prefab` atanmis mi?
- `Spawn Points` listesi dolu mu?
- Enemy prefabinda `EnemyController Role = Enemy` secili mi?

### HUD Calismiyor

- `Tools > GameJam > Build Game HUD` calistirildi mi?
- `GameHUD_Canvas` uzerinde `WorldController Role = Hud` var mi?
- HUD objelerinin isimleri degistirilmedi mi?

### Sahne Gecisi Calismiyor

- `VillageScene` ve `CaveScene` Build Settings'e eklendi mi?
- `GameManager` sahne isimleri dogru mu?
- Entrance/Exit objelerinde `WorldController` var mi?
- Role degerleri dogru mu?

## 24. Kisa Ozet

Bu prototip artik tamamen 3D fizik uzerinden calisir.

Kritik kurallar:

- Player: `Capsule + Rigidbody + CapsuleCollider + PlayerController`
- Dusman: `Capsule + Rigidbody + CapsuleCollider + EnemyController`
- Projectile: `Sphere + Rigidbody + SphereCollider Trigger + WaterProjectile`
- Triggerlar: `Cube + BoxCollider Trigger + WorldController`
- Zemin: `Cube + BoxCollider + Ground Layer`
- 2D component kullanma.
