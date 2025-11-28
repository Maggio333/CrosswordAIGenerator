# Skrypt do pobrania polskiego slownika
# Uruchom: .\download_dictionary.ps1

$dictionaryPath = Join-Path $PSScriptRoot "slowa.txt"

Write-Host "Pobieranie polskiego slownika..." -ForegroundColor Green

# Probuj pobrac z roznych zrodel (z polskimi znakami!)
$sources = @(
    @{
        Name = "GitHub - morfologik-polimorfologik"
        Url = "https://raw.githubusercontent.com/morfologik/polimorfologik/master/polimorfologik-2.1.txt"
        ParseMethod = "polimorfologik"
    },
    @{
        Name = "GitHub - polish-words"
        Url = "https://raw.githubusercontent.com/michalburzynski/polish-words/master/slowa.txt"
        ParseMethod = "simple"
    }
)

$downloaded = $false

foreach ($source in $sources) {
    try {
        Write-Host "Probuje pobrac z: $($source.Name)..." -ForegroundColor Yellow
        
        $response = Invoke-WebRequest -Uri $source.Url -UseBasicParsing -ErrorAction Stop
        
        # Parsuj slowa - filtruj tylko te z polskimi znakami i min 6 liter
        # Uzywamy Unicode escape dla polskich znakow w regex (A, C, E, L, N, O, S, Z, Z)
        $polishLettersPattern = '^[A-Z' + [char]0x0104 + [char]0x0106 + [char]0x0118 + [char]0x0141 + [char]0x0143 + [char]0x00D3 + [char]0x015A + [char]0x0179 + [char]0x017B + ']+$'
        $polishLettersMatch = '[' + [char]0x0104 + [char]0x0106 + [char]0x0118 + [char]0x0141 + [char]0x0143 + [char]0x00D3 + [char]0x015A + [char]0x0179 + [char]0x017B + ']'
        
        if ($source.ParseMethod -eq "polimorfologik") {
            # Polimorfologik: format "slowo +spacja+ tagi" - bierzemy tylko pierwsza kolumne
            $words = $response.Content -split "`n" | 
                Where-Object { $_ -and $_.Trim().Length -ge 6 } |
                ForEach-Object { 
                    $line = $_.Trim()
                    $word = ($line -split '\s+')[0].ToUpper()
                    if ($word -and $word.Length -ge 6) {
                        if ($word -match $polishLettersPattern) {
                            $word
                        }
                    }
                } |
                Select-Object -Unique |
                Sort-Object
        } else {
            # Prosty format: jedno slowo na linie
            $words = $response.Content -split "`n" | 
                Where-Object { $_ -and $_.Trim().Length -ge 6 } |
                ForEach-Object { 
                    $word = $_.Trim().ToUpper()
                    if ($word -match $polishLettersPattern) {
                        $word
                    }
                } |
                Select-Object -Unique |
                Sort-Object
        }
        
        # Sprawdz czy sa polskie znaki w slowniku
        $polishWordsCount = ($words | Where-Object { $_ -match $polishLettersMatch }).Count
        
        if ($words.Count -gt 100) {
            $words | Out-File -FilePath $dictionaryPath -Encoding UTF8
            Write-Host "Pobrano $($words.Count) slow do $dictionaryPath" -ForegroundColor Green
            if ($polishWordsCount -gt 0) {
                Write-Host "  Znaleziono $polishWordsCount slow z polskimi znakami" -ForegroundColor Green
            } else {
                Write-Host "  Uwaga: Slownik nie zawiera polskich znakow diakrytycznych!" -ForegroundColor Yellow
                Write-Host "    Aplikacja uzyje fallback (L->L, A->A, etc.)" -ForegroundColor Yellow
            }
            $downloaded = $true
            break
        }
    }
    catch {
        Write-Host "Nie udalo sie pobrac z $($source.Name): $($_.Exception.Message)" -ForegroundColor Red
        continue
    }
}

if (-not $downloaded) {
    Write-Host "Nie udalo sie pobrac slownika automatycznie." -ForegroundColor Yellow
    Write-Host "Mozesz recznie pobrac slownik i zapisac jako: $dictionaryPath" -ForegroundColor Yellow
    Write-Host "Format: jedno slowo na linie, UTF-8, min 6 liter" -ForegroundColor Yellow
}
