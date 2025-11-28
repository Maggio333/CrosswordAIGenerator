# QLoRA Training dla Bielik 4.5B v3 - Krzyżówki

Ten folder zawiera skrypty do treningu QLoRA modelu Bielik 4.5B v3 na datasetcie krzyżówek.

## Wymagania

- Python 3.8+
- NVIDIA GPU z CUDA (RTX 3050 6GB lub lepsza)
- CUDA toolkit (dla bitsandbytes)
- **Konto HuggingFace z dostępem do modelu Bielik 4.5B v3** (model jest "gated")

## Instalacja

### 1. Utwórz środowisko wirtualne

```bash
python -m venv venv
```

### 2. Aktywuj środowisko

**Windows (PowerShell):**
```powershell
.\venv\Scripts\Activate.ps1
```

**Windows (CMD):**
```cmd
venv\Scripts\activate.bat
```

**Linux/Mac:**
```bash
source venv/bin/activate
```

### 3. Zainstaluj zależności

```bash
pip install -r requirements.txt
```

**Uwaga:** Jeśli masz problemy z `bitsandbytes` na Windows, może być potrzebna dodatkowa konfiguracja. Sprawdź [dokumentację bitsandbytes](https://github.com/TimDettmers/bitsandbytes).

### 4. Autoryzacja HuggingFace

Model Bielik 4.5B v3 jest "gated" (wymaga autoryzacji). Musisz:

1. **Zalogować się na HuggingFace:**
   - Przejdź na https://huggingface.co/speakleash/Bielik-4.5B-v3.0-Instruct
   - Kliknij "Agree and access repository" aby poprosić o dostęp
   - Poczekaj na akceptację (zwykle automatyczna)

2. **Utworzyć token:**
   - Przejdź na https://huggingface.co/settings/tokens
   - Utwórz nowy token (typ: "Read")

3. **Zalogować się w terminalu:**

   **Opcja A - Użyj huggingface-cli:**
   ```bash
   huggingface-cli login
   ```
   Wklej swój token gdy zostaniesz poproszony.

   **Opcja B - Ustaw zmienną środowiskową:**
   ```powershell
   # PowerShell
   $env:HF_TOKEN="twój_token_tutaj"
   ```
   ```cmd
   # CMD
   set HF_TOKEN=twój_token_tutaj
   ```

   **Opcja C - Ustaw globalnie (Windows):**
   - Otwórz "Zmienne środowiskowe" w systemie
   - Dodaj nową zmienną: `HF_TOKEN` = `twój_token`

**Alternatywa:** Jeśli masz lokalny model w formacie HuggingFace, umieść go w:
```
PythonDatasetsScripts/speakleash/Bielik-4.5B-v3.0-Instruct/
```
(z plikami: `config.json`, `tokenizer.json`, `model.safetensors` itp.)

## Dataset

Dataset znajduje się w: `../datasets/crossword_finetune_20251125_200641.jsonl`

Format: JSONL z polami `prompt` i `response` (1000 rekordów)

## Trening

### Uruchomienie treningu

```bash
python train_crossword_qlora_1k.py
```

### Parametry treningu

- **Model:** `speakleash/Bielik-4.5B-v3.0-Instruct`
- **Quantization:** 4-bit (QLoRA)
- **LoRA:** r=16, alpha=32, dropout=0.05
- **Batch size:** 1 (efektywny 8 przez gradient accumulation)
- **Max sequence length:** 512
- **Learning rate:** 2e-4
- **Epochs:** 1

### Output

Wytrenowany adapter zostanie zapisany w: `bielik-crossword-qlora-1k/`

## Troubleshooting

### Out of Memory (OOM) Error

Jeśli napotkasz błędy OOM, spróbuj:

1. **Zmniejsz `max_seq_length`:**
   - W pliku `train_crossword_qlora_1k.py` zmień `max_seq_length=512` na `384` lub `256`

2. **Zwiększ `gradient_accumulation_steps`:**
   - Zmień `gradient_accumulation_steps=8` na `16` lub `32`

3. **Sprawdź użycie VRAM:**
   ```bash
   nvidia-smi
   ```

### Problemy z bitsandbytes na Windows

Jeśli `bitsandbytes` nie działa na Windows, możesz spróbować:

1. Zainstalować pre-compiled wheel:
   ```bash
   pip install bitsandbytes-windows
   ```

2. Lub użyć WSL2 (Windows Subsystem for Linux)

## Testowanie modelu

Po zakończeniu treningu, przetestuj model:

```bash
python test_crossword_qlora.py
```

Skrypt załaduje base model + adapter i wygeneruje przykładową krzyżówkę. Oczekiwany output powinien zawierać sekcję `# GRID` z formatem CrossGrid.

### Oczekiwany format outputu

```
Ułóż polską krzyżówkę jako CrossGrid.
...
Zwróć tylko sekcję # GRID.
# GRID
R0: ..... ..... .....
R1: ..... .E.C. .....
...
```

## Następne kroki

Po udanym treningu na 1k datasetcie:

1. Wygeneruj większy dataset (5k+ rekordów)
2. Dostosuj parametry treningu (więcej epok, learning rate scheduling)
3. Eksperymentuj z różnymi konfiguracjami LoRA (r, alpha)

## Struktura plików

```
PythonDatasetsScripts/bielik/
├── train_crossword_qlora_1k.py    # Skrypt treningowy
├── test_crossword_qlora.py        # Skrypt testowy
├── requirements.txt               # Zależności Python
├── README.md                      # Ten plik
└── bielik-crossword-qlora-1k/     # Output (po treningu)
    ├── adapter_config.json
    ├── adapter_model.bin
    └── tokenizer files...
```

## Wsparcie

Jeśli napotkasz problemy:
1. Sprawdź logi treningu
2. Zweryfikuj format datasetu (pierwsze kilka linii JSONL)
3. Upewnij się, że masz wystarczająco VRAM (minimum 6GB)

