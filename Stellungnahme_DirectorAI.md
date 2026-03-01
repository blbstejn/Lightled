# Stellungnahme zum praktischen Einbau der Director AI

## Vorbemerkung

Das folgende Dokument beschreibt den praktischen Einbau des in Abschnitt 8 entwickelten Systemkonzepts einer Director AI in das Taktikspiel *Lightled*. Es handelt sich um eine persönliche Reflexion des Umsetzungsprozesses: was umgesetzt wurde, wie der strategische Vorgang verlief, wo das Spiel als Demonstrator Grenzen gesetzt hat, und welche Aspekte des Konzepts aus Zeit-, Komplexitäts- oder architektonischen Gründen nicht eingebaut werden konnten.

*Lightled* ist ein rundenbasiertes 2D-Taktikspiel auf einem Grid, bei dem ein Heldentrupp gegen aufkommende Gegnerwellen aus sogenannten Blips (unbekannte Gegnertokens) und Ghouls (aufgedeckte Nahkampfeinheiten) antritt. Die Spielenden steuern ihre Helden rundenbasiert, der Gegner wird von einer verhaltensbasierten KI gesteuert. Es gibt kein Trefferpunkte-System — alle Einheiten sterben bei einem erfolgreichen Angriff sofort.

---

## Strategischer Vorgang

Die Umsetzung folgte dem Transparenz-Schichten-Modell (Abschnitt 8.3) als architektonischer Leitlinie. Der Einbau wurde in drei Phasen gegliedert:

1. **Die analytische Messschicht zuerst.** Das Pressure-Management-System wurde als kontinuierliches Messinstrument implementiert, das den Spielzustand in Echtzeit ausliest und zu einem aggregierten Pressure-Wert (0-1) verdichtet. Dies entspricht der im Konzept beschriebenen Informationsbasis für Interventionsentscheidungen.

2. **Die Interventionsschicht als API.** Statt den Director direkt in andere Systeme eingreifen zu lassen, wurde eine Query-basierte Architektur gewählt: andere Systeme *fragen* den Director nach Empfehlungen und entscheiden selbst, wie sie diese anwenden. Dies wahrt die Entscheidungsautonomie der einzelnen Systeme und verhindert unbeabsichtigte Kopplungen.

3. **Einbaupunkte als minimal-invasive Ergänzungen** — bestehende Systeme wurden nur an wenigen, klar definierten Stellen erweitert. Es wurden keine bestehenden Mechaniken verändert, nur optionale Abfragen hinzugefügt.

Die Schutzschicht erforderte keinen Einbau, da sie den Schutz bestehender Systeme beschreibt. Grid, Kampfmathematik, Sichtlinien und Bewegungskosten waren bereits architektonisch isoliert und besitzen keine Schnittstelle zum Director. Das Spiel erfüllt diese Schicht von Haus aus.

---

## Das Pressure-Management-System

### Kernimplementierung

Die zentrale Implementierung befindet sich in `DirectorAI.cs` (Assets/Scripts/Managers/DirectorAI.cs). Die Klasse folgt dem Singleton-Pattern des Projekts. Die Messwerte werden ereignisbasiert über die öffentliche Methode `RecalculatePressure()` aktualisiert (Zeile 195), die von anderen Systemen nach zustandsverändernden Ereignissen aufgerufen wird — nicht pro Frame.

**Architekturentscheidung: Ereignisbasierte statt framebasierter Messung.** Die ursprüngliche Implementierung aktualisierte alle sieben Pressure-Komponenten in der `Update()`-Methode, also potentiell 60 Mal pro Sekunde. In einem rundenbasierten Spiel ändert sich der Spielzustand jedoch ausschließlich an diskreten Aktionspunkten: wenn eine Einheit sich bewegt, angreift, spawnt oder stirbt. Zwischen diesen Ereignissen sind die Berechnungen — insbesondere die verschachtelten Schleifen in `CalcProximity()` und `CalcExposure()`, die über alle Held-Gegner-Paare iterieren — redundant. Die Umstellung auf `RecalculatePressure()` eliminiert diese Redundanz und wird stattdessen an folgenden Stellen aufgerufen:

- **Vor der Spawn-Phase** (`EnemyAIManager.ExecuteEnemyTurnCoroutine()`): damit `ShouldSkipSpawn()` auf aktuellen Daten basiert
- **Nach jeder Gegneraktion** (`EnemyAIManager.ProcessEnemyActions()`): damit der nächste Gegner auf Basis des aktuellen Spielzustands entscheidet
- **Bei Kampfereignissen**: `RegisterCombatEvent()` und `RegisterHeroDeath()` lösen intern eine Neuberechnung aus, sodass Combat-Intensity-Spikes sofort in den Pressure-Score einfließen
- **Am Anfang des Heldenzugs** (`GameManager.StartHeroTurn()`): damit das Debug-Overlay aktuelle Werte zeigt

Der Combat-Intensity-Zerfall verwendet `Time.time`-basierte Elapsed-Time statt `Time.deltaTime`, um auch bei variablen Intervallen zwischen Aufrufen korrekt zu zerfallen. Das Smoothing nutzt denselben Mechanismus: kurze Intervalle zwischen Aktionen erzeugen sanfte Übergänge, lange Pausen (z.B. Spieler-Bedenkzeit) lassen den Wert zum Rohwert springen.

Der Pressure-Wert aggregiert sechs gewichtete Komponenten, die zusammen 1.0 ergeben:

| Komponente | Gewicht | Konzept-Entsprechung | Code |
|---|---|---|---|
| Proximity | 0.30 | Räumliche Bedrohungswahrnehmung (8.5.1) | `CalcProximity()` |
| Outnumbered | 0.15 | Kräfteasymmetrie (rasterspezifisch) | `CalcOutnumbered()` |
| Hidden Threats | 0.10 | Informationsunsicherheit (8.5.5) | `CalcHiddenThreats()` |
| Hero Loss | 0.25 | Fehlerakkumulation, vereinfacht (8.4.4) | `CalcHeroLoss()` |
| Combat Intensity | 0.10 | Temporale Spannungskomponente | `_combatIntensity`, zerfällt über Zeit |
| Exposure | 0.10 | Taktische Exposition (8.4.1) | `CalcExposure()` |

Eine ursprünglich vorgesehene **Resource-Komponente** (CP-Verbrauch) wurde entfernt. Command Points setzen sich in *Lightled* jede Runde automatisch auf den Maximalwert zurück, unabhängig von der Spielsituation. CP-Knappheit innerhalb eines Zuges ist ein transientes Signal ohne nachhaltige Aussagekraft über den Druck auf die Spielenden — es bildet keine strategische Verschlechterung ab, nur eine momentane Einschränkung, die sich von selbst auflöst. Das frei gewordene Gewicht (0.10) wurde auf Proximity (+0.05, nun 0.30) und Hero Loss (+0.05, nun 0.25) verteilt, da unmittelbare räumliche Bedrohung und irreversible Einheitenverluste die stärksten Drucksignale in einem Permadeath-Taktikspiel darstellen.

Im Folgenden wird erläutert, was jede Komponente konkret misst und warum sie als Drucksignal relevant ist:

**Proximity** (Gewicht 0.30) misst die kürzeste begehbare Pfaddistanz zwischen irgendeinem angreifbaren Gegner und irgendeinem Helden. Steht ein Ghoul direkt neben einem Helden, liegt der Wert bei 1.0; ist kein Gegner innerhalb von 8 Schritten erreichbar, liegt er bei 0.0. Die Messung verwendet BFS-Pfaddistanz statt Manhattan-Distanz, damit ein Gegner hinter einer Wand — der 2 Manhattan-Tiles, aber 15 tatsächliche Schritte entfernt ist — keinen künstlich hohen Druckwert erzeugt. Dies ist die gewichtigste Komponente, weil tatsächliche räumliche Erreichbarkeit in einem Taktikspiel das stärkste Drucksignal darstellt.

**Outnumbered** (Gewicht 0.15) setzt die Anzahl angreifbarer Gegner ins Verhältnis zu lebenden Helden. Bei einem Verhältnis von 1:1 liegt der Wert bei 0.25, bei 2:1 bei 0.5 und ab 4:1 bei 1.0. Blips, die noch auf ihrem Spawn-Tile stehen, werden nicht mitgezählt, da sie weder angreifbar sind noch aktiv am Kampfgeschehen teilnehmen. Diese Komponente hat keine direkte Entsprechung im Konzept, sondern ist eine rasterspezifische Metrik für die zahlenmäßige Kräfteasymmetrie. Das Konzept beschreibt zwar "Ressourcenstatus" als Pressure-Komponente, aber in einem Spiel wie diesem gibt es keine Ressourcen im klassischen Sinne — keine Munition, kein Mana, keine Verbrauchsgüter. Was es gibt, sind Einheiten auf dem Feld, und deren Zahlenverhältnis ist in einem Taktikspiel ein eigenständiges Drucksignal: Wer zahlenmäßig unterlegen ist, muss jede verbliebene Einheit umso sorgfältiger einsetzen.

**Hidden Threats** (Gewicht 0.10) zählt, wie viele unaufgedeckte Blips sich aktiv auf dem Spielfeld bewegen (also ihr Spawn-Tile verlassen haben). Bei 3 oder mehr aktiven Blips liegt der Wert bei 1.0. Diese Komponente bildet die Unsicherheit ab, die das Blip-System erzeugt: Jeder Blip enthält zwischen 1 und 3 Ghouls, aber die Spielenden wissen nicht, wie viele. Je mehr unaufgedeckte Blips sich nähern, desto unberechenbarer wird die Situation.

**Hero Loss** (Gewicht 0.25) erfasst den Anteil verlorener Helden bezogen auf die Startanzahl. Wenn von drei Helden einer gefallen ist, liegt der Wert bei 0.33; bei zweien bei 0.67. Zusammen mit Proximity als höchstgewichtete Komponente reflektiert diese, dass der Verlust eines Teammitglieds in einem Spiel ohne Heilung oder Respawn eine drastische und irreversible Verschlechterung der Lage darstellt. Im Konzept (Abschnitt 8.4.4) werden Einheitenverluste als "kritische Fehler" klassifiziert.

**Combat Intensity** (Gewicht 0.10) wird nicht kontinuierlich berechnet, sondern durch Ereignisse gespeist. Jedes Kampfereignis — ob Nahkampf, Fernkampf oder Flammenwerfer — erhöht den Wert um 0.4, ein Heldentod sogar um 0.7, jeweils bis maximal 1.0. Der Wert zerfällt dann über Zeit mit einer konfigurierbaren Rate. Damit bildet die Komponente die Nachwirkung intensiver Kampfphasen ab: Unmittelbar nach einem Feuergefecht ist der Druck höher als nach mehreren ruhigen Runden, selbst wenn sich die sonstige Situation nicht verändert hat.

**Exposure** (Gewicht 0.10) berechnet für jeden lebenden Helden, wie viele angreifbare Gegner innerhalb von 8 begehbaren Schritten erreichbar sind, und bildet den Durchschnitt über alle Helden. Wenn im Durchschnitt 4 oder mehr Gegner pro Held in Reichweite sind, liegt der Wert bei 1.0. Auch hier wird BFS-Pfaddistanz verwendet: Gegner hinter Wänden, die per Manhattan-Distanz nah wirken, aber tatsächlich weit entfernt sind, werden nicht als aktive Bedrohung gewertet. Im Unterschied zur Proximity-Komponente, die nur den *nächsten* Gegner erfasst, misst Exposure die *Breite* der Bedrohung: Ein einzelner naher Ghoul ist gefährlich, aber drei Ghouls in unterschiedlichen Richtungen bei moderater Distanz können überwältigender sein.

Der geglättete Pressure-Wert wird in vier Stufen klassifiziert (`PressureLevel`), die den Interventionsstufen des Konzepts entsprechen (Zeile 175-183):

- **Low** (< 0.25): Eskalation erlaubt — der Director verstärkt die Bedrohung
- **Moderate** (0.25-0.50): Normalbetrieb — keine Intervention
- **Elevated** (0.50-0.70): Weiche Stabilisierung — defensivere KI, reduzierte Spawns
- **Critical** (> 0.70): Starke Stabilisierung — deutlich defensivere KI, stark reduzierte Spawns

### Anpassungen gegenüber dem Konzept

**Verbleibende Gesundheit** konnte nicht direkt abgebildet werden. *Lightled* hat kein Trefferpunkte-System — Einheiten sind entweder lebendig oder tot. Es gibt keine graduellen Gesundheitszustände, die als Drucksignal dienen könnten. Stattdessen wird der **Heldenverlust** als Verhältnis verlorener zu anfänglichen Helden gemessen (`CalcHeroLoss()`). Dies ist eine gröbere Metrik als ein HP-basierter Ressourcenstatus, erfasst aber den kritischsten Aspekt: den unwiderruflichen Verlust von Einheiten.

**Raumkontrolle** ist im Konzept als Verhältnis kontrollierter Zellen beschrieben. Eine vollständige Umsetzung hätte erfordert, die Gesamtzahl begehbarer Zellen öffentlich zugänglich zu machen und die kombinierte Helden-Sichtlinie dagegen zu setzen. Da die Tile-Datenstruktur des `GridManager` privat ist und eine Erweiterung der Schnittstelle nicht zwingend war, wurde Raumkontrolle vereinfacht als **Exposure** abgebildet: die durchschnittliche Anzahl feindlicher Einheiten im Bedrohungsradius pro Held. Dies erfasst die taktische Exposition der Helden, wenn auch aus einer etwas anderen Perspektive als im Konzept beschrieben.

**Fehlerakkumulation** wird im Konzept als granulares System mit verschiedenen Fehlerkategorien (räumlich, taktisch, kritisch) beschrieben. Eine vollständige Implementierung hätte erfordert, jede Spielerentscheidung zu evaluieren und als optimal oder suboptimal zu klassifizieren — ein erheblicher Aufwand für die Analyse von Entscheidungen auf einem Raster. Stattdessen wird der Heldenverlust (`CalcHeroLoss()`) als Proxy für akkumulierte Fehler verwendet: Wer Helden verliert, hat vermutlich Fehler gemacht. Dies ist eine Vereinfachung, die den Kern der Metrik erfasst, ohne die Komplexität einer vollständigen Fehlerklassifikation zu erfordern.

---

## Einbaupunkte und Interventionslogik

### KI-Verhaltensmodulation (Abschnitt 8.3.2.1)

Der Director moduliert die Aggression der Blip-KI über einen Multiplikator. Der Einbau erfolgt in `BlipBehavior.cs`, Zeilen 56-57:

```csharp
if (DirectorAI.Instance != null)
    aggressionFactor = Mathf.Clamp01(aggressionFactor * DirectorAI.Instance.GetAggressionModifier());
```

Das `BlipBehavior` berechnet einen Aggressionsfaktor basierend auf der Anzahl aktiver Blips auf dem Feld. Der Director skaliert diesen Faktor: bei niedrigem Druck wird er auf bis zu 1.3x verstärkt (aggressivere KI), bei kritischem Druck auf 0.4x reduziert (defensivere KI). Die KI-Entscheidungslogik selbst bleibt unverändert — es wird lediglich die Schwelle verschoben, ab der bestimmte Verhaltensweisen aktiviert werden.

Dies entspricht der Konzeptanforderung, dass die KI "weiterhin kompetent erscheinen muss" und dass "Verhaltensvariation als plausible taktische Präferenz interpretierbar bleiben" soll. Ein Blip, der bei hohem Druck defensiver agiert (häufiger versteckte Pfade nutzt, weniger durch Sichtlinien läuft), wirkt wie eine vorsichtigere taktische Entscheidung — nicht wie künstliche Inkompetenz.

Der Aggressions-Multiplikator wird über `GetAggressionModifier()` bereitgestellt (DirectorAI.cs, Zeile 188). Die Interpolation erfolgt linear zwischen `aggressionAtLow` (1.3) und `aggressionAtCritical` (0.4), parametrisiert durch den Pressure-Wert.

#### Anti-Oszillation und Fortschritts-Prüfung

Im Rahmen der Iterationsphase wurden zwei Verhaltensprobleme der Blip-KI identifiziert und behoben, die sich aus dem Zusammenspiel zwischen Director-modulierter Aggression und der Pfadwahl-Logik ergaben:

**Oszillation zwischen zwei Tiles.** Ein Blip mit niedriger Aggression (z.B. durch Director-Dämpfung bei hohem Pressure) nutzte den Sneak-Modus, der den sicheren Tile mit der geringsten Distanz zum Helden wählt. Wenn dieser Tile jedoch nicht näher am Helden war als die aktuelle Position, konnte der Blip im Folgezug zum Ausgangstile zurückkehren — und so endlos hin- und herspringen. Dies blockierte nicht nur den betroffenen Blip, sondern auch nachfolgende Blips, die den besetzten Tile nicht passieren konnten.

Die Lösung besteht aus zwei Mechanismen, implementiert in `BaseEnemy.cs` und `BlipBehavior.cs`:

1. **PreviousTile-Tracking** (`BaseEnemy.cs`, Zeile 11-12, 71): Jeder Gegner merkt sich den Tile, auf dem er vor dem letzten Zug stand. Dieses Feld wird in `ExecuteMove()` gesetzt und bleibt über Rundengrenzen hinweg erhalten. `PathfindingUtils.FindBestMoveToward()` und `FindBestSafeMoveToward()` akzeptieren einen optionalen `excludeTile`-Parameter, der den vorherigen Tile aus den Bewegungskandidaten filtert. Priority 6 (Last Resort) behält den alten Tile als Fallback, damit der Blip nie vollständig blockiert wird.

2. **Fortschritts-Prüfung im Sneak-Modus** (`BlipBehavior.cs`, Priority 4, Zeile 183-194): Ein sicherer Zug wird nur akzeptiert, wenn er die tatsächliche Pfaddistanz zum Ziel *verringert* (siehe BFS-Pfaddistanz unten). Wenn kein sicherer Tile Fortschritt bietet, wechselt der Blip auf den direkten Pfad — auch wenn dieser durch eine Helden-Sichtlinie führt. Der Blip geht lieber ein Risiko ein, als auf der Stelle zu stehen oder seitwärts zu pendeln.

Diese Kombination adressiert auch das Problem, dass Blips bei Director-reduzierter Aggression zu passiv agierten: Ein Blip, dessen Aggressionsfaktor durch den Director auf unter 0.4 gedrückt wird, bevorzugt zwar weiterhin sichere Pfade, wird aber nicht mehr durch fehlende sichere Optionen zum Stillstand gezwungen. Das Verhalten bleibt plausibel — der Blip *versucht* sich zu verstecken, akzeptiert aber Risiken, wenn Verstecken keinen taktischen Fortschritt ermöglicht.

#### BFS-Pfaddistanz statt Manhattan-Distanz

Ein weiteres iteratives Problem betraf Blips, die sich in geringer Manhattan-Distanz zu einem Helden befanden, deren tatsächlicher Weg durch das Level jedoch aufgrund von Wänden und Hindernissen erheblich länger war. Ein Blip, der durch eine Wand nur 2 Tiles von einem Helden entfernt stand, aber 15 Tiles um die Wand herumlaufen musste, verhielt sich erratisch: Die Fortschritts-Prüfung verwarf Züge, die sich in Manhattan-Distanz nicht verbesserten, obwohl sie den einzig gangbaren Weg darstellten. Der Blip konnte keinen Fortschritt erzielen und pendelte oder stagnierte.

Die Lösung ersetzt die Manhattan-Distanz in der gesamten Blip-KI-Entscheidungsfindung durch eine BFS-basierte (Breadth-First Search) Pfaddistanz, die Wände und nicht-begehbare Tiles korrekt berücksichtigt:

1. **Distanzkarte** (`PathfindingUtils.ComputeDistanceMap()`, Zeile 206-231): Zu Beginn jeder Blip-Entscheidung wird eine BFS-Distanzkarte vom Ziel-Helden aus berechnet. Diese Map ordnet jedem begehbaren Tile seine tatsächliche Schrittentfernung zum Helden zu. Tiles hinter Wänden, die per Manhattan-Distanz nah erscheinen, erhalten korrekt hohe Distanzwerte. Die BFS nutzt `Tile.IsWalkableType` statt `Tile.Walkable`, sodass temporär besetzte oder brennende Tiles als grundsätzlich begehbar gelten — der Pfad *durch* sie wird berücksichtigt, auch wenn sie momentan blockiert sind.

2. **Durchgehende Verwendung** (`BlipBehavior.cs`): Alle Pfadentscheidungen — Charge, Escape, Sneak, Advance, Risky Move und Last Resort — nutzen die BFS-Distanzkarte über `PathfindingUtils.FindBestMoveToward(enemy, distMap, ...)` und `FindBestSafeMoveToward(enemy, distMap, ...)`. Die Fortschritts-Prüfung im Sneak-Modus vergleicht `GetPathDistance()` statt `ManhattanDistance()`, sodass ein Zug, der den tatsächlichen Weg verkürzt, korrekt als Fortschritt erkannt wird — auch wenn er sich in Manhattan-Distanz verschlechtert.

Die BFS-Pfaddistanz wurde in einem zweiten Schritt auch auf die Pressure-Messung ausgeweitet. `CalcProximity()` und `CalcExposure()` in `DirectorAI.cs` verwendeten ursprünglich Manhattan-Distanz, was dazu führte, dass Gegner hinter Wänden fälschlicherweise als unmittelbare Bedrohung gewertet wurden — der Director reduzierte die Schwierigkeit, obwohl die Spielenden faktisch sicher waren. Beide Methoden verwenden nun dieselbe BFS-Infrastruktur: `RecalculatePressure()` berechnet zu Beginn jeder Neuberechnung eine Distanzkarte pro lebendem Helden und übergibt diese an `CalcProximity()` und `CalcExposure()`, sodass die BFS-Läufe nicht doppelt anfallen.

### Eskalationssteuerung und Verstärkung (Abschnitt 8.3.2.2)

Der Director moduliert die Verstärkungsmechanik an zwei Stellen im `EnemyAIManager`:

**Probabilistische Spawn-Modulation** (EnemyAIManager.cs, Zeilen 105-109):
```csharp
if (DirectorAI.Instance != null && DirectorAI.Instance.ShouldSkipSpawn())
{
    Debug.Log($"[AI] Spawn Phase: Director deferred spawn #{i + 1}");
    continue;
}
```

Einzelne Spawns innerhalb einer Runde können mit einer bestimmten Wahrscheinlichkeit übersprungen werden (30% bei Elevated, 60% bei Critical). Eine bewusste Designentscheidung war es, auf eine deterministische Mengenreduktion zu verzichten. Stattdessen wird jeder einzelne Spawn unabhängig gegen eine Wahrscheinlichkeit gewürfelt. Dies ist subtiler als eine pauschale Kürzung: Die Grundanzahl der vorgesehenen Spawns bleibt bestehen, aber nicht jeder kommt zwangsläufig zustande. Es simuliert eine Art Verstärkungsverzögerung, wie im Konzept unter "Verstärkungstiming innerhalb kommunizierter Fenster" beschrieben, und wahrt gleichzeitig die Möglichkeit, dass auch bei hohem Druck die volle Spawn-Anzahl durchkommt.

### Spawn-Positionierung (Abschnitt 8.4.1)

Der Director beeinflusst die taktische Bewertung von Spawn-Positionen in `UnitManager.cs`, Zeilen 213-217:

```csharp
if (DirectorAI.Instance != null)
{
    float bias = DirectorAI.Instance.GetSpawnPositionBias();
    score += bias * (distToHeroes / 10f);
}
```

Das bestehende taktische Scoring bewertet Spawn-Tiles nach Sichtbarkeit und Distanz zu Helden. Der Director addiert einen distanzabhängigen Bias: bei hohem Druck werden entferntere Tiles bevorzugt (weniger bedrohliche Spawn-Positionen), bei niedrigem Druck nähergelegene (taktisch vorteilhaftere Positionen für den Gegner).

Dies entspricht direkt der Konzeptbeschreibung: "Bei Eskalationsbedarf werden Gegner in taktisch vorteilhafteren Positionen platziert [...] Bei Stabilisierungsbedarf werden Gegner in taktisch nachteiligeren Positionen platziert." Die Intervention liegt in der Wahl der Situation, nicht in der Verfälschung der Mechanik.

### Würfelmodulation (Abschnitt 8.3.2.1)

*Lightled* verwendet für alle Kampfauflösungen ein d6-basiertes Würfelsystem, das direkt dem Vorbild Space Hulk entspricht und sich als weiterer Einbaupunkt für den Director eignet. Alle drei Kampfmechaniken besitzen eine Zufallskomponente:

- **Nahkampf** (`MeleeAttackAbility.cs`): Beide Seiten würfeln N×d6 und nehmen das höchste Ergebnis. Helden würfeln 2 (Angriff) bzw. 1 (Verteidigung) Würfel, Ghouls immer 3. Wer das höhere Ergebnis würfelt, gewinnt.
- **Fernkampf** (`RangedAttackAbility.cs`): 2×d6, Treffer bei 6+ auf mindestens einem Würfel (5+ bei Sustained Fire).
- **Flammenwerfer** (`FlamethrowerAbility.cs`): 1×d6 pro betroffener Einheit, Tötung bei 2+.

Der Director moduliert zwei dieser Parameter über die Query-API:

**Nahkampf-Würfelanzahl** (`MeleeAttackAbility.cs`, `GetDiceCount()`):
```csharp
int modifier = DirectorAI.Instance?.GetMeleeDiceModifier() ?? 0;
int dice = Mathf.Max(1, 3 + modifier);
```
Bei Critical-Pressure erhalten Ghouls nur 2 statt 3 Würfel — die Wahrscheinlichkeit, einen Helden im Nahkampf zu töten, sinkt spürbar. Bei Low-Pressure steigt die Würfelzahl auf 4, was den Druck bei komfortablen Spielständen erhöht. Die Würfelanzahl der Helden bleibt unverändert.

**Fernkampf-Trefferschwelle** (`RangedAttackAbility.cs`):
```csharp
int thresholdMod = DirectorAI.Instance?.GetHitThresholdModifier() ?? 0;
if (thresholdMod != 0)
    hitThreshold = Mathf.Clamp(hitThreshold + thresholdMod, 3, 6);
```
Bei Critical-Pressure sinkt die Trefferschwelle von 6+ auf 5+ (bzw. von 5+ auf 4+ bei Sustained Fire), was die Trefferwahrscheinlichkeit von ca. 31% auf ca. 56% erhöht. Beide Schwellenwerte und der Würfel-Modifier sind im Inspector konfigurierbar.

**Designentscheidung: Würfelanzahl statt Ergebnismanipulation.** Das Konzept nennt als mögliche Ansätze sowohl die Anpassung einzelner Ergebnisse als auch allgemeine Modifikatoren. Implementiert wurde die Würfelanzahl als Hebel, da sie die natürliche Streuung des Würfelsystems beibehält: Ein Ghoul mit 2 statt 3 Würfeln kann immer noch ein 6 würfeln — er hat nur statistisch schlechtere Chancen. Direkte Ergebnismanipulation (z.B. das höchste Ergebnis kappen) wäre erkennbar unnatürlich und würde die Glaubwürdigkeit der Mechanik untergraben.

**Transparenzabwägung.** Würfelmodifikationen sind die am schwierigsten erkennbare Form der Director-Intervention — Spielende erleben sie als Glücksschwankung, nicht als Systemeingriff. Dies entspricht dem Konzept, dass Interventionen "als plausible taktische Variation" interpretierbar bleiben sollen. Gleichzeitig ist es die Intervention mit dem größten Risiko für das Spielgefühl: Wer weiß, dass das System bei Verlusten die Würfel manipuliert, könnte das als unfair empfinden. Das Debug-Overlay (F3) zeigt beide Modifikatoren explizit an, um zumindest auf Entwicklerebene Transparenz zu wahren.

### Kampfereignis-Meldungen

Drei Kampfsysteme melden Ereignisse an den Director, um die Combat-Intensity-Komponente zu speisen:

- **Nahkampf**: `MeleeAttackAbility.cs`, Zeile 141 — `DirectorAI.Instance?.RegisterCombatEvent()`
- **Fernkampf**: `RangedAttackAbility.cs`, Zeile 156 — `DirectorAI.Instance?.RegisterCombatEvent()`
- **Flammenwerfer**: `FlamethrowerAbility.cs`, Zeile 131 — `DirectorAI.Instance?.RegisterCombatEvent()`

Zusätzlich meldet `UnitManager.cs` in Zeile 302 den Tod eines Helden über `DirectorAI.Instance?.RegisterHeroDeath()`, was einen stärkeren Spike auslöst.

Diese Meldungen beeinflussen nicht den Kampfausgang selbst. Seit der Umstellung auf ereignisbasierte Messung lösen beide Methoden intern `RecalculatePressure()` aus, sodass der Combat-Intensity-Spike sofort in den aggregierten Pressure-Score einfließt und im Debug-Overlay sichtbar wird. Die Null-Propagation (`?.`) stellt sicher, dass das System ohne Director-Instanz weiterhin funktioniert.

---

## Nicht einbaubare Aspekte

### Aufgrund von Spiellimitationen

**Fog-of-War-Management (Abschnitt 8.5.5)** — Das Konzept beschreibt umfangreiche Informationskontrolle über Fog of War: Patrouillenmechaniken, Sichtbarkeitswechsel, Informationsdegradation über Runden, und Ambiguität bei Distanzaufklärung. *Lightled* besitzt kein Fog-of-War-System. Das gesamte Grid ist jederzeit sichtbar. Die einzige Informationsunsicherheit besteht in der Zusammensetzung von Blips (1-3 Ghouls), die erst bei Aufdeckung bekannt wird. Eine Fog-of-War-Implementierung hätte ein grundlegend anderes Informationssystem erfordert, das weit über den Scope des Director-Einbaus hinausgeht. Die `HiddenThreatScore`-Komponente erfasst immerhin die verbleibende Unsicherheit durch unaufgedeckte Blips.

**Missionsziel-Flexibilisierung (Abschnitt 8.3.2.3)** — Das Konzept sieht vor, dass der Director bei akkumulierten Schwierigkeiten Primärziele optionalisieren und alternative Erfolgspfade aktivieren kann. *Lightled* kennt nur zwei Siegbedingungen (Flucht, Flammenwerfer-Ziel), keine Sekundärziele und keine dynamisch aktivierbaren Alternativpfade. Die gesamte Infrastruktur für flexible Missionsziele fehlt. Dies wäre eine grundlegende Erweiterung des Spieldesigns gewesen, nicht des Director-Systems.

**Evakuierungsoption (Abschnitt 8.4.1)** — Bei kritischem Pressure sieht das Konzept die Aktivierung einer Evakuierungsoption vor. *Lightled* hat zwar ein Flucht-System (Helden können Target-Tiles erreichen), aber dieses ist stets verfügbar und nicht dynamisch aktivierbar. Eine Director-gesteuerte Evakuierungsoption hätte erfordert, Target-Tiles zur Laufzeit zu erzeugen oder freizuschalten — eine Mechanik, die nicht existiert.

**Gegnerische Sichtlinien und Rollendifferenzierung (Abschnitt 8.5.4)** — Das Konzept beschreibt "Sichtlinien-Dominanz verschieben" und differenzierte taktische Rollen für Gegnertypen. In *Lightled* haben nur Helden ein Sichtlinien-System; Gegner berechnen keine eigene LOS. Zudem existieren nur zwei Gegnertypen (Blips und Ghouls), die sich zwar im Verhalten, aber nicht in spezifischen taktischen Rollen wie Flanker, Suppressor oder Vanguard unterscheiden. Eine Rollendifferenzierung hätte neue Gegnertypen mit spezialisierten Verhaltensweisen erfordert.

**Informationsintegrität-Protokoll im vollen Umfang (Abschnitt 8.4.3)** — Das Konzept beschreibt einen Zell-Informationsstatus (gesichtet/ungesichtet, beobachtet/unbeobachtet), der festlegt, auf welchen Zellen der Director operieren darf. Da *Lightled* keine Sichthistorie pro Tile trackt und das gesamte Grid sichtbar ist, ist das Protokoll in dieser Form nicht anwendbar. Was jedoch implizit gewahrt wird: Gegner spawnen ausschließlich auf designierten `BlipSpawnTiles`, und auf diesen Tiles sind sie unangreifbar. Dies stellt sicher, dass der Director keine Gegner in vom Spieler kontrollierten Bereichen materialisiert.

### Aufgrund von Zeit- und Komplexitätsgrenzen

**Kompetenztrend-Monitoring (Abschnitt 8.4.2)** — Das kampagnenübergreifende Monitoring der Spielerkompetenz erfordert Persistenz zwischen Missionen: Erfolgsquoten, Effizienzentwicklung, Aufklärungsqualität über mehrere Spieldurchläufe. *Lightled* besitzt kein Save-System und kein Kampagnen-Progressionssystem. Selbst wenn eines vorhanden wäre, hätte die Kalibrierung langfristiger Kompetenzmetriken den Scope dieser Arbeit überschritten. Das Konzept selbst betont, dass diese Anpassungen "auf Kampagnenebene, nicht während laufender Missionen" erfolgen — sie sind per Definition nicht innerhalb einer einzelnen Mission beobachtbar.

**Vollständige Fehlerkonsequenz-Architektur (Abschnitt 8.4.4)** — Die im Konzept beschriebene granulare Fehlerklassifikation (räumliche Fehler, taktische Fehler, kritische Fehler mit unterschiedlicher Gewichtung und progressiven Konsequenzstufen) hätte eine tiefgreifende Analyse jeder Spielerentscheidung erfordert. Was ist eine "ineffiziente Bewegung"? Was ist "schlechte Deckungsnutzung"? Diese Fragen erfordern spielspezifische Heuristiken, deren Entwicklung und Kalibrierung erheblichen Aufwand darstellt. Der Heldenverlust als Proxy für akkumulierte Fehler ist eine bewusste Vereinfachung.

**Eskalationszyklen im vollen Umfang (Abschnitt 8.5.2)** — Das Konzept beschreibt rhythmische Alternierung zwischen Phasen hoher Gegnerpräsenz und Phasen taktischer Konsolidierung. Die bestehende Spawn-Mechanik in *Lightled* ist kontinuierlich — jede Runde spawnen neue Blips mit steigender Rate. Der Director kann die Rate dämpfen, aber keine echten Phasen mit bewussten Pausen erzwingen, da dies ein Wave-System mit definierten Ruhephasen erfordert hätte. Die Spawn-Reduktion bei hohem Druck erzeugt implizit Ruhephasen, allerdings reaktiv statt proaktiv gestaltet.

---

## Abschließende Betrachtung

Der Einbau zeigt, dass das Transparenz-Schichten-Modell als architektonische Leitlinie funktioniert. Die Trennung zwischen geschützten Mechaniken, modulierbaren Parametern und analytischen Metriken ließ sich direkt in Code übersetzen und erzeugt ein System, das an neun klar definierten Stellen in das bestehende Spiel eingreift, ohne eine einzige bestehende Berechnung zu verändern.

Die größten Kompromisse ergaben sich nicht aus dem Konzept selbst, sondern aus dem Demonstrator. *Lightled* ist ein Spiel in aktiver Entwicklung mit bewussten Designentscheidungen — kein HP-System, kein Fog of War, nur zwei Gegnertypen — die bestimmte Aspekte des Konzepts grundsätzlich ausschließen. Das ist kein Defizit des Konzepts; es zeigt vielmehr, dass die im Konzept beschriebenen Interventionsmöglichkeiten eine reichere Spielmechanik voraussetzen als der aktuelle Stand des Demonstrators bietet.

Was funktioniert: Die Pressure-Messung reagiert spürbar auf Spielzustandsänderungen. Die KI-Aggressionsmodulation erzeugt plausible Verhaltensunterschiede ohne offensichtliche Manipulation. Die Spawn-Steuerung reguliert die Eskalationsrate, ohne die Spielmechanik zu brechen. Das Debug-Overlay (F3) macht alle Werte transparent nachvollziehbar — was dem im Konzept geforderten Transparenzprotokoll zumindest auf Entwicklerebene entspricht.

Was noch Validierung braucht: Alle Schwellenwerte und Gewichtungen sind heuristische Ausgangswerte, wie das Konzept selbst betont. Die Kalibrierung — ob 0.70 der richtige Critical-Schwellenwert ist, ob die Aggressionsreduktion auf 0.4x spürbar genug ist, ob die Spawn-Skip-Wahrscheinlichkeit von 60% bei Critical zu aggressiv oder zu zaghaft ist — erfordert iteratives Playtesting, das im Rahmen dieser Arbeit nicht mehr abgeschlossen werden konnte.
