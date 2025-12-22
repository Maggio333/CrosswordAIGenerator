"""
Skrypt treningowy QLoRA dla Qwen3-1.7B na datasetcie krzyżówek (5k przykładów)

INSTRUKCJA SZYBKIEGO TESTU:
1. Dla szybkiego sprawdzenia czy wszystko działa:
   - Ustaw QUICK_TEST = True
   - Opcjonalnie: LIMIT_DATASET = 1000 (dla 1k przykładów zamiast 5k)
   - Uruchom - zrobi tylko 500 kroków, bez zapisów

2. Dla pełnego treningu:
   - QUICK_TEST = False
   - LIMIT_DATASET = None (lub ustaw liczbę jeśli chcesz ograniczyć)
   - Uruchom - pełny trening 2 epoki na wszystkich danych

3. Jeśli ETA jest zawyżone na początku - to normalne, ustabilizuje się po ~20-50 krokach
"""
import torch
import os
from pathlib import Path
from datasets import load_dataset
from transformers import (
    AutoTokenizer,
    AutoModelForCausalLM,
    BitsAndBytesConfig,
)
from peft import LoraConfig
from trl import SFTTrainer, SFTConfig
from transformers import TrainerCallback
import time
from datetime import datetime

# Ścieżki względne - bazujemy na lokalizacji tego skryptu
SCRIPT_DIR = Path(__file__).parent.resolve()
PROJECT_ROOT = SCRIPT_DIR.parent.parent  # CrosswordAIGenerator/

# Dataset - 5k przykładów
DATASET_DIR = PROJECT_ROOT / "datasets"
DATA_FILE = DATASET_DIR / "crossword_finetune_20251126_140211.jsonl"

# Model - można użyć lokalnego lub z HuggingFace
# Sprawdź czy istnieje lokalny model w PythonDatasetsScripts/qw/
LOCAL_MODEL_DIR = PROJECT_ROOT / "PythonDatasetsScripts" / "qw" / "Qwen3-1.7B-Base"
if LOCAL_MODEL_DIR.exists() and (LOCAL_MODEL_DIR / "config.json").exists():
    MODEL_NAME = str(LOCAL_MODEL_DIR)
    print(f"Używam lokalnego modelu: {MODEL_NAME}")
else:
    MODEL_NAME = "Qwen/Qwen3-1.7B-Base"
    print(f"Używam modelu z HuggingFace: {MODEL_NAME}")
    print("💡 Aby pobrać model lokalnie, uruchom: python download_qwen3_transformers.py")

# Output directory
OUTPUT_DIR = SCRIPT_DIR / "qwen3-crossword-qlora-5k"

# ============================================================
# OPCJE SZYBKIEGO TESTU / SMOKE TEST
# ============================================================
# Zmień te wartości dla szybkiego sprawdzenia czy wszystko działa

QUICK_TEST = False      # Jeśli True: 1 epoka, max_steps=500, wyłączone zapisy
LIMIT_DATASET = None    # None = użyj wszystkich danych, lub ustaw liczbę (np. 1000 dla 1k przykładów)
                        # Przydatne do szybkiego testu przed pełnym treningiem


class TrainingProgressCallback(TrainerCallback):
    """Callback do logowania postępu treningu"""
    
    def __init__(self):
        self.start_time = None
        self.last_log_time = None
        
    def on_train_begin(self, args, state, control, **kwargs):
        self.start_time = time.time()
        self.last_log_time = time.time()
        print(f"\n{'='*60}")
        print(f"🚀 TRENING ROZPOCZĘTY")
        print(f"{'='*60}")
        print(f"Start: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"Total steps: {state.max_steps if state.max_steps else 'N/A'}")
        print(f"Epochs: {args.num_train_epochs}")
        print(f"{'='*60}\n")
        
    def on_log(self, args, state, control, logs=None, **kwargs):
        if logs is None:
            return
            
        current_time = time.time()
        elapsed = current_time - self.start_time if self.start_time else 0
        
        # Oblicz czas od ostatniego logu
        if self.last_log_time:
            step_time = current_time - self.last_log_time
        else:
            step_time = 0
        self.last_log_time = current_time
        
        # Pobierz metryki
        step = state.global_step if hasattr(state, 'global_step') else 0
        epoch = state.epoch if hasattr(state, 'epoch') else 0
        
        # Loss
        loss = logs.get('loss', 'N/A')
        if isinstance(loss, float):
            loss_str = f"{loss:.4f}"
        else:
            loss_str = str(loss)
        
        # Learning rate
        lr = logs.get('learning_rate', 'N/A')
        if isinstance(lr, float):
            lr_str = f"{lr:.2e}"
        else:
            lr_str = str(lr)
        
        # Oblicz ETA jeśli mamy max_steps
        eta_str = "N/A"
        if state.max_steps and step > 0:
            steps_remaining = state.max_steps - step
            if step_time > 0:
                eta_seconds = steps_remaining * step_time
                eta_hours = eta_seconds / 3600
                eta_str = f"{eta_hours:.1f}h"
        
        # Formatuj czas
        elapsed_hours = elapsed / 3600
        elapsed_str = f"{elapsed_hours:.2f}h" if elapsed_hours >= 1 else f"{elapsed:.0f}s"
        
        # Wyświetl log
        print(f"[Step {step:5d} | Epoch {epoch:.2f}] "
              f"Loss: {loss_str:>10} | "
              f"LR: {lr_str:>10} | "
              f"Time: {elapsed_str:>8} | "
              f"ETA: {eta_str:>8}")
        
        # Dodatkowe metryki jeśli są
        if 'train_runtime' in logs:
            print(f"   Runtime: {logs['train_runtime']:.2f}s")
        if 'train_samples_per_second' in logs:
            print(f"   Samples/s: {logs['train_samples_per_second']:.2f}")
            
    def on_train_end(self, args, state, control, **kwargs):
        total_time = time.time() - self.start_time if self.start_time else 0
        total_hours = total_time / 3600
        
        print(f"\n{'='*60}")
        print(f"✅ TRENING ZAKOŃCZONY")
        print(f"{'='*60}")
        print(f"Koniec: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"Total time: {total_hours:.2f} godzin ({total_time/60:.1f} minut)")
        print(f"Total steps: {state.global_step if hasattr(state, 'global_step') else 'N/A'}")
        print(f"{'='*60}\n")


def main():
    # Sprawdź czy dataset istnieje
    if not DATA_FILE.exists():
        raise FileNotFoundError(
            f"Nie znaleziono datasetu: {DATA_FILE}\n"
            f"Sprawdź czy plik istnieje w: {DATASET_DIR}"
        )
    
    print(f"Ładuję dataset z: {DATA_FILE}")
    
    # 1. Dataset: mamy pola "prompt" i "response"
    dataset = load_dataset("json", data_files=str(DATA_FILE))["train"]
    original_size = len(dataset)
    print(f"Dataset zawiera {original_size} przykładów")
    
    # Ograniczenie datasetu dla szybkiego testu
    if LIMIT_DATASET is not None and LIMIT_DATASET < original_size:
        dataset = dataset.select(range(LIMIT_DATASET))
        print(f"⚠️  OGRANICZONO dataset do {LIMIT_DATASET} przykładów (dla szybkiego testu)")
        print(f"   Pełny dataset: {original_size} przykładów")

    def merge_prompt_response(example):
        # uczymy model: prompt -> response
        # tekst treningowy = prompt + odpowiedź (# GRID...)
        text = example["prompt"].strip() + "\n" + example["response"].strip()
        return {"text": text}

    dataset = dataset.map(merge_prompt_response, remove_columns=dataset.column_names)
    print(f"✅ Dataset przetworzony ({len(dataset)} przykładów)")

    # 2. Tokenizer
    print(f"\n📥 Ładowanie tokenizera z: {MODEL_NAME}")
    try:
        tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME, trust_remote_code=True)
        print("✅ Tokenizer załadowany")
        if tokenizer.pad_token is None:
            tokenizer.pad_token = tokenizer.eos_token
            print(f"   Ustawiono pad_token na eos_token: {tokenizer.eos_token}")
        tokenizer.padding_side = "right"
        print(f"   Vocab size: {len(tokenizer)}")
    except Exception as e:
        print(f"❌ Błąd podczas ładowania tokenizera: {e}")
        raise

    # 3. Sprawdź dostępność CUDA i bitsandbytes
    print(f"\n⚙️  Sprawdzanie środowiska...")
    if torch.cuda.is_available():
        print(f"   ✅ CUDA dostępne: {torch.cuda.get_device_name(0)}")
        print(f"   VRAM przed ładowaniem: {torch.cuda.memory_allocated() / 1024**3:.2f} GB / {torch.cuda.get_device_properties(0).total_memory / 1024**3:.2f} GB")
    else:
        print("   ⚠️  CUDA niedostępne - model będzie ładowany na CPU (będzie bardzo wolno)")
    
    # Sprawdź bitsandbytes
    try:
        import bitsandbytes as bnb
        print(f"   ✅ bitsandbytes dostępne: {bnb.__version__}")
    except ImportError:
        print("   ❌ bitsandbytes nie jest zainstalowane!")
        print("   Zainstaluj: pip install bitsandbytes")
        print("   Na Windows: pip install bitsandbytes-windows")
        raise

    # Konfiguracja 4-bit (QLoRA) pod 6 GB VRAM
    print(f"\n⚙️  Konfiguracja 4-bit quantization (QLoRA)...")
    compute_dtype = torch.float16

    try:
        bnb_config = BitsAndBytesConfig(
            load_in_4bit=True,
            bnb_4bit_use_double_quant=True,
            bnb_4bit_quant_type="nf4",
            bnb_4bit_compute_dtype=compute_dtype,
        )
        print("✅ BitsAndBytesConfig utworzony")
    except Exception as e:
        print(f"❌ Błąd podczas tworzenia BitsAndBytesConfig: {e}")
        print("\nMożliwe przyczyny:")
        print("- bitsandbytes nie działa na Windows")
        print("- Spróbuj: pip install bitsandbytes-windows")
        raise

    # 4. Model
    print(f"\n📥 Ładowanie modelu z: {MODEL_NAME}")
    print("   (To może zająć chwilę, szczególnie przy pierwszym uruchomieniu...)")
    print("   Ładowanie z 4-bit quantization...")
    print("   Proszę czekać...")
    
    try:
        import sys
        sys.stdout.flush()  # Wymuś wypisanie przed długą operacją
        
        # Dodaj timeout protection - jeśli to zajmie więcej niż 5 minut, może być problem
        import time
        start_time = time.time()
        
        # Sprawdź czy trust_remote_code jest potrzebne
        # Dla większości modeli Qwen3 nie jest potrzebne, ale sprawdzimy
        try:
            model = AutoModelForCausalLM.from_pretrained(
                MODEL_NAME,
                quantization_config=bnb_config,
                device_map="auto",
                trust_remote_code=False,  # SPRÓBUJ BEZ - może przyspieszyć
                torch_dtype=torch.float16,
                low_cpu_mem_usage=True,
            )
            print("   ✅ Model załadowany BEZ trust_remote_code (szybsze)")
        except Exception as e:
            if "trust_remote_code" in str(e).lower():
                print("   ⚠️  Wymagane trust_remote_code=True (może spowalniać)")
                model = AutoModelForCausalLM.from_pretrained(
                    MODEL_NAME,
                    quantization_config=bnb_config,
                    device_map="auto",
                    trust_remote_code=True,
                    torch_dtype=torch.float16,
                    low_cpu_mem_usage=True,
                )
            else:
                raise
        
        elapsed = time.time() - start_time
        print(f"✅ Model załadowany (czas: {elapsed:.1f}s)")
        
        # Sprawdź czy quantization działa
        if hasattr(model, 'hf_quantizer') or hasattr(model, 'quantization_config'):
            print("   ✅ 4-bit quantization aktywna")
        else:
            print("   ⚠️  UWAGA: Quantization może nie być aktywna!")
        
        # Sprawdź gdzie jest model (GPU vs CPU)
        if torch.cuda.is_available():
            device_info = {}
            for name, param in model.named_parameters():
                if param.device.type not in device_info:
                    device_info[param.device.type] = 0
                device_info[param.device.type] += param.numel()
            print(f"   Parametry na GPU: {device_info.get('cuda', 0):,}")
            if 'cpu' in device_info:
                print(f"   ⚠️  UWAGA: {device_info['cpu']:,} parametrów na CPU (spowalnia trening!)")
            print(f"   VRAM po załadowaniu: {torch.cuda.memory_allocated() / 1024**3:.2f} GB / {torch.cuda.get_device_properties(0).total_memory / 1024**3:.2f} GB")
    except KeyboardInterrupt:
        print("\n⚠️  Przerwano przez użytkownika")
        raise
    except Exception as e:
        print(f"❌ Błąd podczas ładowania modelu: {e}")
        print(f"\nSzczegóły błędu:")
        import traceback
        traceback.print_exc()
        print("\nMożliwe przyczyny:")
        print("- Za mało pamięci VRAM (wymagane minimum ~4-6GB)")
        print("- Problem z bitsandbytes na Windows")
        print("- Model jest uszkodzony")
        print("\nSpróbuj:")
        print("1. Sprawdź czy bitsandbytes działa: python -c 'import bitsandbytes; print(bitsandbytes.__version__)'")
        print("2. Na Windows spróbuj: pip install bitsandbytes-windows")
        print("3. Sprawdź użycie VRAM: nvidia-smi")
        raise

    # 5. LoRA – delikatne ustawienia
    print(f"\n⚙️  Konfiguracja LoRA...")
    # Qwen używa podobnych modułów jak Bielik
    lora_config = LoraConfig(
        r=8,
        lora_alpha=16,
        lora_dropout=0.05,
        bias="none",
        task_type="CAUSAL_LM",
        target_modules=[
            "q_proj", "k_proj", "v_proj", "o_proj",
            "gate_proj", "up_proj", "down_proj",
        ],
    )
    print("✅ Konfiguracja LoRA gotowa")

    # 6. Parametry treningu - 2 epoki dla 5k datasetu
    # Dla 5k przykładów: 5000 / 8 (gradient accumulation) = ~625 kroków na epokę
    # 2 epoki = ~1250 kroków
    # Zoptymalizowane dla szybkości (Qwen3 wydaje się wolniejszy niż Bielik)
    
    # Oblicz rzeczywiste parametry
    effective_batch_size = 1 * 8  # per_device * gradient_accumulation
    steps_per_epoch = len(dataset) // effective_batch_size
    total_steps = steps_per_epoch * 2 if not QUICK_TEST else 100
    
    # Przygotuj parametry treningu
    training_params = {
        "output_dir": str(OUTPUT_DIR),
        "num_train_epochs": 1 if QUICK_TEST else 2,  # 1 epoka dla szybkiego testu
        "per_device_train_batch_size": 1,
        "gradient_accumulation_steps": 8,         # efektywny batch ~8
        "learning_rate": 2e-4,
        "logging_steps": 10,                     # loguj co 10 kroków (szybsze feedback)
        "max_seq_length": 512,                    # IDENTYCZNE jak w Bieliku (było 256, ale Bielik miał 512)
        "packing": False,
        "bf16": False,
        "gradient_checkpointing": True,           # WŁĄCZONE jak w Bieliku (oszczędza VRAM, może spowolnić ale stabilniejsze)
        "report_to": [],                         # wyłącz wandb/tensorboard (szybsze)
        "dataloader_num_workers": 0,             # 0 dla Windows (unikaj problemów z multiprocessing)
        "eval_strategy": "no",                   # Wyłącz eval (przyspiesza)
        # "optim": "paged_adamw_8bit",          # WYŁĄCZONE - może spowalniać, użyj domyślnego (adamw_torch)
        "dataloader_pin_memory": False,         # Wyłączone dla Windows/kompatybilności
    }
    
    # Konfiguracja dla QUICK_TEST vs pełny trening
    if QUICK_TEST:
        # Smoke test - minimalne zapisy, limit kroków
        training_params["max_steps"] = 500
        training_params["save_strategy"] = "no"  # Wyłącz zapisy dla szybkości
        training_params["save_steps"] = None
        training_params["save_total_limit"] = None
        print("   ⚡ QUICK TEST MODE - minimalne zapisy, max_steps=500")
    else:
        # Pełny trening - normalne zapisy
        training_params["save_steps"] = 250
        training_params["save_total_limit"] = 3
        training_params["save_strategy"] = "steps"
    
    training_args = SFTConfig(**training_params)
    
    print(f"\n📊 KONFIGURACJA TRENINGU:")
    print(f"   {'='*60}")
    print(f"   Dataset: {len(dataset)} przykładów")
    print(f"   Batch size: {training_args.per_device_train_batch_size} × {training_args.gradient_accumulation_steps} = {effective_batch_size} (efektywny)")
    print(f"   Steps per epoch: ~{steps_per_epoch}")
    print(f"   Total steps: {total_steps if QUICK_TEST else steps_per_epoch * training_args.num_train_epochs}")
    print(f"   Epochs: {training_args.num_train_epochs}")
    if QUICK_TEST:
        print(f"   ⚡ QUICK TEST MODE - max_steps={training_args.max_steps}")
    print(f"   {'='*60}")
    print(f"   ⚙️  Parametry treningu (zoptymalizowane do Bielika):")
    print(f"      - max_seq_length: {training_args.max_seq_length} (identyczne jak Bielik)")
    print(f"      - gradient_checkpointing: {training_args.gradient_checkpointing} (identyczne jak Bielik)")
    print(f"      - learning_rate: {training_args.learning_rate}")
    print(f"      - optim: {getattr(training_args, 'optim', 'adamw_torch (domyślny)')}")
    print(f"      - logging_steps: {training_args.logging_steps}")
    print(f"\n   ⚠️  RÓŻNICE vs Bielik (które mogą spowalniać):")
    print(f"      - Vocab size: 151k (Qwen3) vs ~mniejszy (Bielik) - spowalnia tokenizację")
    print(f"      - trust_remote_code: próbujemy bez (może przyspieszyć)")
    print(f"      - Architektura Qwen3 może być mniej zoptymalizowana dla treningu")
    if hasattr(training_args, 'save_steps') and training_args.save_steps:
        print(f"      - save_steps: {training_args.save_steps}")
    print(f"   {'='*60}")
    print(f"   💡 ETA na początku może być zawyżone (pierwsze kroki są wolniejsze)")
    print(f"   💡 Po ~20-50 krokach ETA powinno się ustabilizować")
    print(f"\n   {'='*60}")
    if QUICK_TEST:
        print(f"   ⚡ QUICK TEST MODE AKTYWNY")
        print(f"   - max_steps: {training_params.get('max_steps', 'N/A')}")
        print(f"   - save_strategy: {training_params.get('save_strategy', 'N/A')}")
    else:
        print(f"   💡 Dla szybkiego testu ustaw QUICK_TEST=True na początku pliku")
        if LIMIT_DATASET is not None:
            print(f"   💡 Dataset ograniczony do {LIMIT_DATASET} przykładów")
        else:
            print(f"   💡 Możesz też ustawić LIMIT_DATASET=1000 dla szybkiego testu")
    print(f"   {'='*60}")

    # Formatowanie funkcja dla datasetu (TRL 0.18.1)
    def formatting_func(examples):
        # Dataset już ma pole "text" z połączonym prompt+response
        return examples["text"]
    
    print(f"\n⚙️  Tworzenie trainer'a...")
    trainer = SFTTrainer(
        model=model,
        args=training_args,
        train_dataset=dataset,
        peft_config=lora_config,
        processing_class=tokenizer,  # W TRL 0.18.1 używa się processing_class zamiast tokenizer
        formatting_func=formatting_func,
        callbacks=[TrainingProgressCallback()],  # Dodaj callback do logowania
    )
    print("✅ Trainer utworzony")
    
    # Sprawdź czy LoRA jest aktywna
    if hasattr(trainer.model, 'peft_config'):
        print(f"   ✅ LoRA aktywna: {len(trainer.model.peft_config)} adapter(s)")
        for adapter_name, config in trainer.model.peft_config.items():
            print(f"      - {adapter_name}: r={config.r}, alpha={config.lora_alpha}")
    else:
        print("   ⚠️  UWAGA: LoRA może nie być aktywna!")

    # 7. Ogień
    print(f"\n🚀 Rozpoczynam trening na {len(dataset)} przykładach przez {training_args.num_train_epochs} epok...")
    trainer.train()

    # 8. Zapis adaptera
    OUTPUT_DIR.mkdir(exist_ok=True)
    trainer.model.save_pretrained(str(OUTPUT_DIR))
    tokenizer.save_pretrained(str(OUTPUT_DIR))
    print(f"\n✅ Adapter zapisany w: {OUTPUT_DIR}")


if __name__ == "__main__":
    main()

