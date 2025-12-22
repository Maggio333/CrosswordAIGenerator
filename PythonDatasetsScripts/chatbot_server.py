"""
Chatbot server dla modeli Bielik 4.5B v3 i Qwen3 1.7B
Obsługuje dwa tryby:
- General: GGUF przez llama-cpp-python (tylko Bielik)
- Crossword: Transformers + QLoRA adapter (Bielik lub Qwen)
"""

import os
import torch
from pathlib import Path
from flask import Flask, request, jsonify, Response, stream_with_context
from flask_cors import CORS
from typing import Optional, Dict
import json
import time
from datetime import datetime
from transformers import TextIteratorStreamer
from threading import Thread

# Ścieżki względne - bazujemy na lokalizacji tego skryptu
SCRIPT_DIR = Path(__file__).parent.resolve()
PROJECT_ROOT = SCRIPT_DIR.parent  # CrosswordAIGenerator/

# Konfiguracja modeli
GGUF_MODEL_PATH = PROJECT_ROOT / "PythonDatasetsScripts" / "speakleash" / "Bielik-4.5B-v3.0-Instruct-GGUF" / "Bielik-4.5B-v3.0-Instruct.Q8_0.gguf"

# Konfiguracja adapterów dla różnych modeli
BIELIK_ADAPTER_DIR = PROJECT_ROOT / "PythonDatasetsScripts" / "bielik" / "bielik-crossword-qlora-5k"
QWEN_ADAPTER_DIR = PROJECT_ROOT / "PythonDatasetsScripts" / "qwen3" / "qwen3-crossword-qlora-5k"

# Model base dla Bielika - sprawdź lokalny lub HuggingFace
BIELIK_LOCAL_MODEL_DIR = PROJECT_ROOT / "PythonDatasetsScripts" / "speakleash" / "Bielik-4.5B-v3.0-Instruct"
if BIELIK_LOCAL_MODEL_DIR.exists() and (BIELIK_LOCAL_MODEL_DIR / "config.json").exists():
    BIELIK_BASE_MODEL = str(BIELIK_LOCAL_MODEL_DIR)
    BIELIK_USE_HF_TOKEN = False
    print(f"✅ Używam lokalnego modelu Bielik Transformers: {BIELIK_BASE_MODEL}")
else:
    BIELIK_BASE_MODEL = "speakleash/Bielik-4.5B-v3.0-Instruct"
    BIELIK_USE_HF_TOKEN = True
    print(f"⚠️  Używam modelu Bielik z HuggingFace: {BIELIK_BASE_MODEL}")

# Model base dla Qwena
QWEN_LOCAL_MODEL_DIR = PROJECT_ROOT / "PythonDatasetsScripts" / "qw" / "Qwen3-1.7B-Base"
if QWEN_LOCAL_MODEL_DIR.exists() and (QWEN_LOCAL_MODEL_DIR / "config.json").exists():
    QWEN_BASE_MODEL = str(QWEN_LOCAL_MODEL_DIR)
    print(f"✅ Używam lokalnego modelu Qwen Transformers: {QWEN_BASE_MODEL}")
else:
    QWEN_BASE_MODEL = "Qwen/Qwen3-1.7B-Base"
    print(f"⚠️  Używam modelu Qwen z HuggingFace: {QWEN_BASE_MODEL}")

app = Flask(__name__)
CORS(app)  # Włącz CORS dla komunikacji z C#

# Globalne zmienne dla modeli (lazy loading)
gguf_model = None
gguf_llm = None

# Osobne zmienne dla każdego adaptera - można mieć oba załadowane jednocześnie
bielik_model = None
bielik_tokenizer = None
qwen_model = None
qwen_tokenizer = None


def load_gguf_model():
    """Ładuje model GGUF przez llama-cpp-python"""
    global gguf_llm
    
    if gguf_llm is not None:
        return gguf_llm
    
    try:
        from llama_cpp import Llama
        
        if not GGUF_MODEL_PATH.exists():
            raise FileNotFoundError(f"Nie znaleziono modelu GGUF: {GGUF_MODEL_PATH}")
        
        print(f"Ładowanie modelu GGUF: {GGUF_MODEL_PATH}")
        print(f"Pełna ścieżka: {GGUF_MODEL_PATH.resolve()}")
        print(f"Plik istnieje: {GGUF_MODEL_PATH.exists()}")
        
        gguf_llm = Llama(
            model_path=str(GGUF_MODEL_PATH),
            n_ctx=2048,  # Kontekst
            n_threads=4,  # Liczba wątków
            verbose=True  # Włącz verbose aby zobaczyć szczegóły ładowania
        )
        
        # Sprawdź metadane modelu
        print(f"✅ Model GGUF załadowany")
        print(f"Model info: {gguf_llm}")
        return gguf_llm
    except ImportError:
        raise ImportError("llama-cpp-python nie jest zainstalowany. Zainstaluj: pip install llama-cpp-python")
    except Exception as e:
        raise RuntimeError(f"Błąd podczas ładowania modelu GGUF: {e}")


def load_qlora_model(model_name: str = "bielik"):
    """Ładuje model Transformers + QLoRA adapter
    
    Args:
        model_name: "bielik" lub "qwen"
    
    Returns:
        (model, tokenizer) tuple
    """
    global bielik_model, bielik_tokenizer, qwen_model, qwen_tokenizer
    
    # Sprawdź czy model jest już załadowany
    if model_name == "bielik":
        if bielik_model is not None and bielik_tokenizer is not None:
            print(f"ℹ️  Model Bielik jest już załadowany, używam istniejącego")
            return bielik_model, bielik_tokenizer
    else:  # qwen
        if qwen_model is not None and qwen_tokenizer is not None:
            print(f"ℹ️  Model Qwen jest już załadowany, używam istniejącego")
            return qwen_model, qwen_tokenizer
    
    try:
        from transformers import AutoTokenizer, AutoModelForCausalLM, BitsAndBytesConfig
        from peft import PeftModel
        from huggingface_hub import HfFolder
        
        # Wybierz konfigurację w zależności od modelu
        if model_name == "qwen":
            BASE_MODEL = QWEN_BASE_MODEL
            ADAPTER_DIR = QWEN_ADAPTER_DIR
            USE_HF_TOKEN = False  # Qwen jest publiczny
            print(f"📦 Ładowanie modelu Qwen3-1.7B")
        else:  # bielik
            BASE_MODEL = BIELIK_BASE_MODEL
            ADAPTER_DIR = BIELIK_ADAPTER_DIR
            USE_HF_TOKEN = BIELIK_USE_HF_TOKEN
            print(f"📦 Ładowanie modelu Bielik 4.5B")
        
        if not ADAPTER_DIR.exists():
            raise FileNotFoundError(
                f"Nie znaleziono adaptera: {ADAPTER_DIR}\n"
                f"Uruchom najpierw train_crossword_qlora_5k.py"
            )
        
        print(f"Ładowanie modelu QLoRA: {BASE_MODEL} + {ADAPTER_DIR}")
        print(f"Pełna ścieżka adaptera: {ADAPTER_DIR.resolve()}")
        print(f"Adapter istnieje: {ADAPTER_DIR.exists()}")
        
        # Token HuggingFace jeśli potrzebny
        hf_token = None
        if USE_HF_TOKEN:
            hf_token = os.getenv("HF_TOKEN") or os.getenv("HUGGINGFACE_TOKEN") or HfFolder.get_token()
            if not hf_token:
                raise RuntimeError(
                    "Model wymaga tokenu HuggingFace (model jest 'gated').\n"
                    "Ustaw zmienną środowiskową HF_TOKEN lub uruchom: huggingface-cli login"
                )
            print(f"✅ Token HuggingFace znaleziony")
        else:
            print(f"✅ Używam lokalnego modelu, token nie jest potrzebny")
        
        # Konfiguracja 4-bit quantization - optymalizacja dla 4GB VRAM
        print("Konfiguracja 4-bit quantization (optymalizacja dla 4GB VRAM)...")
        compute_dtype = torch.float16
        bnb_config = BitsAndBytesConfig(
            load_in_4bit=True,
            bnb_4bit_use_double_quant=True,
            bnb_4bit_quant_type="nf4",
            bnb_4bit_compute_dtype=compute_dtype,
        )
        
        # Sprawdź dostępną VRAM
        if torch.cuda.is_available():
            total_vram = torch.cuda.get_device_properties(0).total_memory / 1024**3
            print(f"Dostępna VRAM: {total_vram:.2f} GB")
            if total_vram < 6:
                print("⚠️  Mało VRAM - używam agresywnej optymalizacji pamięci")
        
        # Tokenizer
        print(f"Ładowanie tokenizera z: {BASE_MODEL}")
        if model_name == "qwen":
            loaded_tokenizer = AutoTokenizer.from_pretrained(BASE_MODEL, trust_remote_code=False)
        else:
            loaded_tokenizer = AutoTokenizer.from_pretrained(BASE_MODEL, token=hf_token)
        if loaded_tokenizer.pad_token is None:
            loaded_tokenizer.pad_token = loaded_tokenizer.eos_token
        print("✅ Tokenizer załadowany")
        
        # Base model
        print(f"Ładowanie base modelu z: {BASE_MODEL}")
        model_size = "~3.5GB" if model_name == "qwen" else "~9GB"
        print(f"(To może zająć kilka minut - model ma {model_size})...")
        
        # Sprawdź użycie pamięci przed ładowaniem
        if torch.cuda.is_available():
            print(f"VRAM przed ładowaniem: {torch.cuda.memory_allocated() / 1024**3:.2f} GB / {torch.cuda.get_device_properties(0).total_memory / 1024**3:.2f} GB")
        
        print("Ładowanie checkpoint shards...")
        print("(Jeśli proces się zawiesza, może brakować pamięci VRAM)")
        
        try:
            # Optymalizacja dla małej VRAM (4GB)
            # Użyj max_memory aby ograniczyć użycie VRAM
            max_memory = {0: "3GB"}  # Zostaw 1GB na system
            
            load_kwargs = {
                "quantization_config": bnb_config,
                "device_map": "auto",
                "max_memory": max_memory,
                "low_cpu_mem_usage": True,
                "torch_dtype": torch.float16,
            }
            
            if model_name == "qwen":
                load_kwargs["trust_remote_code"] = False
            else:
                load_kwargs["token"] = hf_token
            
            base_model = AutoModelForCausalLM.from_pretrained(
                BASE_MODEL,
                **load_kwargs
            )
            print("✅ Base model załadowany")
            
            if torch.cuda.is_available():
                print(f"VRAM po załadowaniu: {torch.cuda.memory_allocated() / 1024**3:.2f} GB / {torch.cuda.get_device_properties(0).total_memory / 1024**3:.2f} GB")
        except Exception as e:
            print(f"❌ Błąd podczas ładowania base modelu: {e}")
            print("\nMożliwe przyczyny:")
            print("- Za mało pamięci VRAM (wymagane minimum ~6GB)")
            print("- Problem z bitsandbytes (sprawdź czy działa na Windows)")
            print("- Model jest uszkodzony")
            raise
        
        # QLoRA adapter
        print(f"Ładowanie adaptera QLoRA z: {ADAPTER_DIR}")
        try:
            loaded_model = PeftModel.from_pretrained(base_model, str(ADAPTER_DIR))
            loaded_model.eval()
            
            # Optymalizacja: upewnij się że model jest na GPU (jeśli dostępne)
            if torch.cuda.is_available():
                # Sprawdź czy model jest już na GPU
                device = next(loaded_model.parameters()).device
                if device.type != 'cuda':
                    print(f"⚠️  Model jest na {device}, przenoszenie na GPU...")
                    loaded_model = loaded_model.to('cuda')
                else:
                    print(f"✅ Model jest na GPU ({device})")
            
            print("✅ Adapter QLoRA załadowany")
            
            # Optymalizacja: torch.compile dla szybszego generowania (PyTorch 2.0+)
            # UWAGA: torch.compile może mieć problemy z niektórymi modelami PEFT
            # Więc robimy to opcjonalnie
            try:
                if hasattr(torch, 'compile') and torch.cuda.is_available():
                    print("⚡ Próba kompilacji modelu z torch.compile...")
                    # Użyj trybu "reduce-overhead" dla lepszej kompatybilności
                    # fullgraph=False pozwala na częściową kompilację
                    compiled_model = torch.compile(loaded_model, mode="reduce-overhead", fullgraph=False)
                    # Przetestuj kompilację na krótkim promptcie
                    test_inputs = tokenizer("test", return_tensors="pt").to(loaded_model.device)
                    with torch.inference_mode():
                        _ = compiled_model.generate(**test_inputs, max_new_tokens=1)
                    loaded_model = compiled_model
                    print("✅ Model skompilowany - generowanie będzie szybsze (~20-30%)")
            except Exception as compile_error:
                print(f"ℹ️  Pomijam kompilację (może być starsza wersja PyTorch lub problem z PEFT): {compile_error}")
                # Kontynuuj bez kompilacji - model i tak będzie działał
            
        except Exception as e:
            print(f"❌ Błąd podczas ładowania adaptera: {e}")
            print(f"\nSprawdź czy adapter istnieje w: {ADAPTER_DIR}")
            raise
        
        # Zapisz do odpowiedniej zmiennej globalnej
        if model_name == "bielik":
            bielik_model = loaded_model
            bielik_tokenizer = loaded_tokenizer
        else:  # qwen
            qwen_model = loaded_model
            qwen_tokenizer = loaded_tokenizer
        
        print(f"✅ Model QLoRA załadowany (base + adapter) - {model_name.upper()}")
        return loaded_model, loaded_tokenizer
    except Exception as e:
        raise RuntimeError(f"Błąd podczas ładowania modelu QLoRA ({model_name}): {e}")


def generate_general(prompt: str, max_tokens: int = 512, temperature: float = 0.7) -> str:
    """Generuje odpowiedź w trybie ogólnym (GGUF)"""
    llm = load_gguf_model()
    
    # Formatowanie promptu dla modelu Bielik 4.5B v3
    # Model Bielik używa formatu ChatML z tokenami <|im_start|> i <|im_end|>
    # NIE dodajemy system promptu - używamy tylko promptu użytkownika
    formatted_prompt = f"<|im_start|>user\n{prompt}<|im_end|>\n<|im_start|>assistant\n"
    
    print(f"DEBUG: Używam lokalnego modelu GGUF: {GGUF_MODEL_PATH}")
    print(f"DEBUG: Formatowany prompt (pierwsze 200 znaków): {formatted_prompt[:200]}")
    
    try:
        response = llm(
            formatted_prompt,
            max_tokens=max_tokens,
            temperature=temperature,
            stop=["<|im_end|>", "<|im_start|>", "<|eot_id|>"],
            echo=False
        )
        
        if response and "choices" in response and len(response["choices"]) > 0:
            text = response["choices"][0]["text"].strip()
            # Usuń ewentualne pozostałe tokeny specjalne
            text = text.replace("<|im_end|>", "").replace("<|im_start|>", "").replace("<|eot_id|>", "").strip()
            return text
        else:
            return "Błąd: Pusta odpowiedź z modelu"
    except Exception as e:
        raise RuntimeError(f"Błąd podczas generowania (GGUF): {e}")


def generate_crossword(prompt: str, max_tokens: int = 512, temperature: float = 0.5, stream: bool = False, model_name: str = "bielik"):
    """Generuje odpowiedź w trybie krzyżówek (QLoRA)
    
    Args:
        model_name: "bielik" lub "qwen"
    """
    start_time = time.time()
    print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 🔄 Rozpoczynam generowanie odpowiedzi (tryb: Crossword, model: {model_name})")
    print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 📝 Prompt długość: {len(prompt)} znaków")
    print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ⚙️  Parametry: max_tokens={max_tokens}, temperature={temperature}, stream={stream}")
    
    # Załaduj model (lub użyj już załadowanego)
    model, tokenizer = load_qlora_model(model_name)
    
    inputs = tokenizer(prompt, return_tensors="pt").to(model.device)
    print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ✅ Tokenizacja zakończona, input_ids shape: {inputs['input_ids'].shape}")
    
    if stream:
        # Prawdziwy streaming z TextIteratorStreamer
        def generate_stream():
            accumulated_text = ""
            chunk_count = 0
            
            # Utwórz streamer
            streamer = TextIteratorStreamer(
                tokenizer, 
                skip_prompt=True, 
                skip_special_tokens=True,
                timeout=300.0  # 5 minut timeout
            )
            
            # Parametry generowania - optymalizowane dla szybkości
            generation_kwargs = {
                **inputs,
                "max_new_tokens": max_tokens,
                "temperature": temperature,
                "do_sample": True,
                "pad_token_id": tokenizer.eos_token_id,
                "streamer": streamer,
                "use_cache": True,  # Cache attention - przyspiesza generowanie
                "top_p": 0.95,  # Nucleus sampling - lepsza jakość przy podobnej szybkości
                "repetition_penalty": 1.1,  # Zmniejsza powtórzenia
            }
            
            # Funkcja generująca w osobnym wątku
            # Użyj inference_mode() dla lepszej wydajności niż no_grad()
            def generate_with_no_grad():
                with torch.inference_mode():  # Szybsze niż no_grad() dla inference
                    model.generate(**generation_kwargs)
            
            # Uruchom generowanie w osobnym wątku
            generation_thread = Thread(
                target=generate_with_no_grad,
                daemon=True
            )
            generation_thread.start()
            
            print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 🔄 Rozpoczynam streaming tokenów...")
            
            # Czytaj tokeny ze streamera i wysyłaj je jako chunki
            try:
                for new_text in streamer:
                    if new_text:
                        chunk_count += 1
                        accumulated_text += new_text
                        
                        # Wyślij chunk jako SSE
                        yield f"data: {json.dumps({'chunk': new_text})}\n\n"
                        
                        # Loguj co 50 tokenów
                        if chunk_count % 50 == 0:
                            print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 📦 Wysłano {chunk_count} chunków, długość: {len(accumulated_text)} znaków")
                
                # Czekaj na zakończenie wątku generowania
                generation_thread.join(timeout=5.0)
                
                elapsed_time = time.time() - start_time
                has_grid = "#GRID" in accumulated_text.upper() or "# GRID" in accumulated_text.upper()
                
                print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ✅ Streaming zakończony pomyślnie!")
                print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 📊 Statystyki:")
                print(f"   - Długość odpowiedzi: {len(accumulated_text)} znaków")
                print(f"   - Liczba chunków: {chunk_count}")
                print(f"   - Czas generowania: {elapsed_time:.2f} sekund")
                print(f"   - Zawiera Grid: {'✅ TAK' if has_grid else '❌ NIE'}")
                if has_grid:
                    grid_lines = [line for line in accumulated_text.split('\n') if 'R' in line and ':' in line]
                    print(f"   - Liczba linii Grid: {len(grid_lines)}")
                print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 🎉 Streaming zakończony")
                
            except Exception as e:
                print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ❌ Błąd podczas streamingu: {e}")
                raise
            
            # Wyślij sygnał zakończenia
            yield f"data: {json.dumps({'done': True})}\n\n"
        
        return generate_stream()
    else:
        # Normal generation - optymalizowane dla szybkości
        with torch.inference_mode():  # Szybsze niż no_grad() dla inference
            outputs = model.generate(
                **inputs,
                max_new_tokens=max_tokens,
                temperature=temperature,
                do_sample=True,
                pad_token_id=tokenizer.eos_token_id,
                use_cache=True,  # Cache attention - przyspiesza generowanie
                top_p=0.95,  # Nucleus sampling
                repetition_penalty=1.1,  # Zmniejsza powtórzenia
            )
        
        # Dekoduj tylko nowo wygenerowane tokeny (bez promptu)
        generated_text = tokenizer.decode(outputs[0][inputs["input_ids"].shape[1]:], skip_special_tokens=True)
        elapsed_time = time.time() - start_time
        
        # Sprawdź czy odpowiedź zawiera Grid
        has_grid = "#GRID" in generated_text.upper() or "# GRID" in generated_text.upper()
        
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ✅ Generowanie zakończone pomyślnie!")
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 📊 Statystyki:")
        print(f"   - Długość odpowiedzi: {len(generated_text)} znaków")
        print(f"   - Czas generowania: {elapsed_time:.2f} sekund")
        print(f"   - Zawiera Grid: {'✅ TAK' if has_grid else '❌ NIE'}")
        if has_grid:
            grid_lines = [line for line in generated_text.split('\n') if 'R' in line and ':' in line]
            print(f"   - Liczba linii Grid: {len(grid_lines)}")
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 🎉 Odpowiedź gotowa do wysłania")
        
        return generated_text.strip()


@app.route("/health", methods=["GET"])
def health():
    """Endpoint sprawdzający status serwera"""
    return jsonify({"status": "ok", "message": "Chatbot server is running"})


@app.route("/chat", methods=["POST"])
def chat():
    """Główny endpoint do generowania odpowiedzi"""
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({"error": "Brak danych JSON"}), 400
        
        prompt = data.get("prompt")
        mode = data.get("mode", "general")  # "general" lub "crossword"
        model_name = data.get("model", "bielik")  # "bielik" lub "qwen" (tylko dla crossword)
        max_tokens = data.get("max_tokens", 512)
        temperature = data.get("temperature", 0.7 if mode == "general" else 0.5)
        stream = data.get("stream", False)  # Czy streamować odpowiedź
        
        if not prompt:
            return jsonify({"error": "Brak promptu"}), 400
        
        if mode not in ["general", "crossword"]:
            return jsonify({"error": "Nieprawidłowy tryb. Użyj 'general' lub 'crossword'"}), 400
        
        # Jeśli streaming jest włączony i tryb to crossword
        if stream and mode == "crossword":
            print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 📡 Rozpoczynam streaming odpowiedzi (tryb: Crossword, model: {model_name})")
            return Response(
                stream_with_context(generate_crossword(prompt, max_tokens, temperature, stream=True, model_name=model_name)),
                mimetype="text/event-stream",
                headers={
                    "Cache-Control": "no-cache",
                    "Connection": "keep-alive",
                    "X-Accel-Buffering": "no"  # Wyłącz buffering w nginx
                }
            )
        
        # Generuj odpowiedź w zależności od trybu (bez streamingu)
        request_start_time = time.time()
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 📨 Otrzymano żądanie /chat")
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 📋 Tryb: {mode}, Prompt długość: {len(prompt)} znaków")
        
        if mode == "general":
            response_text = generate_general(prompt, max_tokens, temperature)
        else:  # crossword
            response_text = generate_crossword(prompt, max_tokens, temperature, stream=False, model_name=model_name)
        
        request_elapsed = time.time() - request_start_time
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ✅ Odpowiedź wygenerowana i wysłana do klienta")
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ⏱️  Całkowity czas przetwarzania żądania: {request_elapsed:.2f} sekund")
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] " + "="*60)
        
        return jsonify({
            "response": response_text,
            "mode": mode
        })
    
    except FileNotFoundError as e:
        return jsonify({"error": f"Nie znaleziono pliku: {str(e)}"}), 404
    except ImportError as e:
        return jsonify({"error": f"Brak wymaganej biblioteki: {str(e)}"}), 500
    except RuntimeError as e:
        return jsonify({"error": f"Błąd modelu: {str(e)}"}), 500
    except Exception as e:
        return jsonify({"error": f"Błąd serwera: {str(e)}"}), 500


@app.route("/models/status", methods=["GET"])
def models_status():
    """Endpoint sprawdzający status załadowanych modeli"""
    # Sprawdź użycie VRAM
    vram_info = {}
    if torch.cuda.is_available():
        vram_allocated = torch.cuda.memory_allocated() / 1024**3
        vram_total = torch.cuda.get_device_properties(0).total_memory / 1024**3
        vram_info = {
            "allocated_gb": round(vram_allocated, 2),
            "total_gb": round(vram_total, 2),
            "free_gb": round(vram_total - vram_allocated, 2),
            "usage_percent": round((vram_allocated / vram_total) * 100, 1)
        }
    
    status = {
        "gguf_loaded": gguf_llm is not None,
        "crossword_adapters": {
            "bielik": {
                "loaded": bielik_model is not None and bielik_tokenizer is not None,
                "model": "Bielik 4.5B v3",
                "adapter_path": str(BIELIK_ADAPTER_DIR)
            },
            "qwen": {
                "loaded": qwen_model is not None and qwen_tokenizer is not None,
                "model": "Qwen3 1.7B",
                "adapter_path": str(QWEN_ADAPTER_DIR)
            }
        },
        "vram": vram_info
    }
    return jsonify(status)


@app.route("/models/load-crossword", methods=["POST"])
def load_crossword_adapter():
    """Endpoint do ręcznego załadowania adaptera Crossword (QLoRA)"""
    global bielik_model, bielik_tokenizer, qwen_model, qwen_tokenizer
    
    load_start_time = time.time()
    print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 📥 Otrzymano żądanie załadowania adaptera Crossword")
    
    try:
        # Pobierz wybór modelu z requestu
        data = request.get_json() or {}
        model_name = data.get("model", "bielik")  # domyślnie Bielik
        
        if model_name not in ["bielik", "qwen"]:
            return jsonify({
                "status": "error",
                "error": f"Nieznany model: {model_name}. Użyj 'bielik' lub 'qwen'"
            }), 400
        
        # Sprawdź czy już załadowany
        if model_name == "bielik":
            if bielik_model is not None and bielik_tokenizer is not None:
                print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ℹ️  Adapter QLoRA (Bielik) jest już załadowany")
                return jsonify({
                    "status": "already_loaded",
                    "message": "Adapter QLoRA (Bielik 4.5B) jest już załadowany"
                })
        else:  # qwen
            if qwen_model is not None and qwen_tokenizer is not None:
                print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ℹ️  Adapter QLoRA (Qwen) jest już załadowany")
                return jsonify({
                    "status": "already_loaded",
                    "message": "Adapter QLoRA (Qwen3 1.7B) jest już załadowany"
                })
        
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 🔄 Rozpoczynam ładowanie adaptera QLoRA ({model_name})...")
        # Załaduj adapter (nie zwalnia innych)
        load_qlora_model(model_name)
        
        load_elapsed = time.time() - load_start_time
        model_display = "Bielik 4.5B" if model_name == "bielik" else "Qwen3 1.7B"
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ✅ Adapter QLoRA ({model_display}) został załadowany pomyślnie!")
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ⏱️  Czas ładowania: {load_elapsed:.2f} sekund")
        
        # Sprawdź stan wszystkich adapterów
        bielik_loaded = bielik_model is not None
        qwen_loaded = qwen_model is not None
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 📊 Stan adapterów: Bielik={'✅' if bielik_loaded else '❌'}, Qwen={'✅' if qwen_loaded else '❌'}")
        
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 🎉 Adapter gotowy do użycia")
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] " + "="*60)
        
        return jsonify({
            "status": "loaded",
            "message": f"Adapter QLoRA ({model_display}) został załadowany pomyślnie",
            "model": model_name,
            "load_time_seconds": round(load_elapsed, 2)
        })
    except Exception as e:
        load_elapsed = time.time() - load_start_time
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ❌ Błąd podczas ładowania adaptera: {str(e)}")
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ⏱️  Czas przed błędem: {load_elapsed:.2f} sekund")
        return jsonify({
            "status": "error",
            "error": str(e)
        }), 500


@app.route("/models/unload-crossword", methods=["POST"])
def unload_crossword_adapter():
    """Endpoint do zwalniania adaptera Crossword (QLoRA)"""
    global bielik_model, bielik_tokenizer, qwen_model, qwen_tokenizer
    
    print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 📥 Otrzymano żądanie zwolnienia adaptera Crossword")
    
    try:
        # Pobierz wybór modelu z requestu
        data = request.get_json() or {}
        model_name = data.get("model")  # "bielik", "qwen" lub None (wszystkie)
        
        unloaded = []
        
        if model_name is None or model_name == "bielik":
            if bielik_model is not None:
                print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 🔄 Zwalnianie adaptera Bielik...")
                del bielik_model
                bielik_model = None
                del bielik_tokenizer
                bielik_tokenizer = None
                unloaded.append("bielik")
                print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ✅ Adapter Bielik zwolniony")
        
        if model_name is None or model_name == "qwen":
            if qwen_model is not None:
                print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 🔄 Zwalnianie adaptera Qwen...")
                del qwen_model
                qwen_model = None
                del qwen_tokenizer
                qwen_tokenizer = None
                unloaded.append("qwen")
                print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ✅ Adapter Qwen zwolniony")
        
        # Zwolnij pamięć GPU
        if torch.cuda.is_available():
            torch.cuda.empty_cache()
            vram_after = torch.cuda.memory_allocated() / 1024**3
            print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 💾 VRAM po zwolnieniu: {vram_after:.2f} GB")
        
        if not unloaded:
            return jsonify({
                "status": "not_loaded",
                "message": f"Adapter ({model_name or 'wszystkie'}) nie był załadowany"
            })
        
        message = f"Zwolniono adapter(y): {', '.join(unloaded)}"
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] 🎉 {message}")
        
        return jsonify({
            "status": "unloaded",
            "message": message,
            "unloaded_models": unloaded
        })
    except Exception as e:
        print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ❌ Błąd podczas zwalniania adaptera: {str(e)}")
        return jsonify({
            "status": "error",
            "error": str(e)
        }), 500


if __name__ == "__main__":
    print("=" * 60)
    print("Chatbot Server - Bielik 4.5B v3 & Qwen3 1.7B")
    print("=" * 60)
    print(f"GGUF Model (Bielik General): {GGUF_MODEL_PATH}")
    print(f"Bielik QLoRA Adapter: {BIELIK_ADAPTER_DIR}")
    print(f"Qwen QLoRA Adapter: {QWEN_ADAPTER_DIR}")
    print(f"Bielik Base Model: {BIELIK_BASE_MODEL}")
    print(f"Qwen Base Model: {QWEN_BASE_MODEL}")
    print("=" * 60)
    print("\nSerwer uruchomiony na http://localhost:5000")
    print("Endpoints:")
    print("  GET  /health - status serwera")
    print("  POST /chat - generowanie odpowiedzi (stream: true dla streamingu)")
    print("  GET  /models/status - szczegółowy status modeli i VRAM")
    print("  POST /models/load-crossword - załaduj adapter Crossword")
    print("      (body: {\"model\": \"bielik\" lub \"qwen\"})")
    print("  POST /models/unload-crossword - zwolnij adapter Crossword")
    print("      (body: {\"model\": \"bielik\", \"qwen\" lub null dla wszystkich})")
    print("\nModele są ładowane lazy (tylko gdy potrzebne)")
    print("Można mieć załadowane oba adaptery jednocześnie")
    print("=" * 60)
    
    app.run(host="127.0.0.1", port=5000, debug=False)

