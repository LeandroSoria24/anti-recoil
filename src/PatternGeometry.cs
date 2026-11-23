using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace RecoilStudio
{
    public class PatternNode
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double TimeMs { get; set; }

        public PatternNode()
        {
        }

        public PatternNode(double x, double y, double timeMs)
        {
            X = x;
            Y = y;
            TimeMs = timeMs;
        }

        public PatternNode Clone()
        {
            return new PatternNode(X, Y, TimeMs);
        }
    }

    /// <summary>
    /// Utilidades matematicas para curvas de patron 2D, plantillas de armas y conversion sin perdida a WeaponProfile.
    /// </summary>
    public static class PatternGeometry
    {
        public static readonly string[] TemplateNames = new string[]
        {
            "AK-47", "M4A4", "M4A1-S",
            "FAMAS", "GALIL", "MP7",
            "UMP-45", "MP9", "MAC-10"
        };

        /// <summary>
        /// Convierte una lista ordenada de nodos de compensacion en parametros y secuencias de WeaponProfile.
        /// </summary>
        public static void NodesToProfile(List<PatternNode> nodes, double intensityScale, bool stopAtEnd, WeaponProfile target)
        {
            if (target == null || nodes == null || nodes.Count < 2) return;

            if (target.secuencias == null)
            {
                target.secuencias = new List<RecoilStep>();
            }
            target.secuencias.Clear();

            double scale = Clamp(intensityScale, 0.05, 10.0);
            double prevTime = 0.0;
            double curX = nodes[0].X * scale;
            double curY = nodes[0].Y * scale;

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                double nextTime = Math.Round(nodes[i + 1].TimeMs);
                if (nextTime < prevTime + 8.0)
                {
                    nextTime = prevTime + 8.0;
                }

                double targetX = nodes[i + 1].X * scale;
                double targetY = nodes[i + 1].Y * scale;
                double ticks = (nextTime - prevTime) / 8.0;

                double rawDx = (targetX - curX) / ticks;
                double rawDy = (targetY - curY) / ticks;

                double dxPerTick = Math.Round(Clamp(rawDx, -20.0, 20.0), 3);
                double dyPerTick = Math.Round(Clamp(rawDy, -15.0, 30.0), 3);

                curX += dxPerTick * ticks;
                curY += dyPerTick * ticks;

                if (i == 0)
                {
                    target.patea_arriba = dyPerTick;
                    target.se_va_lado = Math.Round(-dxPerTick, 3);
                }
                else
                {
                    target.secuencias.Add(new RecoilStep
                    {
                        tiempo_ms = prevTime,
                        patea_arriba = dyPerTick,
                        se_va_lado = Math.Round(-dxPerTick, 3)
                    });
                }

                prevTime = nextTime;
            }

            if (stopAtEnd)
            {
                target.secuencias.Add(new RecoilStep
                {
                    tiempo_ms = prevTime,
                    patea_arriba = 0.0,
                    se_va_lado = 0.0
                });
            }

            target.cambia_de_lado = target.secuencias.Count > 0;
        }

        /// <summary>
        /// Reconstruye nodos 2D a partir de un WeaponProfile existente integrando RecoilEngine.EvaluateVector.
        /// </summary>
        public static List<PatternNode> ProfileToNodes(WeaponProfile profile, double defaultDurationMs, out double detectedDurationMs, out bool detectedStopAtEnd)
        {
            List<PatternNode> nodes = new List<PatternNode>();
            detectedDurationMs = Math.Max(500.0, defaultDurationMs);
            detectedStopAtEnd = false;

            if (profile == null)
            {
                nodes.Add(new PatternNode(0.0, 0.0, 0.0));
                nodes.Add(new PatternNode(0.0, 300.0, detectedDurationMs));
                return nodes;
            }

            RecoilStep[] sorted = ConfigService.BuildSortedSteps(profile);
            List<double> timeMarks = new List<double>();
            timeMarks.Add(0.0);

            if (profile.cambia_de_lado)
            {
                if (sorted.Length > 0)
                {
                    for (int i = 0; i < sorted.Length; i++)
                    {
                        double t = Math.Round(sorted[i].tiempo_ms);
                        if (t > timeMarks[timeMarks.Count - 1] + 0.5)
                        {
                            timeMarks.Add(t);
                        }
                    }

                    RecoilStep lastStep = sorted[sorted.Length - 1];
                    if (Math.Abs(lastStep.patea_arriba) < 0.001 && Math.Abs(lastStep.se_va_lado) < 0.001 && timeMarks.Count >= 2)
                    {
                        detectedStopAtEnd = true;
                        detectedDurationMs = timeMarks[timeMarks.Count - 1];
                    }
                    else
                    {
                        double lastT = timeMarks[timeMarks.Count - 1];
                        double endT = Math.Max(defaultDurationMs, lastT + 300.0);
                        timeMarks.Add(endT);
                        detectedDurationMs = endT;
                    }
                }
                else
                {
                    double switchT = Math.Max(100.0, profile.tiempo_cambio_ms);
                    timeMarks.Add(switchT);
                    double endT = Math.Max(defaultDurationMs, switchT + 400.0);
                    timeMarks.Add(endT);
                    detectedDurationMs = endT;
                }
            }
            else
            {
                detectedDurationMs = Math.Max(500.0, defaultDurationMs);
                timeMarks.Add(detectedDurationMs);
            }

            double curX = 0.0;
            double curY = 0.0;
            nodes.Add(new PatternNode(0.0, 0.0, 0.0));

            for (int i = 0; i < timeMarks.Count - 1; i++)
            {
                double tPrev = timeMarks[i];
                double tCurr = timeMarks[i + 1];
                double dx;
                double dy;
                int phaseIdx;
                RecoilEngine.EvaluateVector(profile, sorted, tPrev + 0.001, out dx, out dy, out phaseIdx);

                double ticks = (tCurr - tPrev) / 8.0;
                curX += dx * ticks;
                curY += dy * ticks;
                nodes.Add(new PatternNode(curX, curY, tCurr));
            }

            return nodes;
        }

        /// <summary>
        /// Suaviza un trazo dibujado a mano alzada y lo remuestrea uniformemente en segmentos controlables.
        /// </summary>
        public static List<PatternNode> ProcessFreehandStroke(List<Point> rawPoints, double totalDurationMs, int targetSegments)
        {
            List<PatternNode> result = new List<PatternNode>();
            if (rawPoints == null || rawPoints.Count < 2)
            {
                result.Add(new PatternNode(0.0, 0.0, 0.0));
                result.Add(new PatternNode(0.0, 300.0, Math.Max(200.0, totalDurationMs)));
                return result;
            }

            int segs = Math.Max(4, Math.Min(24, targetSegments));
            double duration = Math.Max(200.0, totalDurationMs);

            // 1. Normalizar origen a (0,0) y filtrar puntos muy cercanos
            Point origin = rawPoints[0];
            List<Point> filtered = new List<Point>();
            filtered.Add(new Point(0.0, 0.0));

            for (int i = 1; i < rawPoints.Count; i++)
            {
                double rx = rawPoints[i].X - origin.X;
                double ry = rawPoints[i].Y - origin.Y;
                Point last = filtered[filtered.Count - 1];
                double dist = Math.Sqrt((rx - last.X) * (rx - last.X) + (ry - last.Y) * (ry - last.Y));
                if (dist >= 3.0 || i == rawPoints.Count - 1)
                {
                    filtered.Add(new Point(rx, ry));
                }
            }

            if (filtered.Count < 2)
            {
                result.Add(new PatternNode(0.0, 0.0, 0.0));
                result.Add(new PatternNode(0.0, 120.0, duration));
                return result;
            }

            // 2. Dos pasadas de suavizado manteniendo extremos fijos
            List<Point> smoothed = filtered;
            if (smoothed.Count >= 3)
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    List<Point> next = new List<Point>(smoothed.Count);
                    next.Add(smoothed[0]);
                    for (int i = 1; i < smoothed.Count - 1; i++)
                    {
                        double sx = 0.25 * smoothed[i - 1].X + 0.50 * smoothed[i].X + 0.25 * smoothed[i + 1].X;
                        double sy = 0.25 * smoothed[i - 1].Y + 0.50 * smoothed[i].Y + 0.25 * smoothed[i + 1].Y;
                        next.Add(new Point(sx, sy));
                    }
                    next.Add(smoothed[smoothed.Count - 1]);
                    smoothed = next;
                }
            }

            // 3. Longitud acumulada para remuestreo uniforme
            double[] cumDist = new double[smoothed.Count];
            cumDist[0] = 0.0;
            for (int i = 1; i < smoothed.Count; i++)
            {
                double dx = smoothed[i].X - smoothed[i - 1].X;
                double dy = smoothed[i].Y - smoothed[i - 1].Y;
                cumDist[i] = cumDist[i - 1] + Math.Sqrt(dx * dx + dy * dy);
            }

            double totalLen = cumDist[cumDist.Length - 1];
            if (totalLen < 5.0)
            {
                result.Add(new PatternNode(0.0, 0.0, 0.0));
                result.Add(new PatternNode(0.0, 120.0, duration));
                return result;
            }

            result.Add(new PatternNode(0.0, 0.0, 0.0));
            int cursor = 1;
            for (int s = 1; s < segs; s++)
            {
                double targetD = ((double)s / segs) * totalLen;
                while (cursor < cumDist.Length - 1 && cumDist[cursor] < targetD)
                {
                    cursor++;
                }

                double d0 = cumDist[cursor - 1];
                double d1 = cumDist[cursor];
                double frac = (d1 > d0) ? (targetD - d0) / (d1 - d0) : 0.0;
                double px = smoothed[cursor - 1].X + (smoothed[cursor].X - smoothed[cursor - 1].X) * frac;
                double py = smoothed[cursor - 1].Y + (smoothed[cursor].Y - smoothed[cursor - 1].Y) * frac;
                double timeMs = Math.Round(((double)s / segs) * duration);
                result.Add(new PatternNode(px, py, timeMs));
            }

            Point endPt = smoothed[smoothed.Count - 1];
            result.Add(new PatternNode(endPt.X, endPt.Y, Math.Round(duration)));
            return result;
        }

        public static Point EvaluatePositionAtTime(List<PatternNode> nodes, double timeMs)
        {
            if (nodes == null || nodes.Count == 0) return new Point(0.0, 0.0);
            if (nodes.Count == 1 || timeMs <= nodes[0].TimeMs) return new Point(nodes[0].X, nodes[0].Y);

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                PatternNode a = nodes[i];
                PatternNode b = nodes[i + 1];
                if (timeMs <= b.TimeMs)
                {
                    double span = b.TimeMs - a.TimeMs;
                    double frac = span > 0.001 ? Clamp((timeMs - a.TimeMs) / span, 0.0, 1.0) : 0.0;
                    return new Point(a.X + (b.X - a.X) * frac, a.Y + (b.Y - a.Y) * frac);
                }
            }

            PatternNode last = nodes[nodes.Count - 1];
            return new Point(last.X, last.Y);
        }

        public static void MirrorHorizontal(List<PatternNode> nodes)
        {
            if (nodes == null) return;
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].X = -nodes[i].X;
            }
        }

        public static void RescaleDuration(List<PatternNode> nodes, double newTotalDurationMs)
        {
            if (nodes == null || nodes.Count < 2) return;
            double oldEnd = nodes[nodes.Count - 1].TimeMs;
            double targetEnd = Math.Max(200.0, newTotalDurationMs);

            if (oldEnd <= 1.0)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].TimeMs = Math.Round(((double)i / (nodes.Count - 1)) * targetEnd);
                }
                return;
            }

            double ratio = targetEnd / oldEnd;
            nodes[0].TimeMs = 0.0;
            for (int i = 1; i < nodes.Count; i++)
            {
                nodes[i].TimeMs = Math.Round(nodes[i].TimeMs * ratio);
                if (nodes[i].TimeMs < nodes[i - 1].TimeMs + 8.0)
                {
                    nodes[i].TimeMs = nodes[i - 1].TimeMs + 8.0;
                }
            }
            nodes[nodes.Count - 1].TimeMs = Math.Round(targetEnd);
        }

        public static Color GetColorAlongCurve(double t01)
        {
            double t = Clamp(t01, 0.0, 1.0);
            // Ambar (#FBBF24) -> Naranja (#F97316) -> Rojo (#EF4444)
            Color c0 = Color.FromRgb(251, 191, 36);
            Color c1 = Color.FromRgb(249, 115, 22);
            Color c2 = Color.FromRgb(239, 68, 68);

            if (t < 0.5)
            {
                double u = t * 2.0;
                return LerpColor(c0, c1, u);
            }
            else
            {
                double u = (t - 0.5) * 2.0;
                return LerpColor(c1, c2, u);
            }
        }

        private static Color LerpColor(Color a, Color b, double u)
        {
            byte r = (byte)Math.Round(a.R + (b.R - a.R) * u);
            byte g = (byte)Math.Round(a.G + (b.G - a.G) * u);
            byte bl = (byte)Math.Round(a.B + (b.B - a.B) * u);
            return Color.FromRgb(r, g, bl);
        }

        /// <summary>
        /// Plantillas de formas inspiradas en patrones clasicos (x, y, fraccionTiempo 0..1 desde origen 1 arriba).
        /// </summary>
        public static List<PatternNode> GetTemplateNodes(string templateName, double totalDurationMs, out int recommendedBullets)
        {
            double dur = Math.Max(500.0, totalDurationMs);
            recommendedBullets = 30;
            double[] xyt;

            switch ((templateName ?? "").ToUpperInvariant())
            {
                case "AK-47":
                    recommendedBullets = 30;
                    xyt = new double[]
                    {
                        0, 0, 0.00,
                        4, 155, 0.10,
                        18, 325, 0.21,
                        14, 415, 0.28,
                        -34, 442, 0.34,
                        -12, 462, 0.44,
                        68, 482, 0.58,
                        92, 472, 0.66,
                        18, 452, 0.78,
                        -85, 434, 0.90,
                        -142, 424, 1.00
                    };
                    break;

                case "M4A4":
                    recommendedBullets = 30;
                    xyt = new double[]
                    {
                        0, 0, 0.00,
                        -6, 150, 0.11,
                        -10, 305, 0.22,
                        28, 368, 0.30,
                        68, 392, 0.38,
                        15, 412, 0.50,
                        -68, 415, 0.60,
                        -105, 425, 0.69,
                        -38, 446, 0.84,
                        38, 472, 1.00
                    };
                    break;

                case "M4A1-S":
                    recommendedBullets = 25;
                    xyt = new double[]
                    {
                        0, 0, 0.00,
                        2, 145, 0.12,
                        -2, 275, 0.24,
                        26, 332, 0.35,
                        48, 350, 0.44,
                        8, 365, 0.58,
                        -55, 378, 0.74,
                        -88, 392, 0.86,
                        -62, 398, 1.00
                    };
                    break;

                case "FAMAS":
                    recommendedBullets = 25;
                    xyt = new double[]
                    {
                        0, 0, 0.00,
                        -15, 140, 0.13,
                        -18, 275, 0.26,
                        6, 332, 0.38,
                        42, 346, 0.55,
                        78, 355, 0.72,
                        56, 372, 0.84,
                        96, 344, 1.00
                    };
                    break;

                case "GALIL":
                    recommendedBullets = 35;
                    xyt = new double[]
                    {
                        0, 0, 0.00,
                        22, 145, 0.10,
                        46, 285, 0.20,
                        54, 352, 0.28,
                        -22, 375, 0.42,
                        -92, 392, 0.56,
                        -18, 378, 0.70,
                        58, 366, 0.84,
                        -95, 348, 1.00
                    };
                    break;

                case "MP7":
                    recommendedBullets = 30;
                    xyt = new double[]
                    {
                        0, 0, 0.00,
                        -22, 130, 0.11,
                        -46, 250, 0.22,
                        -64, 310, 0.32,
                        -18, 338, 0.46,
                        48, 368, 0.65,
                        8, 350, 0.82,
                        -75, 326, 1.00
                    };
                    break;

                case "UMP-45":
                    recommendedBullets = 25;
                    xyt = new double[]
                    {
                        0, 0, 0.00,
                        -16, 145, 0.14,
                        -22, 285, 0.28,
                        12, 375, 0.42,
                        52, 422, 0.62,
                        74, 446, 0.79,
                        36, 424, 1.00
                    };
                    break;

                case "MP9":
                    recommendedBullets = 30;
                    xyt = new double[]
                    {
                        0, 0, 0.00,
                        -4, 135, 0.11,
                        22, 255, 0.22,
                        72, 332, 0.34,
                        114, 382, 0.48,
                        48, 422, 0.60,
                        -28, 430, 0.72,
                        -68, 440, 0.86,
                        -84, 464, 1.00
                    };
                    break;

                case "MAC-10":
                    recommendedBullets = 30;
                    xyt = new double[]
                    {
                        0, 0, 0.00,
                        18, 140, 0.11,
                        44, 275, 0.22,
                        74, 378, 0.34,
                        82, 422, 0.46,
                        14, 434, 0.58,
                        -70, 422, 0.72,
                        -24, 422, 0.86,
                        4, 420, 1.00
                    };
                    break;

                default:
                    recommendedBullets = 30;
                    xyt = new double[] { 0, 0, 0.00, 0, 220, 0.50, 0, 440, 1.00 };
                    break;
            }

            int count = xyt.Length / 3;
            List<PatternNode> nodes = new List<PatternNode>(count);
            for (int i = 0; i < count; i++)
            {
                double timeMs = Math.Round(xyt[i * 3 + 2] * dur);
                nodes.Add(new PatternNode(xyt[i * 3], xyt[i * 3 + 1], timeMs));
            }
            return nodes;
        }

        private static double Clamp(double val, double min, double max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
    }
}
