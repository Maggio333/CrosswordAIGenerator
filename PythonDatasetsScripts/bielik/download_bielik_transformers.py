"""
Skrypt do pobierania modelu Bielik 4.5B v3 w formacie Transformers (dla QLoRA)
"""

import os
from pathlib import Path
from huggingface_hub import snapshot_download, login, HfFolder

# Ścieżki względne
SCRIPT_DIR = Path(__file__).parent.resolve()
PROJECT_ROOT = SCRIPT_DIR.parent.parent
OUTPUT_DIR = PROJECT_ROOT / "PythonDatasetsScripts" / "speakleash" / "Bielik-4.5B-v3.0-Instruct"

MODEL_ID = "speakleash/Bielik-4.5B-v3.0-Instruct"

def main():
    print("=" * 60)
    print("Pobieranie modelu Bielik 4.5B v3 w formacie Transformers")
    print("=" * 60)
    print(f"Model: {MODEL_ID}")
    print(f"Lokalizacja: {OUTPUT_DIR}")
    print("=" * 60)
    
    # Sprawdź czy model już istnieje
    if OUTPUT_DIR.exists() and (OUTPUT_DIR / "config.json").exists():
        print(f"\n⚠️  Model już istnieje w: {OUTPUT_DIR}")
        response = input("Czy chcesz pobrać ponownie? (t/n): ")
        if response.lower() != 't':
            print("Anulowano.")
            return
    
    # Sprawdź token HuggingFace (model jest "gated")
    hf_token = os.getenv("HF_TOKEN") or os.getenv("HUGGINGFACE_TOKEN") or HfFolder.get_token()
    
    if not hf_token:
        print("\n❌ Nie znaleziono tokenu HuggingFace!")
        print("\nModel Bielik 4.5B v3 jest 'gated' i wymaga autoryzacji.")
        print("\nAby uzyskać dostęp:")
        print("1. Przejdź na https://huggingface.co/speakleash/Bielik-4.5B-v3.0-Instruct")
        print("2. Kliknij 'Agree and access repository'")
        print("3. Utwórz token na https://huggingface.co/settings/tokens")
        print("4. Zaloguj się używając jednej z opcji:")
        print("   a) Uruchom: huggingface-cli login")
        print("   b) Ustaw zmienną środowiskową: set HF_TOKEN=twój_token")
        print("\nLub uruchom ten skrypt ponownie po zalogowaniu.")
        return
    
    print(f"\n✅ Token HuggingFace znaleziony")
    
    # Utwórz katalog jeśli nie istnieje
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    
    print(f"\n📥 Pobieranie modelu...")
    print("(To może zająć kilka minut i pobrać ~9GB)")
    print("=" * 60)
    
    try:
        # Pobierz model
        snapshot_download(
            repo_id=MODEL_ID,
            local_dir=str(OUTPUT_DIR),
            token=hf_token,
        )
        
        print("\n" + "=" * 60)
        print("✅ Model pobrany pomyślnie!")
        print(f"Lokalizacja: {OUTPUT_DIR}")
        print("=" * 60)
        
        # Sprawdź co zostało pobrane
        files = list(OUTPUT_DIR.glob("*"))
        print(f"\nPobrane pliki ({len(files)}):")
        for file in sorted(files):
            if file.is_file():
                size_mb = file.stat().st_size / (1024 * 1024)
                print(f"  - {file.name} ({size_mb:.1f} MB)")
        
    except Exception as e:
        print(f"\n❌ Błąd podczas pobierania: {e}")
        print("\nMożliwe przyczyny:")
        print("- Brak dostępu do modelu (nie zaakceptowano warunków)")
        print("- Nieprawidłowy token HuggingFace")
        print("- Problemy z połączeniem internetowym")
        raise


if __name__ == "__main__":
    main()

