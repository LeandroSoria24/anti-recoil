using System;
using System.Collections.Generic;

namespace RecoilStudio
{
    public enum BurstMode
    {
        Linear = 0,
        TwoPhase = 1,
        MultiPhase = 2
    }

    public class RecoilStep
    {
        public double tiempo_ms { get; set; }
        public double se_va_lado { get; set; }
        public double patea_arriba { get; set; }

        public RecoilStep Clone()
        {
            return new RecoilStep
            {
                tiempo_ms = this.tiempo_ms,
                se_va_lado = this.se_va_lado,
                patea_arriba = this.patea_arriba
            };
        }
    }

    /// <summary>
    /// Perfil de compensacion de retroceso por arma. Mantiene compatibilidad exacta con config.json.
    /// </summary>
    public class WeaponProfile
    {
        public double patea_arriba { get; set; }
        public double se_va_lado { get; set; }
        public bool cambia_de_lado { get; set; }
        public double tiempo_cambio_ms { get; set; }
        public double nuevo_lado { get; set; }
        public bool suavizado_activo { get; set; }
        public double humanizacion_fuerza { get; set; }
        public List<RecoilStep> secuencias { get; set; }

        public WeaponProfile()
        {
            secuencias = new List<RecoilStep>();
            tiempo_cambio_ms = 400.0;
        }

        public BurstMode GetBurstMode()
        {
            if (!cambia_de_lado)
            {
                return BurstMode.Linear;
            }
            if (secuencias != null && secuencias.Count > 0)
            {
                return BurstMode.MultiPhase;
            }
            return BurstMode.TwoPhase;
        }

        public WeaponProfile Clone()
        {
            WeaponProfile copy = new WeaponProfile
            {
                patea_arriba = this.patea_arriba,
                se_va_lado = this.se_va_lado,
                cambia_de_lado = this.cambia_de_lado,
                tiempo_cambio_ms = this.tiempo_cambio_ms,
                nuevo_lado = this.nuevo_lado,
                suavizado_activo = this.suavizado_activo,
                humanizacion_fuerza = this.humanizacion_fuerza,
                secuencias = new List<RecoilStep>()
            };

            if (this.secuencias != null)
            {
                for (int i = 0; i < this.secuencias.Count; i++)
                {
                    if (this.secuencias[i] != null)
                    {
                        copy.secuencias.Add(this.secuencias[i].Clone());
                    }
                }
            }

            return copy;
        }
    }

    public class RecoilConfig
    {
        public string active_profile { get; set; }
        public Dictionary<string, WeaponProfile> profiles { get; set; }
        public bool engine_enabled { get; set; }
        public int toggle_key { get; set; }
        public int profile_macro_key { get; set; }
        public string macro_profile_a { get; set; }
        public string macro_profile_b { get; set; }

        public RecoilConfig()
        {
            profiles = new Dictionary<string, WeaponProfile>();
            macro_profile_a = "";
            macro_profile_b = "";
        }
    }

    /// <summary>
    /// Instantanea inmutable consumida sin bloqueos por el hilo del motor.
    /// </summary>
    public class EngineSnapshot
    {
        public bool EngineEnabled { get; private set; }
        public string ActiveProfileName { get; private set; }
        public WeaponProfile Profile { get; private set; }
        public RecoilStep[] SortedSteps { get; private set; }

        public EngineSnapshot(bool engineEnabled, string activeProfileName, WeaponProfile profile, RecoilStep[] sortedSteps)
        {
            EngineEnabled = engineEnabled;
            ActiveProfileName = activeProfileName ?? "";
            Profile = profile;
            SortedSteps = sortedSteps ?? new RecoilStep[0];
        }
    }

    /// <summary>
    /// Telemetria en vivo publicada por el motor para el visualizador y panel de estado.
    /// </summary>
    public class EngineTelemetry
    {
        public static readonly EngineTelemetry Idle = new EngineTelemetry(false, 0.0, -1, 0.0, 0.0, 0.0);

        public bool IsCompensating { get; private set; }
        public double ElapsedBurstMs { get; private set; }
        public int ActivePhaseIndex { get; private set; }
        public double CurrentDx { get; private set; }
        public double CurrentDy { get; private set; }
        public double LastTickMs { get; private set; }

        public EngineTelemetry(bool isCompensating, double elapsedBurstMs, int activePhaseIndex, double currentDx, double currentDy, double lastTickMs)
        {
            IsCompensating = isCompensating;
            ElapsedBurstMs = elapsedBurstMs;
            ActivePhaseIndex = activePhaseIndex;
            CurrentDx = currentDx;
            CurrentDy = currentDy;
            LastTickMs = lastTickMs;
        }
    }
}
