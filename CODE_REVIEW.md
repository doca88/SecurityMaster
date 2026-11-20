# Code review: SecurityMaster backend

**Datum:** 2026-11-20
**Obim:** `SecurityMaster.Core`, `SecurityMasterService`, `SecurityMasterService.Tests` (26 testova)
**Status:** samo review, ništa nije menjano

**Ocena:** arhitektura i čitljivost 7/10, bezbednost 2/10

> ⚠️ Auth popravke (tačke 1–3) rade se **zajedno sa Angular frontendom** (`D:\Angular\01-starting-project\CODE_REVIEW.md`, tačke 1–3). Jedno bez drugog ne radi.

---

## 🔴 Kritično (bezbednost)

### 1. Autentikacija se nigde ne primenjuje, pa je ceo API javan
- `Program.cs:38-67` podešava JWT (`AddAuthentication` + `AddJwtBearer`), ali nema `app.UseAuthentication()` / `app.UseAuthorization()`.
- Nijedan kontroler ni hub nema `[Authorize]`.
- `[AllowAnonymous]` na `AuthController.cs:14` pokazuje da je namera bila da sve ostalo bude zaštićeno.

**Posledica:** svako bez tokena može da čita, menja i briše sve, uključujući SignalR hub.

**Rešenje:**
```csharp
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();
app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();
```

### 2. Tajne su commit-ovane u git
`SecurityMasterService/appsettings.json` je praćen u gitu i sadrži:
- `sa` lozinku za SQL Server (connection string `PC`)
- JWT ključ za potpisivanje. Ko ima repo, može sam da napravi validan admin token.

**Rešenje:**
- prebaciti u User Secrets (`dotnet user-secrets`) ili environment varijable
- promeniti `sa` lozinku, jer stara ostaje u git istoriji
- repo je privatan, pa čišćenje istorije nije hitno, ali **jeste** ako repo ikad postane javan

### 3. Zakucan korisnik `admin` / `admin`
`AuthController.cs:28`. U kombinaciji sa tačkom 2 zaštita je praktično nulta i kad se tačka 1 popravi.

### 4. Tabele se prave u bazi `master`
Oba connection stringa koriste `Database=master`, pa aplikacione tabele završavaju u sistemskoj bazi. Treba napraviti posebnu bazu (npr. `SecurityMaster`) i nalog sa minimalnim pravima umesto `sa`.

---

## 🟠 Bagovi

### 5. Order sa alokacijama ne može da se obriše
`OrdersController.cs:157-159` vraća 409 ako order ima alokacije. Alokacije se prave zajedno sa orderom (`Create` ih prima u istom zahtevu), pa praktično nijedan pravi order ne može da se obriše. Alokacije verovatno treba brisati zajedno sa orderom.

### 6. `Orders.Update` ne proverava da li Manager/Strategy/Security postoje
`Create` (`:54-64`) proverava sva tri, a `Update` (`:108-112`) samo upisuje ID-jeve. Pogrešan ID izaziva FK grešku i **500** umesto 400.

### 7. `Allocations.Update` nema nikakvu validaciju
`AllocationsController.cs:82-96`:
- ne proverava postojanje Manager/Strategy/Order, pa nevalidan ID daje **500**
- ne proverava `Quantity > 0`, a ne proverava ni `Create` u istom kontroleru
- dozvoljava prebacivanje alokacije na **drugi order** promenom `OrderId`
- vraća `Manager`/`Strategy` kao `null` ili zastarele, jer nema `Include`

### 8. `SecuritiesController.Delete` šalje notifikaciju pre `SaveChanges`
`SecuritiesController.cs:81-82`: ako snimanje padne, klijenti su već obavešteni o brisanju koje se nije desilo. Treba zameniti redosled. U ostalim kontrolerima je ispravno.

### 9. `ServiceQuery`: neispravan unos klijenta daje 500 umesto 400
`ServiceQuery.cs`:
- nepostojeće polje baca `ArgumentException` (`:43`, `:71`)
- neispravna vrednost (`"abc"` za int) baca `FormatException` (`:87`)
- `Contains` nad ne-string poljem poziva `ToString()` (`:49`), što EF često ne može da prevede
- filter preko kolekcije (`Allocations.ManagerId`, primer iz `Program.cs:163`) ne radi

**Rešenje:** hvatati te izuzetke i vraćati `400 BadRequest` sa porukom.

### 10. Nedostaju validacije dužine stringova
`Description` (255) i `Display` (100) nemaju `[MaxLength]` u Request klasama, pa predugačak unos daje **500** iz baze.

### 11. Race condition kod brisanja
Svi `Delete` endpointi rade "proveri, pa obriši". Ako se između ta dva koraka pojavi referenca, FK `Restrict` baci izuzetak i klijent dobija **500** umesto 409. Treba hvatati `DbUpdateException`.

---

## 🟡 Dizajn / kvalitet

12. **Entiteti se vraćaju kao API odgovor.** EF entiteti su u folderu `DTO` i direktno se serijalizuju. Treba razdvojiti `Entities/` i response DTO-e.
13. **Globalni namespace:** sve klase u `SecurityMaster.Core` nemaju `namespace`.
14. **Nema paginacije:** `/query` endpointi vraćaju celu tabelu.
15. **Nema poslovne validacije ordera:** ni `Order.Quantity > 0` ni zbir alokacija = količina ordera. Frontend proverava zbir, backend ne.
16. **`ILogger` se svuda injektuje, a nigde ne koristi.**
17. **Log ide na Desktop** (`Program.cs:13-14`). Putanju treba čitati iz konfiguracije.
18. **Connection string po imenu mašine** (`Program.cs:30`). Na nepoznatoj mašini dobija se `null` i aplikacija puca tek na prvom zahtevu.
19. **Komentar ne odgovara kodu:** `NotificationHub.cs:16` kaže da se uz `OrderChanged` šalje `orderId`, ali se ništa ne šalje.
20. **Verzija paketa:** `JwtBearer` je `9.*` uz .NET 10, a ostali paketi su `10.0.11`. Treba fiksirati i uskladiti.
21. **Duplirani Request tipovi:** `CreateAllocationRequest` i `AllocationRequest`. Uz to `Update` endpointi koriste `Create*Request`.
22. **Mrtav kod:** oko 80 linija zakomentarisanog minimal API-ja (`Program.cs:78-179`) i `//TODO` na `:71`.
23. **Formatiranje:** nekonzistentno uvlačenje (8 umesto 4 razmaka, `OrdersController.cs:17-22`, `AllocationsController.cs:77,107`, `ManagersController.cs:9-12`, `SecuritiesController.cs:29,81`).

---

## Plan

| # | Šta | Trud |
|---|---|---|
| 1 | Auth (`UseAuthentication` + `RequireAuthorization`), zajedno sa frontendom | ~5 min |
| 2 | Tajne u User Secrets + nova `sa` lozinka | ~15 min |
| 4 | Posebna baza umesto `master` | ~10 min + migracija |
| 5–8 | Bagovi u kontrolerima | ~1h |
| 9–11 | 400/409 umesto 500 | ~30 min |
| 12+ | Refaktorisanje i kozmetika | po želji |
