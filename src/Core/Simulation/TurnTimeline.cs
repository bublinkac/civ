using System;
using System.Collections.Generic;

namespace CivGame.Core;

public class TimelineSegment
{
    public int MaxTurn { get; set; }
    public int YearsPerTurn { get; set; }

    public TimelineSegment(int maxTurn, int yearsPerTurn)
    {
        MaxTurn = maxTurn;
        YearsPerTurn = yearsPerTurn;
    }
}

public static class TurnTimeline
{
    // These parameters can easily be customized
    public const int StartingYear = -4000; // -4000 is 4000 BC

    // Dynamic segments defining how many years pass per turn as turns progress.
    // Order matters (must be sorted by MaxTurn ascending).
    public static readonly List<TimelineSegment> Segments = new()
    {
        new TimelineSegment(60, 50),     // Turns 1-60: 50 years/turn (4000 BC - 1000 BC)
        new TimelineSegment(100, 25),    // Turns 61-100: 25 years/turn (1000 BC - 0 AD/10 AD)
        new TimelineSegment(162, 20),    // Turns 101-162: 20 years/turn (10 AD - 1250 AD)
        new TimelineSegment(212, 10),    // Turns 163-212: 10 years/turn (1250 AD - 1750 AD)
        new TimelineSegment(312, 2),     // Turns 213-312: 2 years/turn (1750 AD - 1950 AD)
        new TimelineSegment(9999, 1)     // Turns 313+: 1 year/turn (1950 AD+)
    };

    /// <summary>
    /// Calculates the integer year value for a given turn number (1-based).
    /// </summary>
    public static int GetYearForTurn(int turn)
    {
        if (turn <= 1) return StartingYear;

        int currentYear = StartingYear;
        int currentTurn = 1;

        foreach (var segment in Segments)
        {
            if (turn > segment.MaxTurn)
            {
                // Add the whole segment
                int turnsInSegment = segment.MaxTurn - currentTurn + 1;
                currentYear += turnsInSegment * segment.YearsPerTurn;
                currentTurn = segment.MaxTurn + 1;
            }
            else
            {
                // We are inside this segment
                int remainingTurns = turn - currentTurn;
                currentYear += remainingTurns * segment.YearsPerTurn;
                break;
            }
        }

        // Adjust for non-existent year 0 (move from 1 BC directly to 1 AD)
        if (StartingYear < 0 && currentYear >= 0)
        {
            currentYear += 1;
        }

        return currentYear;
    }

    /// <summary>
    /// Formats an integer year into a readable Civ-style string (e.g. "4000 BC" or "2050 AD").
    /// </summary>
    public static string FormatYear(int year)
    {
        if (year < 0)
        {
            return $"{Math.Abs(year)} BC";
        }
        else if (year == 0)
        {
            return "1 AD"; // Avoid year 0, jump to 1 AD like original Civ3
        }
        else
        {
            return $"{year} AD";
        }
    }

    /// <summary>
    /// Gets the formatted year string for a specific turn number.
    /// </summary>
    public static string GetFormattedYear(int turn)
    {
        return FormatYear(GetYearForTurn(turn));
    }
}
