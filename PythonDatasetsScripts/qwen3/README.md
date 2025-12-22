# QLoRA Training dla Qwen3-1.7B - Krzyżówki

Ten folder zawiera skrypty do treningu QLoRA modelu Qwen3-1.7B na datasetcie krzyżówek.

## Wymagania

- Python 3.8+
- NVIDIA GPU z CUDA (RTX 3050 6GB lub lepsza)
- CUDA toolkit (dla bitsandbytes)
- **Model Qwen3-1.7B jest publiczny** - nie wymaga tokenu HuggingFace

## Instalacja

### 0. Pobierz model (opcjonalnie)

Model Qwen3-1.7B-Base będzie automatycznie pobierany z HuggingFace podczas pierwszego uruchomienia treningu. Jeśli chcesz pobrać go wcześniej lokalnie:

```bash
python download_qwen3_transformers.py
```

Model zostanie zapisany w: `../qw/Qwen3-1.7B-Base/`

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
pip install -r ../bielik/requirements.txt
```

Lub zainstaluj bezpośrednio:
```bash
pip install torch>=2.0.0 transformers>=4.35.0 datasets>=2.14.0 trl>=0.7.0 peft>=0.6.0 bitsandbytes>=0.41.0 accelerate>=0.24.0
```

**Uwaga:** Jeśli masz problemy z `bitsandbytes` na Windows, może być potrzebna dodatkowa konfiguracja. Sprawdź [dokumentację bitsandbytes](https://github.com/TimDettmers/bitsandbytes).

## Dataset

Dataset znajduje się w: `../datasets/crossword_finetune_20251126_140211.jsonl`

Format: JSONL z polami `prompt` i `response` (5000 rekordów)

## Walidacja konfiguracji (opcjonalnie)

Przed treningiem możesz zwalidować konfigurację modelu i tokenizera:

```bash
python validate_config.py
```

Skrypt sprawdzi:
- Czy model istnieje i można go załadować
- Czy tokenizer działa poprawnie
- Czy target_modules dla LoRA są dostępne w modelu
- Czy konfiguracja LoRA jest poprawna

## Trening

### Uruchomienie treningu

```bash
python train_crossword_qlora_5k.py
```

### Parametry treningu

- **Model:** `Qwen/Qwen3-1.7B-Base`
- **Quantization:** 4-bit (QLoRA)
- **LoRA:** r=16, alpha=32, dropout=0.05
- **Batch size:** 1 (efektywny 8 przez gradient accumulation)
- **Max sequence length:** 512
- **Learning rate:** 2e-4
- **Epochs:** 2

### Output

Wytrenowany adapter zostanie zapisany w: `qwen3-crossword-qlora-5k/`

## Troubleshooting

### Out of Memory (OOM) Error

Jeśli napotkasz błędy OOM, spróbuj:

1. **Zmniejsz `max_seq_length`:**
   - W pliku `train_crossword_qlora_5k.py` zmień `max_seq_length=512` na `384` lub `256`

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

### Problemy z target_modules dla LoRA

Jeśli trening nie działa z domyślnymi target_modules, sprawdź architekturę modelu:
```python
from transformers import AutoConfig
config = AutoConfig.from_pretrained("Qwen/Qwen3-1.7B-Base")
print(config.architectures)
```

Następnie dostosuj `target_modules` w `train_crossword_qlora_5k.py` zgodnie z rzeczywistą architekturą.

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

## Różnice względem Bielika

- **Model jest mniejszy** (1.7B vs 4.5B), więc trening powinien być szybszy i wymagać mniej VRAM
- **Model jest publiczny** - nie wymaga tokenu HuggingFace ani autoryzacji
- **Architektura Qwen** może wymagać weryfikacji target_modules dla LoRA

## Struktura plików

```
PythonDatasetsScripts/qwen3/
├── train_crossword_qlora_5k.py    # Skrypt treningowy
├── test_crossword_qlora.py        # Skrypt testowy
├── download_qwen3_transformers.py # Skrypt do pobrania modelu
├── validate_config.py            # Skrypt walidacyjny konfiguracji
├── requirements.txt               # Zależności Python
├── README.md                      # Ten plik
└── qwen3-crossword-qlora-5k/     # Output (po treningu)
    ├── adapter_config.json
    ├── adapter_model.safetensors
    └── tokenizer files...
```

## Wsparcie

Jeśli napotkasz problemy:
1. Sprawdź logi treningu
2. Zweryfikuj format datasetu (pierwsze kilka linii JSONL)
3. Upewnij się, że masz wystarczająco VRAM (minimum 6GB)
4. Sprawdź czy model Qwen3-1.7B-Base jest dostępny na HuggingFace

## Linki

- Model Qwen3-1.7B-Base: https://huggingface.co/Qwen/Qwen3-1.7B-Base
- Dokumentacja Qwen: https://github.com/QwenLM/Qwen

