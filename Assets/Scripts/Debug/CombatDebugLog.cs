using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static manager for combat debug logging.
/// Collects combat events and notifies listeners for UI display.
/// </summary>
public static class CombatDebugLog
{
    public enum LogType
    {
        Attack,
        Hit,
        Miss,
        Damage,
        Kill,
        Fire,
        Ability,
        Info
    }

    public struct LogEntry
    {
        public float Timestamp;
        public LogType Type;
        public string Message;
        public Color Color;

        public LogEntry(LogType type, string message, Color color)
        {
            Timestamp = Time.time;
            Type = type;
            Message = message;
            Color = color;
        }
    }

    private static List<LogEntry> _entries = new List<LogEntry>();
    private static int _maxEntries = 100;

    public static event Action<LogEntry> OnLogAdded;
    public static event Action OnLogCleared;

    public static IReadOnlyList<LogEntry> Entries => _entries;

    /// <summary>
    /// Logs a ranged attack attempt with dice rolls.
    /// </summary>
    public static void LogRangedAttack(string attacker, string target, int roll1, int roll2, int threshold, bool hit)
    {
        string rollStr = $"[{roll1}][{roll2}] vs {threshold}+";
        string result = hit ? "HIT" : "MISS";
        Color color = hit ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);

        AddEntry(hit ? LogType.Hit : LogType.Miss,
            $"RANGED: {attacker} -> {target}: {rollStr} = {result}", color);
    }

    /// <summary>
    /// Logs a melee attack attempt with dice rolls.
    /// </summary>
    public static void LogMeleeAttack(string attacker, string target, int roll1, int roll2, int threshold, bool hit)
    {
        string rollStr = $"[{roll1}][{roll2}] vs {threshold}+";
        string result = hit ? "HIT" : "MISS";
        Color color = hit ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);

        AddEntry(hit ? LogType.Hit : LogType.Miss,
            $"MELEE: {attacker} -> {target}: {rollStr} = {result}", color);
    }

    /// <summary>
    /// Logs a fire damage roll.
    /// </summary>
    public static void LogFireDamage(string target, int tileX, int tileY, int roll, int threshold, bool killed)
    {
        string rollStr = $"[{roll}] vs {threshold}+";
        string result = killed ? "KILLED" : "SURVIVED";
        Color color = killed ? new Color(1f, 0.6f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);

        AddEntry(LogType.Fire,
            $"FIRE ({tileX},{tileY}): {target}: {rollStr} = {result}", color);
    }

    /// <summary>
    /// Logs damage dealt to a unit.
    /// </summary>
    public static void LogDamage(string target, int damage, int remainingHP, int maxHP)
    {
        Color color = new Color(1f, 0.8f, 0.3f);
        AddEntry(LogType.Damage,
            $"DAMAGE: {target} takes {damage} dmg ({remainingHP}/{maxHP} HP)", color);
    }

    /// <summary>
    /// Logs a unit death.
    /// </summary>
    public static void LogKill(string target, string killer)
    {
        Color color = new Color(1f, 0.3f, 0.3f);
        string killerStr = string.IsNullOrEmpty(killer) ? "environment" : killer;
        AddEntry(LogType.Kill,
            $"KILL: {target} killed by {killerStr}", color);
    }

    /// <summary>
    /// Logs ability usage.
    /// </summary>
    public static void LogAbilityUse(string user, string abilityName, int apCost)
    {
        Color color = new Color(0.5f, 0.8f, 1f);
        AddEntry(LogType.Ability,
            $"ABILITY: {user} uses {abilityName} ({apCost} AP)", color);
    }

    /// <summary>
    /// Logs flamethrower usage with tile count.
    /// </summary>
    public static void LogFlamethrower(string user, int tileCount, int chargesRemaining, int maxCharges)
    {
        Color color = new Color(1f, 0.5f, 0.1f);
        AddEntry(LogType.Fire,
            $"FLAMETHROWER: {user} ignites {tileCount} tiles ({chargesRemaining}/{maxCharges} charges)", color);
    }

    /// <summary>
    /// Logs sustained fire bonus activation.
    /// </summary>
    public static void LogSustainedFire(string attacker, string target)
    {
        Color color = new Color(0.8f, 0.8f, 0.3f);
        AddEntry(LogType.Info,
            $"SUSTAINED FIRE: {attacker} has bonus vs {target} (5+ to hit)", color);
    }

    /// <summary>
    /// Logs a general info message.
    /// </summary>
    public static void LogInfo(string message)
    {
        AddEntry(LogType.Info, message, new Color(0.7f, 0.7f, 0.7f));
    }

    /// <summary>
    /// Logs a free shot (after movement).
    /// </summary>
    public static void LogFreeShot(string attacker)
    {
        Color color = new Color(0.3f, 1f, 0.8f);
        AddEntry(LogType.Info,
            $"FREE SHOT: {attacker} fires without AP cost (moved this turn)", color);
    }

    private static void AddEntry(LogType type, string message, Color color)
    {
        var entry = new LogEntry(type, message, color);
        _entries.Add(entry);

        // Trim old entries if we exceed max
        while (_entries.Count > _maxEntries)
        {
            _entries.RemoveAt(0);
        }

        OnLogAdded?.Invoke(entry);

        // Also log to Unity console for debugging
        Debug.Log($"[CombatLog] {message}");
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
