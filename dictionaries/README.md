# Słowniki polskich słów

W tym katalogu umieść plik tekstowy z polskimi słowami (jedno słowo na linię).

## Format pliku
- Jedno słowo na linię
- Kodowanie: UTF-8
- Nazwa pliku: `polish_words.txt`
- Przykład:
```
SAMOCHÓD
AUTOBUS
POCIĄG
KSIĄŻKA
...
```

## Gdzie znaleźć słownik?

### ⚠️ Ważne: Słownik z polskimi znakami

**Obecny słownik `words.polish.txt.gz` NIE zawiera polskich znaków diakrytycznych** (Ą, Ć, Ę, Ł, Ń, Ó, Ś, Ź, Ż).

Aplikacja używa **fallback** (Ł→L, Ą→A, etc.), więc działa, ale krzyżówki będą bardziej realistyczne z prawdziwym słownikiem.

### Opcja 1: Ręczne pobranie (zalecane)

1. **Polimorfologik** (najlepszy - zawiera polskie znaki):
   - Pobierz z: https://github.com/morfologik/polimorfologik/releases
   - Plik: `polimorfologik-2.1.txt` (lub nowszy)
   - Format: `slowo +spacja+ tagi` - aplikacja automatycznie wyciągnie tylko słowa
   - Zapisz jako: `polish_words.txt` w katalogu `dictionaries/`

2. **Oficjalny Słownik Polskiego Scrabblisty (OSPS)**:
   - Zawiera wszystkie polskie znaki
   - Dostępny na stronie PZScrabble

3. **Własny słownik**:
   - Utwórz plik `polish_words.txt` w katalogu `dictionaries/`
   - Format: jedno słowo na linię, UTF-8, min 6 liter
   - Przykład:
     ```
     SAMOCHÓD
     POCIĄG
     KSIĄŻKA
     ŁÓDŹ
     ```

### Opcja 2: Automatyczne pobranie (może nie działać)

```powershell
cd dictionaries
.\download_dictionary.ps1
```

**Uwaga:** Skrypt próbuje pobrać z GitHub, ale źródła mogą być niedostępne (404). W takim przypadku pobierz ręcznie.

### Opcja 3: Użyj obecnego słownika z fallback

Możesz używać `words.polish.txt.gz` - aplikacja automatycznie użyje fallback dla polskich znaków:
- `Ł` → `L`
- `Ą` → `A`
- `Ć` → `C`
- etc.

Krzyżówki będą działać, ale słowa mogą być mniej realistyczne.

## Jak użyć?

### Automatyczne pobranie (PowerShell):
```powershell
cd dictionaries
.\download_dictionary.ps1
```

### Ręczne pobranie:
1. Pobierz plik z polskimi słowami (jedno słowo na linię)
2. Zapisz jako `polish_words.txt` w katalogu `dictionaries`
3. Uruchom aplikację - automatycznie załaduje słownik z pliku
4. Jeśli plik nie istnieje, używa domyślnego małego słownika

## Uwagi

- Aplikacja automatycznie filtruje słowa o min 6 literach
- Słowa są konwertowane na wielkie litery
- Duplikaty są automatycznie usuwane

