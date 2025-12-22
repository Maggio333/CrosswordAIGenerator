"""
Skrypt do pobierania modelu Qwen3-1.7B-Base w formacie Transformers (dla QLoRA)
"""

import os
from pathlib import Path
from huggingface_hub import snapshot_download

# Ścieżki względne
SCRIPT_DIR = Path(__file__).parent.resolve()
PROJECT_ROOT = SCRIPT_DIR.parent.parent
OUTPUT_DIR = PROJECT_ROOT / "PythonDatasetsScripts" / "qw" / "Qwen3-1.7B-Base"

MODEL_ID = "Qwen/Qwen3-1.7B-Base"

def main():
    print("=" * 60)
    print("Pobieranie modelu Qwen3-1.7B-Base w formacie Transformers")
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
    
    # Model Qwen3 jest publiczny - nie wymaga tokenu
    print(f"\n✅ Model jest publiczny - nie wymaga tokenu HuggingFace")
    
    # Utwórz katalog jeśli nie istnieje
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    
    print(f"\n📥 Pobieranie modelu...")
    print("(To może zająć kilka minut i pobrać ~3.5GB)")
    print("=" * 60)
    
    try:
        # Pobierz model
        snapshot_download(
            repo_id=MODEL_ID,
            local_dir=str(OUTPUT_DIR),
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
        print("- Problemy z połączeniem internetowym")
        print("- Brak miejsca na dysku")
        raise


if __name__ == "__main__":
    main()

