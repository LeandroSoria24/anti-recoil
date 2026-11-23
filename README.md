# Recoil Studio

Aplicación de escritorio en **WPF (.NET Framework 4.8)** para gestionar perfiles de compensación de retroceso y calibrar patrones en tiempo real.

## Características

- **Interfaz moderna oscura (Slate/Obsidian)**: diseño limpio sin bordes, píldora superior interactiva de estado del motor (`ACTIVO` / `COMPENSANDO` / `PAUSADO`) y botón *Pin* (`Topmost`) para mantener la ventana encima al calibrar.
- **Gestión completa de perfiles**: buscador en tiempo real, creación, duplicado, renombrado y borrado sobre `config.json` con escritura atómica (`SaveAtomic`) y respaldo automático ante archivos dañados.
- **Modos de ráfaga**:
  - **Lineal**: vector constante durante toda la ráfaga.
  - **2 Fases**: transición horizontal configurable por tiempo (`100–2000 ms`).
  - **Multi-Fase**: secuencia cronológica ilimitada de pasos (`Ms`, `Vert`, `Horiz`) con ordenamiento automático.
- **Visualizador vectorial y telemetría en vivo**: curva degradada ámbar-naranja-rojo con origen `1` y métricas en tiempo real (`Estado`, `Ráfaga`, `Vector X/Y`, `Fase`).
- **Editor visual de patrones**: dibuja la curva a mano alzada con el mouse, edita nodos de control, simula la ráfaga (`1 → 30`), carga plantillas (`AK-47`, `M4A4`, `MP7`, etc.) y convierte el trazo a pasos Multi-Fase sin pérdida.
- **Motor lock-free de baja latencia**: instantáneas inmutables (`EngineSnapshot`), normalización por *delta-time*, espera híbrida (`Thread.Sleep(1)` + `SpinWait`) con `timeBeginPeriod(1)` y suavizado gaussiano con amortiguación de deriva.
- **Atajos globales por flanco de subida**: tecla/botón para activar/pausar el motor y macro rápida para alternar entre perfil A y perfil B.

## Requisitos

- Windows 10 / 11 (x64)
- .NET Framework 4.8 (incluido en Windows; compila directamente con `csc.exe` sin requerir Visual Studio ni .NET SDK)

## Compilación y ejecución

1. Ejecuta `build.bat` para compilar `RecoilStudio.exe`:
   ```bat
   build.bat
   ```
2. Inicia `RecoilStudio.exe` junto a `config.json`.

## Estructura del proyecto

- `Program.cs` — Punto de entrada WPF con soporte DPI y registro de excepciones.
- `build.bat` — Script de compilación directa con `csc.exe` (.NET Framework 4.8).
- `config.json` — Configuración global y perfiles de armas.
- `src/Models.cs` — Modelos de configuración, instantáneas y telemetría.
- `src/NativeInput.cs` — P/Invoke de entrada Win32 (`SendInput`, `GetAsyncKeyState`, timers de alta resolución).
- `src/ConfigService.cs` — Carga, migración y guardado atómico de `config.json`.
- `src/RecoilEngine.cs` — Bucle de compensación en tiempo real y evaluación vectorial compartida.
- `src/ThemeResources.cs` — Paleta, estilos WPF oscuros e iconos vectoriales.
- `src/Controls.cs` — Controles personalizados `ModernSlider` y `RecoilVisualizer`.
- `src/PatternGeometry.cs` — Conversión bidireccional de curvas 2D, plantillas de armas y suavizado de trazos.
- `src/PatternEditorWindow.cs` — Editor visual interactivo de patrones (`PatternCanvas` y `PatternEditorWindow`).
- `src/ProfileDialog.cs` — Diálogos modales oscuros sin bordes.
- `src/MainWindow.cs` — Ventana principal, lista de perfiles, calibración y telemetría.
