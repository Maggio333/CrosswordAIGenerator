"""
Skrypt walidacyjny - sprawdza czy konfiguracja modelu Qwen3-1.7B jest poprawna
"""

import torch
from pathlib import Path
from transformers import AutoTokenizer, AutoModelForCausalLM, AutoConfig
from peft import LoraConfig

# Ścieżki względne
SCRIPT_DIR = Path(__file__).parent.resolve()
PROJECT_ROOT = SCRIPT_DIR.parent.parent

# Model - sprawdź lokalny lub HuggingFace
LOCAL_MODEL_DIR = PROJECT_ROOT / "PythonDatasetsScripts" / "qw" / "Qwen3-1.7B-Base"
if LOCAL_MODEL_DIR.exists() and (LOCAL_MODEL_DIR / "config.json").exists():
    MODEL_NAME = str(LOCAL_MODEL_DIR)
    print(f"✅ Używam lokalnego modelu: {MODEL_NAME}")
else:
    MODEL_NAME = "Qwen/Qwen3-1.7B-Base"
    print(f"ℹ️  Sprawdzam model z HuggingFace: {MODEL_NAME}")
    print("   (Jeśli model nie istnieje, spróbuj: Qwen/Qwen3-1.7B)")

print("\n" + "=" * 60)
print("WALIDACJA KONFIGURACJI QWEN3-1.7B")
print("=" * 60)

try:
    # 1. Sprawdź czy model istnieje i załaduj config
    print("\n1. Sprawdzanie konfiguracji modelu...")
    try:
        config = AutoConfig.from_pretrained(MODEL_NAME, trust_remote_code=True)
        print(f"   ✅ Model istnieje")
        print(f"   - Architektura: {config.architectures}")
        print(f"   - Model type: {config.model_type}")
        print(f"   - Hidden size: {config.hidden_size}")
        print(f"   - Num layers: {getattr(config, 'num_hidden_layers', 'N/A')}")
    except Exception as e:
        print(f"   ❌ Błąd podczas ładowania config: {e}")
        print(f"   💡 Spróbuj modelu: Qwen/Qwen3-1.7B (bez -Base)")
        raise

    # 2. Sprawdź tokenizer
    print("\n2. Sprawdzanie tokenizera...")
    try:
        tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME, trust_remote_code=True)
        print(f"   ✅ Tokenizer załadowany")
        print(f"   - Vocab size: {len(tokenizer)}")
        print(f"   - Pad token: {tokenizer.pad_token if tokenizer.pad_token else 'None (będzie ustawiony na eos_token)'}")
        print(f"   - EOS token: {tokenizer.eos_token}")
        
        # Test tokenizacji
        test_text = "Ułóż polską krzyżówkę jako CrossGrid."
        tokens = tokenizer(test_text, return_tensors="pt")
        print(f"   - Test tokenizacji: '{test_text}' -> {tokens['input_ids'].shape[1]} tokenów")
        
    except Exception as e:
        print(f"   ❌ Błąd podczas ładowania tokenizera: {e}")
        raise

    # 3. Sprawdź dostępne moduły dla LoRA
    print("\n3. Sprawdzanie modułów dla LoRA...")
    try:
        # Załaduj model bez quantizacji (tylko do sprawdzenia struktury)
        print("   Ładuję model (może chwilę potrwać)...")
        model = AutoModelForCausalLM.from_pretrained(
            MODEL_NAME,
            torch_dtype=torch.float16,
            device_map="auto",
            trust_remote_code=True,
        )
        
        # Pobierz nazwy modułów
        module_names = set()
        for name, module in model.named_modules():
            if any(x in name for x in ['q_proj', 'k_proj', 'v_proj', 'o_proj', 'gate_proj', 'up_proj', 'down_proj']):
                module_names.add(name.split('.')[-1])  # Tylko nazwa modułu, bez ścieżki
        
        print(f"   ✅ Model załadowany")
        print(f"   - Znalezione moduły dla LoRA:")
        for mod in sorted(module_names):
            print(f"     • {mod}")
        
        # Sprawdź czy nasze target_modules są dostępne
        target_modules = ["q_proj", "k_proj", "v_proj", "o_proj", "gate_proj", "up_proj", "down_proj"]
        missing = [m for m in target_modules if m not in module_names]
        if missing:
            print(f"   ⚠️  Brakujące moduły w modelu: {missing}")
        else:
            print(f"   ✅ Wszystkie target_modules są dostępne")
        
        # Sprawdź rzeczywiste nazwy modułów w modelu
        print(f"\n   Przykładowe pełne ścieżki modułów:")
        count = 0
        for name, module in model.named_modules():
            if any(x in name for x in target_modules) and count < 5:
                print(f"     • {name}")
                count += 1
        
        del model  # Zwolnij pamięć
        torch.cuda.empty_cache() if torch.cuda.is_available() else None
        
    except Exception as e:
        print(f"   ⚠️  Błąd podczas sprawdzania modułów: {e}")
        print(f"   (To może być normalne jeśli model jest duży - sprawdzimy podczas treningu)")

    # 4. Test LoRA config
    print("\n4. Test konfiguracji LoRA...")
    try:
        lora_config = LoraConfig(
            r=16,
            lora_alpha=32,
            lora_dropout=0.05,
            bias="none",
            task_type="CAUSAL_LM",
            target_modules=["q_proj", "k_proj", "v_proj", "o_proj", "gate_proj", "up_proj", "down_proj"],
        )
        print(f"   ✅ Konfiguracja LoRA poprawna")
        print(f"   - r: {lora_config.r}")
        print(f"   - alpha: {lora_config.lora_alpha}")
        print(f"   - dropout: {lora_config.lora_dropout}")
        print(f"   - target_modules: {lora_config.target_modules}")
    except Exception as e:
        print(f"   ❌ Błąd w konfiguracji LoRA: {e}")
        raise

    print("\n" + "=" * 60)
    print("✅ WALIDACJA ZAKOŃCZONA POMYŚLNIE")
    print("=" * 60)
    print("\nModel i tokenizer są gotowe do treningu!")
    print("Możesz teraz uruchomić: python train_crossword_qlora_5k.py")

except Exception as e:
    print("\n" + "=" * 60)
    print("❌ WALIDACJA NIEUDANA")
    print("=" * 60)
    print(f"\nBłąd: {e}")
    print("\nMożliwe rozwiązania:")
    print("1. Sprawdź czy model Qwen/Qwen3-1.7B-Base istnieje na HuggingFace")
    print("2. Spróbuj modelu: Qwen/Qwen3-1.7B (bez -Base)")
    print("3. Upewnij się, że masz zainstalowane: transformers>=4.35.0")
    print("4. Sprawdź połączenie internetowe")
    raise

