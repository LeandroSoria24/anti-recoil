using System;
using System.Diagnostics;
using System.Threading;

namespace RecoilStudio
{
    public sealed class RecoilEngine : IDisposable
    {
        private const double NominalStepMs = 8.0;

        private readonly ManualResetEventSlim _stopSignal = new ManualResetEventSlim(false);
        private readonly Random _rand = new Random();

        private Thread _workerThread;
        private volatile EngineSnapshot _snapshot;
        private volatile EngineTelemetry _telemetry = EngineTelemetry.Idle;
        private bool _disposed = false;

        public RecoilEngine(EngineSnapshot initialSnapshot)
        {
            _snapshot = initialSnapshot ?? new EngineSnapshot(false, "", null, null);
        }

        public void UpdateSnapshot(EngineSnapshot snapshot)
        {
            if (snapshot != null)
            {
                _snapshot = snapshot;
            }
        }

        public EngineTelemetry GetTelemetry()
        {
            return _telemetry;
        }

        public void Start()
        {
            if (_disposed || _workerThread != null) return;

            _stopSignal.Reset();
            _workerThread = new Thread(RunLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "RecoilEngineLoop"
            };
            _workerThread.Start();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopSignal.Set();
            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(500);
            }
            _stopSignal.Dispose();
        }

        /// <summary>
        /// Evalua el vector de compensacion exacto para un perfil y tiempo transcurrido en milisegundos.
        /// Compartido por el motor en tiempo real y el visualizador vectorial.
        /// </summary>
        public static void EvaluateVector(WeaponProfile profile, RecoilStep[] sortedSteps, double elapsedMs, out double dx, out double dy, out int phaseIndex)
        {
            if (profile == null)
            {
                dx = 0.0;
                dy = 0.0;
                phaseIndex = -1;
                return;
            }

            dy = profile.patea_arriba;
            dx = -profile.se_va_lado;
            phaseIndex = -1;

            if (!profile.cambia_de_lado)
            {
                return;
            }

            if (sortedSteps != null && sortedSteps.Length > 0)
            {
                int matchedIndex = -1;
                for (int i = 0; i < sortedSteps.Length; i++)
                {
                    if (elapsedMs >= sortedSteps[i].tiempo_ms)
                    {
                        matchedIndex = i;
                    }
                    else
                    {
                        break;
                    }
                }

                if (matchedIndex >= 0)
                {
                    dx = -sortedSteps[matchedIndex].se_va_lado;
                    dy = sortedSteps[matchedIndex].patea_arriba;
                    phaseIndex = matchedIndex;
                }
            }
            else if (elapsedMs >= profile.tiempo_cambio_ms)
            {
                dx = -profile.nuevo_lado;
                phaseIndex = 0;
            }
        }

        private void RunLoop()
        {
            NativeInput.BeginHighResolutionTimer();
            double ticksToMs = 1000.0 / Stopwatch.Frequency;
            Stopwatch burstWatch = new Stopwatch();

            try
            {
                while (!_stopSignal.IsSet)
                {
                    EngineSnapshot snap = _snapshot;
                    bool shouldEngage = snap != null &&
                                        snap.EngineEnabled &&
                                        snap.Profile != null &&
                                        NativeInput.IsKeyDown(NativeInput.VK_RBUTTON) &&
                                        NativeInput.IsKeyDown(NativeInput.VK_LBUTTON);

                    if (shouldEngage)
                    {
                        RunBurst(burstWatch, ticksToMs);
                    }
                    else
                    {
                        _telemetry = EngineTelemetry.Idle;
                        _stopSignal.Wait(8);
                    }
                }
            }
            finally
            {
                _telemetry = EngineTelemetry.Idle;
                NativeInput.EndHighResolutionTimer();
            }
        }

        private void RunBurst(Stopwatch burstWatch, double ticksToMs)
        {
            burstWatch.Restart();
            double accumX = 0.0;
            double accumY = 0.0;
            double driftX = 0.0;
            double driftY = 0.0;
            double lastTickMs = -NominalStepMs;
            double nextTargetMs = 0.0;

            while (!_stopSignal.IsSet)
            {
                EngineSnapshot snap = _snapshot;
                if (snap == null ||
                    !snap.EngineEnabled ||
                    snap.Profile == null ||
                    !NativeInput.IsKeyDown(NativeInput.VK_RBUTTON) ||
                    !NativeInput.IsKeyDown(NativeInput.VK_LBUTTON))
                {
                    break;
                }

                double nowMs = burstWatch.ElapsedTicks * ticksToMs;
                double dtMs = nowMs - lastTickMs;
                if (dtMs < 1.0) dtMs = 1.0;
                else if (dtMs > 32.0) dtMs = 32.0;

                lastTickMs = nowMs;
                double scale = dtMs / NominalStepMs;

                double dx;
                double dy;
                int phaseIndex;
                EvaluateVector(snap.Profile, snap.SortedSteps, nowMs, out dx, out dy, out phaseIndex);

                double stepX = dx * scale;
                double stepY = dy * scale;

                double intensity = snap.Profile.humanizacion_fuerza;
                if (intensity > 0.02)
                {
                    double sigma = (intensity / 2.0) * Math.Sqrt(scale);
                    double noiseX = Clamp(NextStandardNormal(), -2.5, 2.5) * sigma;
                    double noiseY = Clamp(NextStandardNormal(), -2.5, 2.5) * sigma;

                    // Amortiguacion de deriva media cero para suavizar sin perder punteria sostenida
                    double jitterX = noiseX - (driftX * 0.15);
                    double jitterY = noiseY - (driftY * 0.15);

                    driftX += jitterX;
                    driftY += jitterY;

                    stepX += jitterX;
                    stepY += jitterY;
                }
                else
                {
                    driftX = 0.0;
                    driftY = 0.0;
                }

                accumX += stepX;
                accumY += stepY;

                int moveX = (int)Math.Truncate(accumX);
                int moveY = (int)Math.Truncate(accumY);

                accumX -= moveX;
                accumY -= moveY;

                if (moveX != 0 || moveY != 0)
                {
                    try
                    {
                        NativeInput.SendMouseMove(moveX, moveY);
                    }
                    catch
                    {
                    }
                }

                _telemetry = new EngineTelemetry(true, nowMs, phaseIndex, dx, dy, dtMs);

                nextTargetMs += NominalStepMs;
                double currentAfterMs = burstWatch.ElapsedTicks * ticksToMs;
                if (nextTargetMs < currentAfterMs)
                {
                    nextTargetMs = currentAfterMs;
                }

                HybridWaitUntil(burstWatch, nextTargetMs, ticksToMs);
            }

            burstWatch.Stop();
            _telemetry = EngineTelemetry.Idle;
        }

        private void HybridWaitUntil(Stopwatch sw, double targetMs, double ticksToMs)
        {
            while (!_stopSignal.IsSet)
            {
                double remainingMs = targetMs - (sw.ElapsedTicks * ticksToMs);
                if (remainingMs <= 0.0)
                {
                    return;
                }

                if (remainingMs > 2.2)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    Thread.SpinWait(25);
                }
            }
        }

        private double NextStandardNormal()
        {
            double u1 = 1.0 - _rand.NextDouble();
            double u2 = 1.0 - _rand.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
