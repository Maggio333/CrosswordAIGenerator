using System;
using System.Collections.Generic;
using System.Linq;
using CrosswordAIGenerator.Core.Infrastructure.Services;
using CrosswordAIGenerator.Core.Domain.Models;

// Prosty skrypt do walidacji CrossGrid
class Program
{
    static void Main(string[] args)
    {
        var generator = new CrossGridGenerator();
        
        // Przykładowe CrossGrid z datasetów
        var crossGrids = new[]
        {
            @"# GRID
R0:  ..... ..M.. ..... ..
R1:  ..... ..[8]A.. ..... ..
R2:  ..... P.Ł.. .K... ..
R3:  .B... [2]O.O.E .O... ..
R4:  .E... S.T.[7]K .O... N.
R5:  .N... T.O.S .R... I.
R6:  .Z.[4]T. A.[3]N.P .D... E.
R7:  .A.R. W.A.U .Y... K.
R8:  .NIER OZŻA[1]L ANIAM I.
R9:  .T.L. T.O.S .O... S.
R10: .R.O. W.W.O .W... Z.
R11: .O.[6]W. Ó.Y.W .A... O.
R12: .N.A. R.M.A .Ł... N.
R13: .[5]Ó.N. C...N .A... K.
R14: .W.Y. Z...A ..... O.
R15: ...M. A.... ..... W.
R16: ...I. ..... ..... E.",
            
            @"# GRID
R0:  ....N ...Z. ..... .
R1:  ....I ...M. ...K. .
R2:  FOTOE D[3]YTOR E[4]K.O. .
R3:  ....Z ...N. ...L. .
R4:  ....[1]B ...E. .Z.U. .
R5:  ..W.R .N.T. .R.S. .
R6:  ..G.O .I.A. .Z.Z. .
R7:  ..Ł.J .E.R. .Y.C. .
R8:  ..[6]Ę.E .Z.Y. .G.Z. .
R9:  .[2]ZBAN ALIZ[8]O WAŁAM .
R10: ..I.I .Ę.U. .Ł.N. .
R11: ..A.O .K.J. .A.I. .
R12: ..[5]N.W .[7]Ł.Ę. .B.N. .
R13: ..Y.Ą .Y... .Y.E. .
R14: ..M.. .M... ...M. .
R15: ..... ..... ..... .
R16: ..... ..... ..... .
R17: ..... ..... ..... .",
            
            @"# GRID
R0:  ..... O.... J....
R1:  ..... P.... O....
R2:  ..... O..N. D....
R3:  ..... D..I. [5]Ł....
R4:  ..... A..E. O....
R5:  ..... [4]T..K. W.O..
R6:  ..... K..Ł. C.M..
R7:  ..K.. O..[8]Ę. O.E..
R8:  ..W.. W..B. W.T..
R9:  .MANG USTOL I[1]S[2]KA.
R10: ..T.. J..W. ..[3]O..
R11: ..E.. E..Y. ..W..
R12: ..R.. ...M. ..[6]U..
R13: ..N.. ..... ..J..
R14: ..I.. ..... ..M..
R15: .NOTU [7]JĄCĄ. ..Y..
R16: ..N.. ..... .....
R17: .POKR USZYŁ .....
R18: ..M.. ..... ....."
        };

        Console.WriteLine("=== Walidacja CrossGrid ===\n");
        
        for (int i = 0; i < crossGrids.Length; i++)
        {
            Console.WriteLine($"--- Dataset {i + 1} ---");
            var validationResult = generator.ValidateCrossGrid(crossGrids[i]);
            
            Console.WriteLine($"Walidacja: {(validationResult.IsValid ? "✓ POPRAWNY" : "✗ BŁĘDNY")}");
            Console.WriteLine($"Szczegóły:");
            foreach (var detail in validationResult.Details)
            {
                Console.WriteLine($"  {detail.Key}: {detail.Value}");
            }
            
            if (validationResult.Errors.Any())
            {
                Console.WriteLine($"Błędy ({validationResult.Errors.Count}):");
                foreach (var error in validationResult.Errors)
                {
                    Console.WriteLine($"  ✗ {error}");
                }
            }
            
            if (validationResult.Warnings.Any())
            {
                Console.WriteLine($"Ostrzeżenia ({validationResult.Warnings.Count}):");
                foreach (var warning in validationResult.Warnings)
                {
                    Console.WriteLine($"  ⚠ {warning}");
                }
            }
            
            Console.WriteLine();
        }
    }
}

