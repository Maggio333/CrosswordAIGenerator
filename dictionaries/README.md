# Słownik polskich słów

W tym katalogu umieść plik tekstowy z polskimi słowami (jedno słowo na linię).

## 🚀 Szybki start

### Krok 1: Pobierz słownik

**Automatycznie (PowerShell):**
```powershell
cd dictionaries
.\download_dictionary.ps1
```

**Lub ręcznie:**
1. Pobierz plik: https://raw.githubusercontent.com/michalburzynski/polish-words/master/slowa.txt
2. Zapisz jako: `dictionaries/slowa.txt`

### Krok 2: Sprawdź plik

Upewnij się, że plik istnieje:
```
dictionaries/slowa.txt
```

### Krok 3: Uruchom aplikację

Aplikacja automatycznie znajdzie i załaduje słownik z `dictionaries/slowa.txt`.

**Gotowe!** 🎉

---

## 📋 Szczegóły

## Format pliku
- Jedno słowo na linię
- Kodowanie: UTF-8
- **Nazwa pliku: `slowa.txt`** (wymagane!)
- Minimum: 6 liter na słowo
- Przykład:
```
SAMOCHÓD
AUTOBUS
POCIĄG
KSIĄŻKA
ŁÓDŹ
ŚWIĘTY
...
```

## Gdzie znaleźć słownik?

### ⚠️ Ważne: Słownik z polskimi znakami

Aplikacja **wymaga** pliku `slowa.txt` z polskimi znakami diakrytycznymi (Ą, Ć, Ę, Ł, Ń, Ó, Ś, Ź, Ż).

### Opcja 1: Automatyczne pobranie (zalecane)

```powershell
cd dictionaries
.\download_dictionary.ps1
```

Skrypt automatycznie pobierze słownik z GitHub i zapisze jako `slowa.txt`.

### Opcja 2: Ręczne pobranie

1. **GitHub - polish-words** (zalecane):
   - Pobierz z: https://raw.githubusercontent.com/michalburzynski/polish-words/master/slowa.txt
   - Zapisz jako: `slowa.txt` w katalogu `dictionaries/`

2. **Polimorfologik**:
   - Pobierz z: https://github.com/morfologik/polimorfologik/releases
   - Plik: `polimorfologik-2.1.txt` (lub nowszy)
   - Format: `slowo +spacja+ tagi` - wyciągnij tylko pierwszą kolumnę (słowa)
   - Zapisz jako: `slowa.txt` w katalogu `dictionaries/`

3. **Oficjalny Słownik Polskiego Scrabblisty (OSPS)**:
   - Zawiera wszystkie polskie znaki
   - Dostępny na stronie PZScrabble
   - Zapisz jako: `slowa.txt` w katalogu `dictionaries/`

## Jak użyć?

### Automatyczne pobranie (PowerShell):
```powershell
cd dictionaries
.\download_dictionary.ps1
```

### Ręczne pobranie:
1. Pobierz plik z polskimi słowami (jedno słowo na linię)
2. **Zapisz jako `slowa.txt`** w katalogu `dictionaries/`
3. Uruchom aplikację - automatycznie załaduje słownik z pliku
4. **Uwaga:** Jeśli plik nie istnieje, aplikacja nie uruchomi się (wymagany plik)

## Uwagi

- Aplikacja automatycznie filtruje słowa o min 6 literach
- Słowa są konwertowane na wielkie litery
- Duplikaty są automatycznie usuwane

