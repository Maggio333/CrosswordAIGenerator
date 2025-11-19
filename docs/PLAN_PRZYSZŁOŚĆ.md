# Plan przyszłości - DSL Format i Generator Krzyżówek z Przecięciami

## 🎯 Cel główny
Stworzenie pipeline'u do generowania nieograniczonego datasetu krzyżówek z przecięciami słów dla finetunowania Bielika 4B.

## 📋 Architektura Pipeline

### Workflow końcowy:
1. **Generator słów** → losuje słowa z przecięciami
2. **Układanie deterministyczne** → rozmieszcza słowa w siatce
3. **XAML Generator** → tworzy XAML z układu
4. **DSL Generator** → konwertuje XAML → DSL (własny format)
5. **Dataset** → DSL + XAML + spec + screenshot
6. **Finetune** → Bielik 4B uczy się DSL → XAML

### Runtime (po finetunie):
1. **Odpytanie 1**: Algorytm wyboru słów + położenie → spec
2. **Odpytanie 2**: Spec + kontekst → model generuje **DSL**
3. **Mapper DSL → XAML**: deterministyczna konwersja
4. **Render**: XAML → screenshot JPG

## 🔤 DSL Format (własny format pośredni)

### Koncepcja
DSL (Domain Specific Language) - tekstowy format łatwy do generowania przez LLM i parsowania.

### Przykładowy format (do ustalenia):
```
GRID 10x10
CELL 0,0 EMPTY
CELL 0,1 LETTER K
CELL 0,2 LETTER O
CELL 0,3 LETTER T
CELL 0,4 WALL
CELL 1,1 LETTER A
CELL 2,1 LETTER S
...
```

### Zalety DSL:
- ✅ Łatwe generowanie przez LLM (tekst strukturalny)
- ✅ Proste parsowanie (regex/split)
- ✅ Czytelne dla człowieka
- ✅ Możliwość walidacji
- ✅ Krótsze niż XAML

### Do zrobienia:
- [ ] Zdefiniować dokładną składnię DSL
- [ ] Stworzyć parser DSL → XAML
- [ ] Stworzyć generator XAML → DSL (dla datasetu)
- [ ] Dodać walidację DSL
- [ ] Dodać DSL do DatasetEntry

## 🎲 Generator Krzyżówek z Przecięciami

### Algorytm:
1. Losuj pierwsze słowo (np. "KOT")
2. Dla każdej litery w słowie:
   - Szukaj losowego słowa które ma tę literę
   - Jeśli nie ma - losuj kolejne
   - Układaj prostopadle (poziomo/pionowo)
3. Powtarzaj aż mamy N słów (np. 4-6)
4. Układaj deterministycznie w siatce

### Przykład:
```
Słowo 1: "KOT" (poziomo od 0,0)
  - Litera 'K' na (0,0) → szukaj słowa z 'K' → "KROWA" (pionowo od 0,0)
  - Litera 'O' na (0,1) → szukaj słowa z 'O' → "DOM" (pionowo od 0,1)
  - Litera 'T' na (0,2) → szukaj słowa z 'T' → "TRAWA" (pionowo od 0,2)
```

### Do zrobienia:
- [ ] Słownik słów (pliki tekstowe lub baza)
- [ ] Klasa `CrosswordWordPlacer` - algorytm układania
- [ ] Wyszukiwanie słów z określoną literą
- [ ] Detekcja przecięć
- [ ] Walidacja układu (brak konfliktów)
- [ ] Generator wielu krzyżówek (tysiące)

## 🔄 Mapper DSL ↔ XAML

### DSL → XAML (deterministyczny)
- Parsuje DSL
- Tworzy CrosswordGrid
- Generuje XAML przez XamlGenerator

### XAML → DSL (dla datasetu)
- Parsuje XAML
- Ekstraktuje strukturę (kratki, litery, ściany)
- Generuje DSL string

### Do zrobienia:
- [ ] Klasa `DslParser` - parsuje DSL → CrosswordGrid
- [ ] Klasa `DslGenerator` - konwertuje CrosswordGrid → DSL
- [ ] Walidacja DSL (sprawdzanie poprawności)
- [ ] Obsługa błędów (niepoprawny DSL)

## 📦 Rozszerzenie DatasetEntry

### Nowa struktura:
```json
{
  "id": "...",
  "xaml": "<Grid>...</Grid>",
  "dsl": "GRID 10x10\nCELL 0,0 EMPTY\n...",
  "spec": {
    "words": [...],
    "layout": {...}
  },
  "description": "...",
  "screenshot_base64": "..."
}
```

### Do zrobienia:
- [ ] Dodać pole `Dsl` do DatasetEntry
- [ ] Zaktualizować DatasetGenerator o generowanie DSL
- [ ] Eksport datasetu z DSL

## 🚀 Pipeline Generowania Datasetu

### Etapy:
1. **Generator słów** → lista słów z przecięciami
2. **WordPlacer** → układanie w siatce
3. **XamlGenerator** → XAML
4. **DslGenerator** → DSL
5. **Screenshot** → JPG
6. **DatasetEntry** → wszystko razem

### Do zrobienia:
- [ ] Integracja wszystkich komponentów
- [ ] Batch generator (1000+ krzyżówek)
- [ ] Progress tracking
- [ ] Export do formatu dla finetunowania

## 🎓 Finetune Workflow

### Dataset dla LoRA:
- Input: Spec + kontekst → Output: DSL
- Tysiące przykładów: (spec, DSL) pairs

### Po finetunie:
- Model generuje DSL z promptu
- Mapper DSL → XAML
- Render XAML → screenshot

### Do zrobienia (później):
- [ ] Format datasetu dla LoRA (JSONL?)
- [ ] Prompt templates
- [ ] Ewaluacja jakości DSL

## 📝 Notatki

- DSL format - do ustalenia dokładnej składni
- Generator krzyżówek - algorytm układania z przecięciami
- Słownik - źródło słów (pliki tekstowe?)
- Mapper - deterministyczna konwersja DSL ↔ XAML
- Dataset - nieograniczony dzięki losowaniu słów

## 🔗 Zależności

1. Najpierw: Generator krzyżówek z przecięciami
2. Potem: DSL format + parser
3. Następnie: Mapper DSL ↔ XAML
4. Na końcu: Pipeline generowania datasetu

