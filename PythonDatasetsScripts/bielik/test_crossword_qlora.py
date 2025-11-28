import torch
import os
from pathlib import Path
from transformers import AutoTokenizer, AutoModelForCausalLM, BitsAndBytesConfig
from peft import PeftModel
from huggingface_hub import HfFolder

# Ścieżki względne - bazujemy na lokalizacji tego skryptu
SCRIPT_DIR = Path(__file__).parent.resolve()
PROJECT_ROOT = SCRIPT_DIR.parent.parent  # CrosswordAIGenerator/

# Model - można użyć lokalnego lub z HuggingFace
LOCAL_MODEL_DIR = PROJECT_ROOT / "PythonDatasetsScripts" / "speakleash" / "Bielik-4.5B-v3.0-Instruct-GGUF"/ "Bielik-4.5B-v3.0-Instruct.Q8_0"
if LOCAL_MODEL_DIR.exists() and (LOCAL_MODEL_DIR / "config.json").exists():
    BASE_MODEL = str(LOCAL_MODEL_DIR)
    USE_HF_TOKEN = False
    print(f"Używam lokalnego modelu: {BASE_MODEL}")
else:
    BASE_MODEL = "speakleash/Bielik-4.5B-v3.0-Instruct"
    USE_HF_TOKEN = True
    print(f"Używam modelu z HuggingFace: {BASE_MODEL}")

ADAPTER_DIR = SCRIPT_DIR / "bielik-crossword-qlora-5k"

if not ADAPTER_DIR.exists():
    raise FileNotFoundError(
        f"Nie znaleziono adaptera: {ADAPTER_DIR}\n"
        f"Uruchom najpierw train_crossword_qlora_1k.py"
    )

# Token HuggingFace jeśli potrzebny
hf_token = os.getenv("HF_TOKEN") or os.getenv("HUGGINGFACE_TOKEN") or HfFolder.get_token() if USE_HF_TOKEN else None

compute_dtype = torch.float16
bnb_config = BitsAndBytesConfig(
    load_in_4bit=True,
    bnb_4bit_use_double_quant=True,
    bnb_4bit_quant_type="nf4",
    bnb_4bit_compute_dtype=compute_dtype,
)

tokenizer = AutoTokenizer.from_pretrained(BASE_MODEL, token=hf_token)
if tokenizer.pad_token is None:
    tokenizer.pad_token = tokenizer.eos_token

base_model = AutoModelForCausalLM.from_pretrained(
    BASE_MODEL,
    quantization_config=bnb_config,
    device_map="auto",
    token=hf_token,
)

model = PeftModel.from_pretrained(base_model, str(ADAPTER_DIR))
model.eval()

##prompt = """Ułóż polską krzyżówkę jako CrossGrid.\nRozmiar: 15x17\nHasło główne: KUROWATE\nSłowa (kierunki w nawiasach) – UŻYJ WSZYSTKICH SŁÓW:\n- ESKALOWANO (Across)\n- SPOROGENEZO (Down)\n- STARASOWAŁ (Down)\n- ZAADRESUJEMY (Across)\n- TAMPONOWAŁY (Across)\n- OKTROJOWANEMU (Down)\n- ROZWIERAKU (Across)\n- ODWRACAJĄCEMU (Across)\nZwróć tylko sekcję # GRID."""
##prompt = """Ułóż polską krzyżówkę jako CrossGrid."""
##prompt = """Ułóż polską krzyżówkę jako CrossGrid.\nRozmiar: 19x16\nHasło główne: WARUJĄC\nSłowa (kierunki w nawiasach) – UŻYJ WSZYSTKICH SŁÓW:\n- ZAWAROWYWAŁABY (Across)\n- SZPICUJĄCYCH (Down)\n- NASNUWAJĄC (Down)\n- NAWYMĄDRZAM (Down)\n- WYTAŃCOWUJE (Down)\n- NIEOCHRANIANIU (Down)\n- ZWĄGROWACIAŁAM (Down)\nZwróć tylko sekcję # GRID."""
##prompt = """Ułóż polską krzyżówkę jako CrossGrid.\nRozmiar: 15x17\nHasło główne: HOMKACH\nSłowa (kierunki w nawiasach) – UŻYJ WSZYSTKICH SŁÓW:\n- WYMOCZKACH (Across)\n- KOSZAROWAŁEM (Down)\n- KWOKTAŁAM (Down)\n- PODHAJCOWAŁAŚ (Down)\n- MIĘKKICH (Down)\n- OCZARACH (Across)\n- COCHANYCH (Down)\nZwróć tylko sekcję # GRID."""
##prompt = """Ułóż polską krzyżówkę jako CrossGrid.\nRozmiar: 18x17\nHasło główne: PALIŁBYM\nSłowa (kierunki w nawiasach) – UŻYJ WSZYSTKICH SŁÓW:\n- NAPRZYWOZIŁYŚMY (Across)\n- PRZYMIESZAŁA (Down)\n- AZOTOWAŁBYM (Down)\n- NADGANIANIAMI (Down)\n- LŻYŁABYM (Down)\n- NASŁUŻYLIBYŚCIE (Down)\n- ODBALASTOWAŁBY (Down)\n- EWAPOROWAŁABYM (Down)\nZwróć tylko sekcję # GRID."""
##prompt = """Ułóż polską krzyżówkę jako CrossGrid.\nRozmiar: 19x18\nHasło główne: TZATZIKI\nSłowa (kierunki w nawiasach) – UŻYJ WSZYSTKICH SŁÓW:\n- TASZCZYCIE (Across)\n- ILUZJONISTYK (Down)\n- NIEPRZYKAZYWANĄ (Down)\n- PIĘCIOZAWOROWA (Down)\n- ROZPINACZACH (Down)\n- TRZMIELINAMI (Down)\n- MINISTRANCKI (Across)\n- MIKROFAZACH (Across)\nZwróć tylko sekcję # GRID"""
prompt = """Ułóż polską krzyżówkę jako CrossGrid.\nRozmiar: 19x16\nHasło główne: WCINAJ\nSłowa (kierunki w nawiasach) – UŻYJ WSZYSTKICH SŁÓW:\n- UNIEWAŻNILIŚMY (Across)\n- NAGNOJENIU (Down)\n- ZADZIWIAJĄ (Down)\n- SKARBNICACH (Down)\n- NIEPODGRYWANEJ (Down)\n- NIEPOZAŁĄCZANE (Across)\nZwróć tylko sekcję # GRID."""

inputs = tokenizer(prompt, return_tensors="pt").to(model.device)

with torch.no_grad():
    out = model.generate(
        **inputs,
        max_new_tokens=512,
        temperature=0.5,    
        do_sample=True,
    )

print(tokenizer.decode(out[0], skip_special_tokens=True))

