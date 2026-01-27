using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static manager for AI decision debug logging.
/// Tracks enemy AI decisions, pathfinding, and state changes.
/// </summary>
public static class AIDebugLog
{
    public enum LogType
    {
        TurnStart,
        TurnEnd,
        Decision,
        Move,
        Attack,
        Turn,
        Reveal,
        Wait,
        Spawn,
        Pathfinding,
        LOS,
        Info
    }

    public struct LogEntry
    {
        public float Timestamp;
        public LogType Type;
        public string EnemyName;
        public string Message;
        public Color Color;

        public LogEntry(LogType type, string enemyName, string message, Color color)
        {
            Timestamp = Time.time;
            Type = type;
            EnemyName = enemyName;
            Message = message;
            Color = color;
        }
    }

    private static List<LogEntry> _entries = new List<LogEntry>();
    private static int _maxEntries = 150;

    public static event Action<LogEntry> OnLogAdded;
    public static event Action OnLogCleared;

    public static IReadOnlyList<LogEntry> Entries => _entries;

    // Colors for different log types
    private static readonly Color TurnColor = new Color(1f, 0.9f, 0.4f);       // Yellow
    private static readonly Color DecisionColor = new Color(0.7f, 0.85f, 1f);  // Light blue
    private static readonly Color MoveColor = new Color(0.5f, 1f, 0.5f);       // Green
    private static readonly Color AttackColor = new Color(1f, 0.5f, 0.5f);     // Red
    private static readonly Color RevealColor = new Color(1f, 0.6f, 1f);       // Pink
    private static readonly Color WaitColor = new Color(0.6f, 0.6f, 0.6f);     // Gray
    private static readonly Color SpawnColor = new Color(0.8f, 0.5f, 1f);      // Purple
    private static readonly Color PathColor = new Color(0.4f, 0.8f, 0.8f);     // Cyan
    private static readonly Color LOSColor = new Color(0.9f, 0.7f, 0.4f);      // Orange
    private static readonly Color InfoColor = new Color(0.7f, 0.7f, 0.7f);     // Light gray

    /// <summary>
    /// Logs the start of an enemy turn phase.
    /// </summary>
    public static void LogTurnStart(int turnNumber)
    {
        AddEntry(LogType.TurnStart, "SYSTEM",
            $"═══ ENEMY TURN {turnNumber} START ═══", TurnColor);
    }

    /// <summary>
    /// Logs the end of an enemy turn phase.
    /// </summary>
    public static void LogTurnEnd(int turnNumber)
    {
        AddEntry(LogType.TurnEnd, "SYSTEM",
            $"═══ ENEMY TURN {turnNumber} END ═══", TurnColor);
    }

    /// <summary>
    /// Logs an enemy starting its action phase.
    /// </summary>
    public static void LogEnemyActionStart(string enemyName, int ap, string behaviorName)
    {
        AddEntry(LogType.Decision, enemyName,
            $"Action phase ({ap} AP) - Behavior: {behaviorName}", DecisionColor);
    }

    /// <summary>
    /// Logs a decision being evaluated.
    /// </summary>
    public static void LogDecision(string enemyName, string decision, string reason)
    {
        AddEntry(LogType.Decision, enemyName,
            $"Decision: {decision} | Reason: {reason}", DecisionColor);
    }

    /// <summary>
    /// Logs a move action.
    /// </summary>
    public static void LogMove(string enemyName, int fromX, int fromY, int toX, int toY, int apCost, string reason)
    {
        AddEntry(LogType.Move, enemyName,
            $"MOVE ({fromX},{fromY}) -> ({toX},{toY}) [{apCost} AP] | {reason}", MoveColor);
    }

    /// <summary>
    /// Logs an attack action.
    /// </summary>
    public static void LogAttack(string enemyName, string targetName, int targetX, int targetY)
    {
        AddEntry(LogType.Attack, enemyName,
            $"ATTACK -> {targetName} at ({targetX},{targetY})", AttackColor);
    }

    /// <summary>
    /// Logs a turn/rotation action.
    /// </summary>
    public static void LogTurn(string enemyName, string fromDir, string toDir, string reason)
    {
        AddEntry(LogType.Turn, enemyName,
            $"TURN {fromDir} -> {toDir} | {reason}", MoveColor);
    }

    /// <summary>
    /// Logs a blip reveal action.
    /// </summary>
    public static void LogReveal(string enemyName, int ghoulsSpawned, string reason)
    {
        AddEntry(LogType.Reveal, enemyName,
            $"REVEAL! Spawned {ghoulsSpawned} ghouls | {reason}", RevealColor);
    }

    /// <summary>
    /// Logs a wait/skip action.
    /// </summary>
    public static void LogWait(string enemyName, string reason)
    {
        AddEntry(LogType.Wait, enemyName,
            $"WAIT | {reason}", WaitColor);
    }

    /// <summary>
    /// Logs a spawn event.
    /// </summary>
    public static void LogSpawn(string enemyName, int x, int y, int spawnNumber, int totalToSpawn)
    {
        AddEntry(LogType.Spawn, enemyName,
            $"SPAWNED at ({x},{y}) [{spawnNumber}/{totalToSpawn}]", SpawnColor);
    }

    /// <summary>
    /// Logs pathfinding information.
    /// </summary>
    public static void LogPathfinding(string enemyName, string targetName, int distance, int pathLength, bool pathFound)
    {
        string status = pathFound ? $"Path found ({pathLength} tiles)" : "No path";
        AddEntry(LogType.Pathfinding, enemyName,
            $"PATH to {targetName}: Distance={distance}, {status}", PathColor);
    }

    /// <summary>
    /// Logs line of sight check.
    /// </summary>
    public static void LogLOSCheck(string enemyName, bool isVisible, int heroLOSTileCount)
    {
        string status = isVisible ? "VISIBLE to heroes" : "HIDDEN from heroes";
        AddEntry(LogType.LOS, enemyName,
            $"LOS: {status} ({heroLOSTileCount} tiles in hero vision)", LOSColor);
    }

    /// <summary>
    /// Logs adjacency check.
    /// </summary>
    public static void LogAdjacency(string enemyName, string adjacentTo)
    {
        if (string.IsNullOrEmpty(adjacentTo))
        {
            AddEntry(LogType.Info, enemyName, "Not adjacent to any hero", InfoColor);
        }
        else
        {
            AddEntry(LogType.Info, enemyName, $"Adjacent to {adjacentTo}", InfoColor);
        }
    }

    /// <summary>
    /// Logs available moves evaluation.
    /// </summary>
    public static void LogAvailableMoves(string enemyName, int totalMoves, int safeMoves)
    {
        AddEntry(LogType.Pathfinding, enemyName,
            $"Available moves: {totalMoves} total, {safeMoves} safe (out of LOS)", PathColor);
    }

    /// <summary>
    /// Logs a general info message.
    /// </summary>
    public static void LogInfo(string enemyName, string message)
    {
        AddEntry(LogType.Info, enemyName, message, InfoColor);
    }

    /// <summary>
    /// Logs AP status.
    /// </summary>
    public static void LogAPStatus(string enemyName, int currentAP, int maxAP)
    {
        AddEntry(LogType.Info, enemyName,
            $"AP: {currentAP}/{maxAP}", InfoColor);
    }

    private static void AddEntry(LogType type, string enemyName, string message, Color color)
    {
        var entry = new LogEntry(type, enemyName, message, color);
        _entries.Add(entry);

        // Trim old entries
        while (_entries.Count > _maxEntries)
        {
            _entries.RemoveAt(0);
        }

        OnLogAdded?.Invoke(entry);

        // Also log to Unity console
        Debug.Log($"[AILog] [{enemyName}] {message}");
    }

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    public static void Clear()
    {
        _entries.Clear();
        OnLogCleared?.Invoke();
    }

    /// <summary>
    /// Sets the maximum number of entries to keep.
    /// </summary>
    public static void SetMaxEntries(int max)
    {
        _maxEntries = Mathf.Max(10, max);
        while (_entries.Count > _maxEntries)
        {
            _entries.RemoveAt(0);
        }
    }
}
