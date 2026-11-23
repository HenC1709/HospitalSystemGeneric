# Bitácora de Desarrollo — HospitalSystem

**Rol**: Desarrollador .NET Semi-Senior  
**Stack**: .NET 10 (`10.0.300`) · C# 14 · WPF + WPF-UI (`4.3.0`) · CommunityToolkit.Mvvm (`8.4.0`) · MySQL (`MySqlConnector 3.0.0`)

---

## Entrada 1 — Relevamiento del Repositorio Inicial
- **Estado encontrado**: La solución `HospitalSystem.slnx` contaba con cuatro proyectos (`Core`, `Application`, `Infrastructure` y `Console`) y una carpeta `Database/` con `schema.sql` y `seed.sql`, pero todos los archivos `.cs` y `.sql` estaban vacíos (0 bytes) como esqueleto inicial.
- **Corrección detectada**: En `HospitalSystem.Application/Interfaces/` existía el archivo vacío con error tipográfico `IHpostialRepository.cs`. Se reemplazó por `IHospitalRepository.cs` respetando la convención de nombres del dominio.
- **Configuración transversal**: Se creó `Directory.Build.props` en la raíz fijando `<LangVersion>14.0</LangVersion>`, `<Nullable>enable</Nullable>` e `<ImplicitUsings>enable</ImplicitUsings>` para garantizar C# 14 uniforme en todos los proyectos.

## Entrada 2 — Diseño de Base de Datos MySQL y Funciones Almacenadas
- Se diseñó en `Database/schema.sql` la base `hospital_system_db` con 11 tablas normalizadas (`hospitales`, `convenios`, `salas`, `camas`, `medicos`, `pacientes`, `diagnosticos`, `atenciones`, `diagnosticos_paciente`, `laboratorios` y `salidas_laboratorio`).
- Dado que el objetivo principal de la app es **probar MySQL**, en lugar de ocultar toda la lógica en LINQ se implementaron objetos nativos en el motor SQL:
  - **5 funciones almacenadas**: `fn_camas_disponibles()`, `fn_total_salas_activas()`, `fn_medicos_del_dia()`, `fn_ocupacion_sala_pct(p_sala_id)` y `fn_paciente_tiene_salida_activa(p_paciente_id)`.
  - **2 procedimientos transaccionales**: `sp_registrar_ingreso_paciente` (upsert por cédula + ocupación de cama + derivación opcional a laboratorio en una sola transacción) y `sp_dar_alta_atencion` (egreso médico + liberación automática de cama).
  - **1 vista de indicadores**: `vw_indicadores_hospital`.
- En `Database/seed.sql` se cargaron 5 salas, 16 camas (13 libres y 3 ocupadas), 4 convenios, 4 laboratorios (3 internos y 1 externo por convenio), una plantilla de 8 médicos (6 **Médicos del Día** activos en guardia hoy y 2 fuera de turno) y 3 pacientes iniciales con episodios clínicos y salidas a laboratorio.

## Entrada 3 — Modelado de Dominio y Casos de Uso con C# 14
- En `HospitalSystem.Core` se completaron las entidades aplicando características reales de **C# 14**:
  - Uso de la palabra clave contextual **`field`** en propiedades de `Paciente` y `Medico` para validar reglas obligatorias (*Cédula*, *Nombres* y *Síntomas*) sin declarar campos privados `_backingField`.
  - Uso de **bloques `extension(...)`** en `HospitalDomainExtensions` para exponer propiedades calculadas (`TotalCamas`, `CamasDisponibles`, `PorcentajeOcupacion`, `EtiquetaTurno`, `TextoLegible`).
- En `HospitalSystem.Application` se definieron DTOs como `record` inmutables y la fachada de aplicación `HospitalServices` encargada de validar las reglas de negocio antes de invocar la persistencia.

## Entrada 4 — Infraestructura Híbrida MySQL + Respaldo Local
- Se eligió `MySqlConnector` directo sobre EF Core porque facilita invocar procedimientos almacenados con parámetros `OUT`, funciones escalares de MySQL y scripts DDL completos sin fricción.
- En `HospitalDataStore` se programó un intérprete ligero de scripts SQL capaz de procesar directivas `DELIMITER $$` y `DELIMITER ;`, permitiendo inicializar `schema.sql` y `seed.sql` con un botón directamente desde la app de escritorio.
- Se aplicó asignación condicional nula de C# 14 (`?.=`) al liberar/ocupar camas y actualizar estados en memoria.
- Si el servidor MySQL aún no está levantado o las credenciales de `appsettings.json` no han sido configuradas, `HospitalRepository` opera sobre `HospitalDataStore` informando claramente el origen de datos para evitar caídas en tiempo de arranque.

## Entrada 5 — Construcción del Cliente Desktop con WPF-UI
- Se creó el proyecto `HospitalSystem.Desktop` (`net10.0-windows`) y se incorporó a `HospitalSystem.slnx` y `.vscode/launch.json`.
- Siguiendo el requerimiento de **evitar líneas largas de estilos en WPF**, `App.xaml` se redujo a 16 líneas cargando únicamente `ThemesDictionary` y `ControlsDictionary` de **WPF-UI**.
- Se dividió la interfaz en vistas desacopladas (`ResumenView`, `PacientesView`, `MedicosView`, `LaboratoriosView`) gobernadas por `MainViewModel` (también apoyado en `field` de C# 14 y `[RelayCommand]` de `CommunityToolkit.Mvvm`).

## Entrada 6 — Compilación, Pruebas de Humo y Documentación
- Se verificó la compilación completa de los 5 proyectos de la solución sin errores y se probó el diagnóstico de conexión desde `HospitalSystem.Console`.
- Se redactaron `README.md`, `CONEXION_MYSQL.md` (con diagrama ER y documentación de cada tabla, función, procedimiento y vista) y la presente bitácora.

## Entrada 7 — Rediseño Clínico, Navbar Superior, Hover y Animaciones
- **Paleta de colores hospitalarios**: Se cambió el tema base de `WPF-UI` a modo luminoso clínico (`ApplicationTheme.Light` + acento médico `#0284C7`) y se centralizaron los pinceles sanitarios (blanco limpio, azul médico, verde guardia/disponibilidad y ámbar laboratorio) en `Themes/HospitalStyles.xaml`.
- **Navbar superior**: Se reemplazó el menú lateral por una barra de navegación horizontal superior en `MainWindow.xaml` con identidad institucional, pestañas con subrayado activo y efecto Hover, píldoras en vivo de camas/salas/médicos y un punto indicador pulsante (`RepeatBehavior="Forever"`) para el estado de MySQL.
- **Botones con Hover y animaciones**:
  - Estilos compactos `HospitalPrimaryButtonStyle`, `HospitalSecondaryButtonStyle` y `HospitalNavTabButtonStyle` con `ScaleTransform` / `TranslateTransform` animados (`DoubleAnimation` de 120–160 ms) al pasar el cursor (`IsMouseOver`).
  - Tarjetas `HospitalHoverCardStyle` que se elevan suavemente (`Y = -3`) e iluminan su borde en Hover, más barras de progreso de ocupación por sala en `ResumenView.xaml`.
  - Transiciones suaves *fade + slide* (`CubicEase`) en `MainWindow.xaml.cs` cada vez que el usuario navega en el Navbar o se dispara una notificación en el nuevo **banner clínico de feedback visual** codificado por severidad.

## Entrada 8 — Correcciones de Concurrencia, Modularización y Suite xUnit (Code Review)
1. **Protección contra asignación de camas ocupadas**: En `sp_registrar_ingreso_paciente` (`Database/schema.sql`) se añadió verificación `IF ROW_COUNT() <> 1` + `SIGNAL SQLSTATE '45000'` para provocar `ROLLBACK` si la cama ya fue tomada, replicando la misma validación atómica con `Lock` en `HospitalDataStore.OcuparCamaDisponible`.
2. **Restablecimiento seguro de BD demo y migraciones**: Se renombró la acción destructiva de la interfaz a **«Restablecer base de datos de demostración»** exigiendo confirmación explícita (`MessageBoxResult.Yes`), y se creó `Database/migrations/001_schema_no_destructivo.sql` como base para evolución no destructiva.
3. **Blindaje del alta médica**: En `sp_dar_alta_atencion` se incorporó `EXIT HANDLER FOR SQLEXCEPTION`, bloqueo de fila `SELECT ... FOR UPDATE`, rechazo de altas repetidas (`estado = 'AltaMedica'`) y cláusula `NOT EXISTS` antes de liberar la cama para impedir que un alta tardía o duplicada libere una cama ya reasignada a otro paciente.
4. **Aviso explícito de datos ficticios**: Se añadieron advertencias visibles en UI (`MainWindow.xaml` y `PacientesView.xaml`) y en `README.md` indicando que el sistema carece de autenticación/RBAC/auditoría clínica y debe usarse únicamente con datos sintéticos.
5. **Modularización y pruebas xUnit**: Se dividieron `HospitalRepository` y `MainViewModel` en clases parciales por dominio (`Dashboard`, `Pacientes`, `Medicos`, `Laboratorios`) reduciendo cada archivo a ~95–185 líneas, y se agregó el proyecto `HospitalSystem.Tests` con pruebas automatizadas en **xUnit**.

## Entrada 9 — Modo Oscuro en Caliente, Exportación PDF y Ticket de Salida
- **Modo Oscuro / Modo Claro**: Se implementó `HospitalThemeManager.cs` sincronizando `ApplicationThemeManager.Apply` de `WPF-UI` con pinceles dinámicos (`{DynamicResource ...}`) en todas las vistas, agregando un botón conmutador directo en el Navbar superior para turnos nocturnos.
- **Exportación de Expediente PDF e Impresión de Ticket de Salida**: Se creó el módulo parcial `MainViewModel.Exportacion.cs` junto con botones rápidos en `PacientesView.xaml` (barra superior y acciones por fila) y un diálogo modal superpuesto en `MainWindow.xaml` con gradiente institucional azul-teal, animación de rebote elástico (`BackEase`) y barrido de barra de confirmación.
