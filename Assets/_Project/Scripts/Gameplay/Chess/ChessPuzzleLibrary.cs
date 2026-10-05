using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KMA.Gameplay.Chess
{
    public static class ChessPuzzleLibrary
    {
        public const string ResourceFolder = "Chess/Puzzles";

        public static IReadOnlyList<PuzzleDefinition> LoadAll() => Resources.LoadAll<TextAsset>(ResourceFolder)
            .OrderBy(asset => asset.name, StringComparer.Ordinal)
            .Select(asset => Parse(asset.name, asset.text))
            .ToList();

        public static PuzzleDefinition ForDifficulty(ChallengeDifficulty difficulty)
        {
            string name = difficulty.ToString();
            return LoadAll().FirstOrDefault(puzzle => puzzle.difficulty == name) ??
                throw new InvalidOperationException($"No {name} chess puzzle in Resources/{ResourceFolder}.");
        }

        public static PuzzleDefinition Parse(string name, string json)
        {
            PuzzleDefinition puzzle = JsonUtility.FromJson<PuzzleDefinition>(json);
            if (puzzle == null)
                throw new InvalidOperationException($"Chess puzzle {name} is invalid: empty");
            if (!puzzle.TryValidateShape(out string error))
                throw new InvalidOperationException($"Chess puzzle {name} is invalid: {error}");
            return puzzle;
        }
    }
}
