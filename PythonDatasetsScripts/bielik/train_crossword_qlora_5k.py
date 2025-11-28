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
from huggingface_hub import login, HfFolder

# Ścieżki względne - bazujemy na lokalizacji tego skryptu
SCRIPT_DIR = Path(__file__).parent.resolve()
PROJECT_ROOT = SCRIPT_DIR.parent.parent  # CrosswordAIGenerator/

# Dataset - 5k przykładów
DATASET_DIR = PROJECT_ROOT / "datasets"
DATA_FILE = DATASET_DIR / "crossword_finetune_20251126_140211.jsonl"

# Model - można użyć lokalnego lub z HuggingFace
# Sprawdź czy istnieje lokalny model w PythonDatasetsScripts/speakleash/
LOCAL_MODEL_DIR = PROJECT_ROOT / "PythonDatasetsScripts" / "speakleash" / "Bielik-4.5B-v3.0-Instruct"
if LOCAL_MODEL_DIR.exists() and (LOCAL_MODEL_DIR / "config.json").exists():
    MODEL_NAME = str(LOCAL_MODEL_DIR)
    USE_HF_TOKEN = False
    print(f"Używam lokalnego modelu: {MODEL_NAME}")
else:
    MODEL_NAME = "speakleash/Bielik-4.5B-v3.0-Instruct"
    USE_HF_TOKEN = True
    print(f"Używam modelu z HuggingFace: {MODEL_NAME}")
    print("⚠️  Model wymaga autoryzacji (gated repo)")
    
    # Sprawdź czy jest token HuggingFace
    hf_token = os.getenv("HF_TOKEN") or os.getenv("HUGGINGFACE_TOKEN") or HfFolder.get_token()
    
    if not hf_token:
        print("\n❌ Nie znaleziono tokenu HuggingFace!")
        print("\nAby uzyskać dostęp do modelu Bielik:")
        print("1. Zaloguj się na https://huggingface.co/speakleash/Bielik-4.5B-v3.0-Instruct")
        print("2. Poproś o dostęp do modelu (kliknij 'Agree and access repository')")
        print("3. Utwórz token na https://huggingface.co/settings/tokens")
        print("4. Zaloguj się używając jednej z opcji:")
        print("   a) Uruchom: huggingface-cli login")
        print("   b) Ustaw zmienną środowiskową: set HF_TOKEN=twój_token")
        print("\nLub użyj lokalnego modelu w formacie HuggingFace.")
        raise ValueError("Brak tokenu HuggingFace dla gated model")
    else:
        print(f"✅ Znaleziono token HuggingFace")
        # Upewnij się, że token jest ustawiony
        if not HfFolder.get_token():
            login(token=hf_token)

# Output directory
OUTPUT_DIR = SCRIPT_DIR / "bielik-crossword-qlora-5k"


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
    print(f"Dataset zawiera {len(dataset)} przykładów")

    def merge_prompt_response(example):
        # uczymy model: prompt -> response
        # tekst treningowy = prompt + odpowiedź (# GRID...)
        text = example["prompt"].strip() + "\n" + example["response"].strip()
        return {"text": text}

    dataset = dataset.map(merge_prompt_response, remove_columns=dataset.column_names)

    # 2. Tokenizer
    # Użyj tokenu jeśli potrzebny
    token = os.getenv("HF_TOKEN") or os.getenv("HUGGINGFACE_TOKEN") or HfFolder.get_token() if USE_HF_TOKEN else None
    tokenizer = AutoTokenizer.from_pretrained(MODEL_NAME, token=token)
    if tokenizer.pad_token is None:
        tokenizer.pad_token = tokenizer.eos_token
    tokenizer.padding_side = "right"

    # 3. Konfiguracja 4-bit (QLoRA) pod 6 GB VRAM
    compute_dtype = torch.float16

    bnb_config = BitsAndBytesConfig(
        load_in_4bit=True,
        bnb_4bit_use_double_quant=True,
        bnb_4bit_quant_type="nf4",
        bnb_4bit_compute_dtype=compute_dtype,
    )

    # Użyj tokenu jeśli potrzebny
    token = os.getenv("HF_TOKEN") or os.getenv("HUGGINGFACE_TOKEN") or HfFolder.get_token() if USE_HF_TOKEN else None
    model = AutoModelForCausalLM.from_pretrained(
        MODEL_NAME,
        quantization_config=bnb_config,
        device_map="auto",
        token=token,
    )

    # 4. LoRA – delikatne ustawienia
    lora_config = LoraConfig(
        r=16,
        lora_alpha=32,
        lora_dropout=0.05,
        bias="none",
        task_type="CAUSAL_LM",
        target_modules=[
            "q_proj", "k_proj", "v_proj", "o_proj",
            "gate_proj", "up_proj", "down_proj",
        ],
    )

    # 5. Parametry treningu - 2 epoki dla 5k datasetu
    # Dla 5k przykładów: 5000 / 8 (gradient accumulation) = ~625 kroków na epokę
    # 2 epoki = ~1250 kroków
    training_args = SFTConfig(
        output_dir=str(OUTPUT_DIR),
        num_train_epochs=2,                    # 2 epoki dla większego datasetu
        per_device_train_batch_size=1,
        gradient_accumulation_steps=8,         # efektywny batch ~8
        learning_rate=2e-4,
        logging_steps=10,                     # loguj co 10 kroków (szybsze feedback)
        save_steps=250,                        # zapisuj co 250 kroków (więcej checkpointów)
        save_total_limit=3,                    # zachowaj 3 ostatnie checkpointy
        max_seq_length=512,                    # ważne przy 6 GB
        packing=False,
        bf16=False,
        gradient_checkpointing=True,           # mniej VRAM
        report_to=[],                         # wyłącz wandb/tensorboard (szybsze)
    )

    # Formatowanie funkcja dla datasetu (TRL 0.18.1)
    def formatting_func(examples):
        # Dataset już ma pole "text" z połączonym prompt+response
        return examples["text"]
    
    trainer = SFTTrainer(
        model=model,
        args=training_args,
        train_dataset=dataset,
        peft_config=lora_config,
        processing_class=tokenizer,  # W TRL 0.18.1 używa się processing_class zamiast tokenizer
        formatting_func=formatting_func,
    )

    # 6. Ogień
    print(f"\n🚀 Rozpoczynam trening na {len(dataset)} przykładach przez {training_args.num_train_epochs} epok...")
    trainer.train()

    # 7. Zapis adaptera
    OUTPUT_DIR.mkdir(exist_ok=True)
    trainer.model.save_pretrained(str(OUTPUT_DIR))
    tokenizer.save_pretrained(str(OUTPUT_DIR))
    print(f"\n✅ Adapter zapisany w: {OUTPUT_DIR}")


if __name__ == "__main__":
    main()

